using System.Collections.Concurrent;

namespace Scheduler
{
    public class JobQueue
    {
        #region Fields

        private BlockingCollection<Action> _jobQueue;

        private int _numberOfThreads;

        private Thread[]? _workers;

        private CancellationTokenSource? _cts;

        #endregion

        #region Constructor

        public JobQueue(int numberOfThreads = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfThreads, nameof(numberOfThreads));

            _jobQueue = new BlockingCollection<Action>();
            _numberOfThreads = numberOfThreads;
        }

        #endregion

        /// <summary>
        /// Adds a job to the queue for processing.
        /// </summary>
        /// <param name="job"></param>
        public void Submit(Action job)
        {
            ArgumentNullException.ThrowIfNull(job);
            _jobQueue.Add(job);
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
                foreach (var job in _jobQueue.GetConsumingEnumerable(cancellationToken))
                {
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
