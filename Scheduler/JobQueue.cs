
using System.Diagnostics;

namespace Scheduler
{
    public class JobQueue
    {
        #region Fields

        private readonly Queue<Action>[] _jobQueues;

        private readonly Lock _lock = new();

        private readonly SemaphoreSlim _semaphore;

        private readonly int _numberOfThreads;

        private Thread[]? _workers;

        private CancellationTokenSource? _cts;

        #endregion

        #region Constructor

        public JobQueue(int numberOfThreads = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfThreads, nameof(numberOfThreads));

            int size = Enum.GetNames<Priority>().Length;

            _jobQueues = new Queue<Action>[size];

            foreach (Priority p in Enum.GetValues<Priority>())
            {
                _jobQueues[(int) p] = new Queue<Action>();
            }

            _semaphore = new SemaphoreSlim(0);
            _numberOfThreads = numberOfThreads;
        }

        #endregion

        /// <summary>
        /// Adds a job to the queue for processing.
        /// </summary>
        /// <param name="job">Job that needs to be run</param>
        /// <param name="priority">Priority of the job</param>
        public void Submit(Action job, Priority priority = Priority.Moderate)
        {
            ArgumentNullException.ThrowIfNull(job);

            if(!Enum.IsDefined(priority))
            {
                throw new ArgumentOutOfRangeException(nameof(priority), priority, "Invalid order priority value provided.");
            }

            lock (_lock)
            {
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
                    Action job;

                    lock (_lock)
                    {
                        Queue<Action>? firstNonEmptyQueue = null;
                        for (int i = 0; i < _jobQueues.Length; i++)
                        {
                            if (_jobQueues[i].Count > 0)
                            {
                                firstNonEmptyQueue = _jobQueues[i];
                                break;
                            }
                        }
                        if (firstNonEmptyQueue != null)
                        {
                            job = firstNonEmptyQueue.Dequeue();
                        }
                        else
                        {
                            Debug.Fail("No jobs left in the queue");
                            continue;
                        }
                    }

                    try
                    {
                        job.Invoke();
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

        #endregion
    }
}
