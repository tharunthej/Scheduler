
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Scheduler
{
    public class JobQueue
    {
        #region Fields

        private readonly Queue<Job>[] _jobQueues;

        private readonly Lock _lock = new();

        private readonly SemaphoreSlim _semaphore;

        private readonly int _numberOfThreads;

        private Thread[]? _workers;

        private CancellationTokenSource? _cts;

        private readonly TimeSpan? _agingInterval;

        #endregion

        #region Constructor

        public JobQueue(int numberOfThreads = 1, TimeSpan? agingInterval = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfThreads, nameof(numberOfThreads));
            if (agingInterval is { } interval)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero, nameof(agingInterval));
            }

            int size = Enum.GetNames<Priority>().Length;

            _jobQueues = new Queue<Job>[size];

            foreach (Priority p in Enum.GetValues<Priority>())
            {
                _jobQueues[(int) p] = new Queue<Job>();
            }

            _semaphore = new SemaphoreSlim(0);
            _numberOfThreads = numberOfThreads;
            _agingInterval = agingInterval;
        }

        #endregion

        /// <summary>
        /// Adds a job to the queue for processing.
        /// </summary>
        /// <param name="work">Job that needs to be run</param>
        /// <param name="priority">Priority of the job</param>
        public void Submit(Action work, Priority priority = Priority.Moderate)
        {
            ArgumentNullException.ThrowIfNull(work);

            if(!Enum.IsDefined(priority))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "Invalid order priority value provided.");
            }

            lock (_lock)
            {
                var timestamp = Stopwatch.GetTimestamp();
                Job job = new Job(work, timestamp);
                _jobQueues[(int) priority].Enqueue(job);
            }
            
            _semaphore.Release();
        }

        /// <summary>
        /// Begins processing the jobs in the queue in a loop.
        /// </summary>
        public void StartProcess()
        {
            if (_workers is null)
            {
                _cts = new CancellationTokenSource();

                CancellationToken cancellationToken = _cts.Token;

                _workers = new Thread[_numberOfThreads];

                for (int i=0; i<_numberOfThreads; i++)
                {
                    _workers[i] = new Thread(() => ProcessJobs(cancellationToken));
                    _workers[i].Name = $"Worker-{i+1}";
                    _workers[i].Start();
                }
            }
            else
            {
                throw new InvalidOperationException("The process is already running");
            }
        }

        /// <summary>
        /// Stops the currently running process, if any.
        /// Queued jobs - will remain in the queue and can be processed later.
        /// Running jobs - will be finished before the process stops.
        /// Blocks until the worker threads has returned.
        /// </summary>
        public void StopProcess()
        {
            if (_workers is not null)
            {
                _cts!.Cancel();

                foreach (Thread t in _workers)
                {
                    t.Join();
                }

                _cts.Dispose();

                _workers = null;
            }
        }

        #region Private Methods

        private void ProcessJobs(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    _semaphore.Wait(cancellationToken);
                    
                    if (!TryTakeNextJob(out Action? work))
                    {
                        Debug.Fail("A worker held a ticket but no job found. This should never happen.");
                        continue;
                    }

                    try
                    {
                        work.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation is cancelled
            }
        }

        /// <summary>
        /// Lower number = higher priority. A job is promoted one level for every full
        /// aging interval it has waited, but never above High (index 0).
        /// </summary>
        internal static int GetEffectivePriority(int queueIndex, long enqueuedAt, long now, TimeSpan? agingInterval)
        {
            if (agingInterval is not { } interval)
            {
                return queueIndex;                                   // strict priority: no aging
            }

            TimeSpan waited = Stopwatch.GetElapsedTime(enqueuedAt, now);
            long levelsPromoted = waited.Ticks / interval.Ticks;     // whole intervals waited; long, so no overflow

            return (int)Math.Clamp(queueIndex - levelsPromoted, 0, queueIndex);
        }

        /// <summary>
        /// Picks the next job (best effective priority; oldest wins a tie) and removes it
        /// from its queue. Returns false if every queue is empty.
        /// </summary>
        private bool TryTakeNextJob([NotNullWhen(true)] out Action? work)
        {
            lock (_lock)
            {
                long now = Stopwatch.GetTimestamp();     // one reading for the whole scan, inside the lock

                int bestQueue = -1;
                int bestEffectivePriority = int.MaxValue;
                long bestEnqueuedAt = long.MaxValue;

                for (int i = 0; i < _jobQueues.Length; i++)
                {
                    if (!_jobQueues[i].TryPeek(out Job head))
                    {
                        continue;                        // this queue is empty
                    }

                    int effective = GetEffectivePriority(i, head.EnqueuedAt, now, _agingInterval);

                    bool isBetter = effective < bestEffectivePriority
                        || (effective == bestEffectivePriority && head.EnqueuedAt < bestEnqueuedAt);   // tie: oldest first

                    if (isBetter)
                    {
                        bestQueue = i;
                        bestEffectivePriority = effective;
                        bestEnqueuedAt = head.EnqueuedAt;
                    }
                }

                if (bestQueue < 0)
                {
                    work = null;
                    return false;
                }

                work = _jobQueues[bestQueue].Dequeue().Work;
                return true;
            }
        }

        #endregion
    }
}
