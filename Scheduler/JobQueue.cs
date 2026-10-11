using System.Collections.Concurrent;

namespace Scheduler
{
    public class JobQueue
    {
        #region Fields

        private BlockingCollection<Action> _jobQueue;

        private Thread _worker;

        private CancellationTokenSource _cts;

        #endregion

        #region Constructor

        public JobQueue()
        {
            _jobQueue = new BlockingCollection<Action>();
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
              if (_worker is null)
              {
                  _cts = new CancellationTokenSource();

                  CancellationToken cancellationToken = _cts.Token;

                  _worker = new Thread(() => ProcessJobs(cancellationToken));

                  _worker.Start();
              }
              else
              {
                  throw new InvalidOperationException();
              }     
        }

        /// <summary>
        /// Stops the currently running process, if any.
        /// Queued jobs - will remain in the queue and can be processed later.
        /// Running jobs - will be finished before the process stops.
        /// Blocks until the worker thread has returned.
        /// </summary>
        public void StopProcess()
        {
            if (_worker is not null)
            {
                _cts.Cancel();

                _worker.Join();

                _cts.Dispose();

                _worker = null;
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
            catch (OperationCanceledException ex)
            {
                // Operation is cancelled
            }
        }

        #endregion
    }
}
