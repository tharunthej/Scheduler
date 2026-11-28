
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

        private readonly int _numberOfTasks;

        private Task[]? _tasks;

        private CancellationTokenSource? _cts;

        private readonly TimeSpan? _agingInterval;

        #endregion

        #region Constructor

        // workerCount: max number of jobs running at the same time
        public JobQueue(int workerCount = 1, TimeSpan? agingInterval = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workerCount, nameof(workerCount));
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
            _numberOfTasks = workerCount;
            _agingInterval = agingInterval;
        }

        #endregion

        /// <summary>
        /// Adds a job to the queue for processing. (only for synchronous jobs)
        /// </summary>
        /// <param name="action">Job that needs to be run. Normal sync jobs run on pool threads and should be short, and LongRunning is for jobs that block or run for a long time</param>
        /// <param name="priority">Priority of the job</param>
        /// <param name="option">Options for the job</param>
        public void Submit(Action action, Priority priority = Priority.Moderate, JobOption option = JobOption.None)
        {
            ArgumentNullException.ThrowIfNull(action);
            Func<Task> wrappedAction;

            switch (option)
            {
                case JobOption.None:
                    wrappedAction = () =>  
                    {
                        action();
                        return Task.CompletedTask;
                    };
                    break;

                case JobOption.LongRunning:
                    wrappedAction = () =>
                    {
                        Task task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                        return task;
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, "Invalid job option value provided.");
            }
            
            Submit(wrappedAction, priority);
        }

        /// <summary>
        /// Adds a job to queue for processing. (For async jobs)
        /// </summary>
        /// <param name="work">Work that needs to be run asynchronously</param>
        /// <param name="priority">Priority of the work</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Submit(Func<Task> work, Priority priority = Priority.Moderate)
        {
            ArgumentNullException.ThrowIfNull(work);

            if (!Enum.IsDefined(priority))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "Invalid order priority value provided.");
            }

            lock (_lock)
            {
                var timestamp = Stopwatch.GetTimestamp();
                Job job = new Job(work, timestamp);
                _jobQueues[(int)priority].Enqueue(job);
            }

            _semaphore.Release();
        }

        /// <summary>
        /// Begins processing the jobs in the queue in a loop.
        /// </summary>
        public void StartProcess()
        {
            if (_tasks is null)
            {
                _cts = new CancellationTokenSource();

                CancellationToken cancellationToken = _cts.Token;

                _tasks = new Task[_numberOfTasks];

                for (int i=0; i<_numberOfTasks; i++)
                {
                    _tasks[i] = Task.Run(() => ProcessJobsAsync(cancellationToken));
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
        /// Blocks until the worker threads have returned.
        /// </summary>
        public void StopProcess()
        {
            StopProcessAsync().GetAwaiter().GetResult();
        }

        public async Task StopProcessAsync()
        {
            if (_tasks is not null)
            {
                try
                {
                    _cts!.Cancel();

                    await Task.WhenAll(_tasks);
                }
                finally
                {
                    _cts?.Dispose();
                    _tasks = null;
                }
            }
        }

        #region Private Methods

        private async Task ProcessJobsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    
                    if (!TryTakeNextJob(out Func<Task>? work))
                    {
                        Debug.Fail("A worker held a ticket but no job found. This should never happen.");
                        continue;
                    }

                    try
                    {
                        Task? task = work();

                        if (task is not null)
                        {
                            await task;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception occurred while processing job: {ex}");
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
        private bool TryTakeNextJob([NotNullWhen(true)] out Func<Task>? work)
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
