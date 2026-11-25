using Scheduler;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

// Console.WriteLine(Environment.ProcessorCount.ToString());

/*
List<int> threads = [10, 16, 20, 32, 64];

foreach (int n in threads)
{
    Console.WriteLine($"===================Test with {n} threads======================");
    JobQueue queue = new JobQueue(n);
    CountdownEvent countdown = new CountdownEvent(20);

    for (int i = 0; i < 20; i++)
    {
        var id = i + 1;
        queue.Submit(() =>
        {
            Thread.Sleep(100);
            Console.WriteLine($"Job number - {id}, Thread id: {Environment.CurrentManagedThreadId}");
            countdown.Signal();
        });
    }

    long startTime = Stopwatch.GetTimestamp();

    queue.StartProcess();
    countdown.Wait();

    TimeSpan timeElapsed = Stopwatch.GetElapsedTime(startTime);

    queue.StopProcess();

    Console.WriteLine($"Total time: {timeElapsed.TotalMilliseconds}");
    Console.WriteLine();
}
*/


// Stage 2, Step 4
/*
JobQueue queue = new JobQueue(32);
CountdownEvent countdown = new CountdownEvent(100000);
int count = 0;

for (int i = 0; i < 100000; i++)
{
    queue.Submit(() =>
    {
        Interlocked.Increment(ref count);
        countdown.Signal();
    });
}
*/

/*
Lock countLock = new();

for (int i=0;i<100000;i++)
{
    queue.Submit(() =>
    {
        lock (countLock)
        {
            count++;
            countdown.Signal();
        }
    });
}
*/

/*
long startTime = Stopwatch.GetTimestamp();

queue.StartProcess();

countdown.Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(startTime);

queue.StopProcess();

Console.WriteLine($"Time Elapsed: {timeElapsed.TotalMilliseconds}");

Console.WriteLine(count);
*/

/*
// Stage 2, step 5a
int count = 0;
long startTime = Stopwatch.GetTimestamp(); 

for (int i=0;i<100000;i++)
{
    Interlocked.Increment(ref count);
}

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(startTime);

Console.WriteLine(timeElapsed.TotalMilliseconds);
*/

// Stage 2, step 5b

// Work Calibration
/*
static long Work(int n)
{
    long total = 0;
    for (int i = 0; i < n; i++) total += (long)i * i;
    return total;
}
*/
/*
int n = 15285;          // size of ONE job (the number you tune)
int calls = 10000;   // repetitions, only to make timing measurable
long sink = 0;         // keeps results "used" so the loop isn't optimized away

for (int w = 0; w < 5000; w++) sink += Work(n);    // warm-up, not timed

long start = Stopwatch.GetTimestamp();
for (int c = 0; c < calls; c++) sink += Work(n);
double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

Console.WriteLine($"{ms / calls * 1000:F3} µs per call (sink={sink})");
*/

// Test
/*
JobQueue queue = new JobQueue(1);

int n = 15285;
int calls = 10000;
long sink = 0;

CountdownEvent countdown = new CountdownEvent(calls);

for (int w = 0; w < 5000; w++) sink += Work(n);
sink = 0;

for (int c = 0; c < calls; c++)
{
    queue.Submit(() =>
    {
        long res = Work(n);
        if (res == -1)
        {
            sink = res;
        }
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();
queue.StartProcess();
countdown.Wait();
double timeElapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

queue.StopProcess();
Console.WriteLine(timeElapsed);
*/

// Stage 3, step 3

/*
CountdownEvent countdown = new(6);

JobQueue queue = new(1);

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, Low-1");
    countdown.Signal();
},Priority.Low);

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, Normal-1");
    countdown.Signal();
});

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, High-1");
    countdown.Signal();
}, Priority.High);

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, Normal-2");
    countdown.Signal();
}, Priority.Moderate);

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, High-2");
    countdown.Signal();
}, Priority.High);

queue.Submit(() =>
{
    Console.WriteLine($"Name of the thread: {Thread.CurrentThread.Name}, Is managed thread: {Thread.CurrentThread.IsThreadPoolThread}, Low-2");
    countdown.Signal();
}, Priority.Low);

queue.StartProcess();

countdown.Wait();

queue.StopProcess();
*/
/*
JobQueue queue = new();
CountdownEvent countdown = new(12);
int count = 0;
List<Priority> list = new(12);

for (int i = 0; i< 4; i++)
{
    Thread thread = new Thread(() =>
    {
        for (int j = 0; j < 50; j++)
        {
            if (j%2 == 0)
            {
                queue.Submit(() =>
                {
                    Interlocked.Increment(ref count);
                    Thread.Sleep(100);
                    countdown.Signal();
                }, Priority.High);
            }
            else if (j % 3 == 0)
            {
                queue.Submit(() =>
                {
                    Interlocked.Increment(ref count);
                    Thread.Sleep(100);
                    countdown.Signal();
                }, Priority.Moderate);
            }
            else
            {
                queue.Submit(() =>
                {
                    Interlocked.Increment(ref count);
                    Thread.Sleep(100);
                    countdown.Signal();
                }, Priority.Low);
            }
        }
    });
    thread.Start();
}
*/

/*
for (int i = 0; i < 12;  i++)
{
    if (i%3 == 0)
    {
        queue.Submit(() =>
        {
            list.Add(Priority.High);
            Thread.Sleep(50);
            countdown.Signal();
        },Priority.High);
    }
    else if (i % 3 == 1)
    {
        queue.Submit(() =>
        {
            list.Add(Priority.Moderate);
            Thread.Sleep(50);
            countdown.Signal();
        }, Priority.Moderate);
    }
    else
    {
        queue.Submit(() =>
        {
            list.Add(Priority.Low);
            Thread.Sleep(50);
            countdown.Signal();
        }, Priority.Low);
    }
}

queue.StartProcess();
Thread.Sleep(250);
queue.StopProcess();
list.ForEach(p => Console.WriteLine(p.ToString()));

queue.StartProcess();
countdown.Wait();
queue.StopProcess();

list.ForEach(p => Console.WriteLine(p.ToString()));
*/

/*
long start = Stopwatch.GetTimestamp();

for (int i = 0; i < 100;  i++)
{
    Thread.Sleep(5);
}

double timeelapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

Console.WriteLine($"Time per loop for 5ms: {timeelapsed / 100}");

start = Stopwatch.GetTimestamp();

for (int i = 0; i < 100; i++)
{
    Thread.Sleep(10);
}

timeelapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

Console.WriteLine($"Time per loop for 10ms: {timeelapsed / 100}");

*/

/*
JobQueue queue = new();
int count = 0;
CountdownEvent countdown = new(1);
long start = Stopwatch.GetTimestamp();

queue.Submit(() =>
{
    Console.WriteLine($"Low job started after: {Stopwatch.GetElapsedTime(start)}");
    countdown.Signal();
    long end = Stopwatch.GetTimestamp();
    Console.WriteLine($"Low job ended after : {Stopwatch.GetElapsedTime(start)}");
},Priority.Low);
Console.WriteLine($"Low job submitted time: {Stopwatch.GetElapsedTime(start)}");

for (int i = 0; i < 3; i++)
{
    queue.Submit(() =>
    {
        count++;
        Thread.Sleep(20);
    }, Priority.High);
}

Thread thread = new(() =>
{
    long start = Stopwatch.GetTimestamp();
    TimeSpan threesecs = TimeSpan.FromSeconds(3);

    while(true)
    {
        TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);
        if (timeElapsed >= threesecs) break;

        for (int i = 0; i < 3; i++)
        {
            queue.Submit(() =>
            {
                count++;
                Thread.Sleep(20);
            }, Priority.High);
        }

        Thread.Sleep(5);
    }
});

queue.StartProcess();

thread.Start();

thread.Join();

countdown.Wait();

queue.StopProcess();

Console.WriteLine($"Count: {count}");


start = Stopwatch.GetTimestamp();
TimeSpan interval = TimeSpan.FromSeconds(2);

foreach (double seconds in new[] { 1.0, 2.1, 4.1, 9.1 })
{
    long now = start + (long)(seconds * Stopwatch.Frequency);
    Console.WriteLine($"{seconds} s -> {JobQueue.GetEffectivePriority(2, start, now, interval)}");
}

*/

/*
async Task Job()
{
    await Task.Delay(1000);
}
*/

/*
// Test A: Fire and Forget
List<Task> tasks = new List<Task>();

long start = Stopwatch.GetTimestamp();
for (int i = 0; i < 10; i++)
{
    tasks.Add(Job());
}

Task.WhenAll(tasks).Wait();
TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);
Console.WriteLine($"Total time for 10 jobs: {timeElapsed.TotalMilliseconds} ms");
*/

/*
// Test B: Blocking Call
Thread thread1 = new Thread(() =>
{
    for (int i = 0; i < 5; i++)
    {
        Console.WriteLine($"Before awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
        Job().GetAwaiter().GetResult();
        Console.WriteLine($"After awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
    }
});

Thread thread2 = new Thread(() =>
{
    for (int i = 0; i < 5; i++)
    {
        Console.WriteLine($"Before awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
        Job().GetAwaiter().GetResult();
        Console.WriteLine($"After awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
    }
});

long start = Stopwatch.GetTimestamp();

thread1.Start();
thread2.Start();

thread1.Join();
thread2.Join();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);
Console.WriteLine($"Total time for blocking call: {timeElapsed.TotalMilliseconds} ms");
*/

/*
// Test C: Awating workers
async Task Job()
{
    for (int i = 0; i < 5; i++)
    {
        if (i == 3)
        {
            Console.WriteLine($"At iteration 3: {ThreadPool.ThreadCount}");
        }
        Console.WriteLine($"Before awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
        await Task.Delay(1000);
        Console.WriteLine($"After awaiter. Thread Id: {Thread.CurrentThread.ManagedThreadId}, Is Thread Pool Thread: {Thread.CurrentThread.IsThreadPoolThread}");
    }
}

long start = Stopwatch.GetTimestamp();

Task task1 = Job();
Task task2 = Job();

Task.WhenAll(task1, task2).Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);
Console.WriteLine($"Time elapsed for async/await: {timeElapsed.TotalMilliseconds} ms");
*/

/*
// Blocking implementation tests
JobQueue queue = new JobQueue(50);
CountdownEvent countdown = new CountdownEvent(50);

for (int i = 0; i < 50; i++)
{
    queue.Submit(async () =>
    {
        await Task.Delay(1000);
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();

// Console.WriteLine($"Thread Count before processing: {Process.GetCurrentProcess().Threads.Count}");
queue.StartProcess();

Thread.Sleep(500);
// Console.WriteLine($"Thread Count after 500ms: {Process.GetCurrentProcess().Threads.Count}");

countdown.Wait();

queue.StopProcess();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

Console.WriteLine($"Total time for 50 jobs: {timeElapsed.TotalMilliseconds} ms");
*/

/*
JobQueue queue = new JobQueue();
int count = 0;

queue.Submit(() => Task.Delay(10));

queue.Submit(() => count++);

queue.Submit(async () =>
{
    await Task.Delay(1000);
    throw new Exception("Test exception");
});

queue.StartProcess();
Thread.Sleep(2000);
queue.StopProcess();
*/

/*
// Experiment - X
CountdownEvent countdown = new CountdownEvent(50);

long start = Stopwatch.GetTimestamp();
for (int i = 0; i < 50; i++)
{
    Thread thread = new Thread(() =>
    {
        Task.Delay(1000).GetAwaiter().GetResult();
        countdown.Signal();
    });
    thread.Start();
}

countdown.Wait();
TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

Console.WriteLine($"Total time for 50 threads for Experiment X: {timeElapsed.TotalMilliseconds} ms");
*/

/*
// Experiment - Y
JobQueue queue = new JobQueue(50);
CountdownEvent countdown = new CountdownEvent(50);

for (int i = 0; i < 50; i++)
{
    queue.Submit(async () =>
    {
        await Task.Delay(1000);
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();

queue.StartProcess();

Thread.Sleep(500);

countdown.Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

queue.StopProcess();

TimeSpan timeElapsedforStop = Stopwatch.GetElapsedTime(start);

Console.WriteLine($"Total time for 50 jobs before stop: {timeElapsed.TotalMilliseconds} ms");
Console.WriteLine($"Total time for 50 jobs after stop: {timeElapsedforStop.TotalMilliseconds} ms");
*/

/*
// Test for async jobs with exception handling
int jobs = 1000;
JobQueue queue = new JobQueue(jobs);
CountdownEvent countdown = new CountdownEvent(jobs);

for (int i = 0; i < jobs; i++)
{
    queue.Submit(async () =>
    {
        await Task.Delay(1000);
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();

Console.WriteLine("Thread count before processing: {0}", Process.GetCurrentProcess().Threads.Count);

queue.StartProcess();

countdown.Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

Console.WriteLine("Total time for {0} async jobs: {1} ms", jobs, timeElapsed.TotalMilliseconds);
Console.WriteLine("Thread count after processing: {0}", Process.GetCurrentProcess().Threads.Count);

queue.StopProcess();
*/

//ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
//Console.WriteLine($"ThreadPool.GetMinThreads: Worker Threads = {workerThreads}, Completion Port Threads = {completionPortThreads}");

/*
// Test with 50 workers, 50 sync jobs of Thread.Sleep(1000)
int jobs = 50;
int workers = 50;
Console.WriteLine($"Test with 50 workers, 50 sync jobs of Thread.Sleep(1000)");

JobQueue queue = new JobQueue(workers);
CountdownEvent countdown = new CountdownEvent(jobs);

for (int i = 0; i < jobs; i++)
{
    queue.Submit(() =>
    {
        Thread.Sleep(1000);
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();

queue.StartProcess();

countdown.Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

queue.StopProcess();

Console.WriteLine($"Total time for {jobs} sync jobs with {workers} workers: {timeElapsed.TotalMilliseconds} ms");

// Test with 50 workers, 50 sync jobs of Thread.Sleep(1000) after ThreadPool.SetMinThreads(100, 100)

Console.WriteLine($"Test with 50 workers, 50 sync jobs of Thread.Sleep(1000) after ThreadPool.SetMinThreads(100, 100)");

ThreadPool.SetMinThreads(100, 100);

JobQueue queue2 = new JobQueue(workers);

CountdownEvent countdown2 = new CountdownEvent(jobs);

for (int i = 0; i < jobs; i++)
{
    queue2.Submit(() =>
    {
        Thread.Sleep(1000);
        countdown2.Signal();
    });
}

long start2 = Stopwatch.GetTimestamp();

queue2.StartProcess();

countdown2.Wait();

TimeSpan timeElapsed2 = Stopwatch.GetElapsedTime(start2);

queue2.StopProcess();

Console.WriteLine($"Total time for {jobs} sync jobs with {workers} workers after ThreadPool.SetMinThreads(100, 100): {timeElapsed2.TotalMilliseconds} ms");
*/

//Test with 50 workers, 50 async jobs of Task.Delay(1000)

/*
Console.WriteLine("Test with 50 workers, 50 async jobs of Task.Delay(1000)");
var jobs = 50;
var workers = 50;

JobQueue queue = new JobQueue(workers);

CountdownEvent countdown = new CountdownEvent(jobs);

for (int i = 0; i < jobs; i++)
{
    queue.Submit(async () =>
    {
        await Task.Delay(1000);
        countdown.Signal();
    });
}

long start = Stopwatch.GetTimestamp();

queue.StartProcess();

countdown.Wait();

TimeSpan timeElapsed = Stopwatch.GetElapsedTime(start);

queue.StopProcess();

Console.WriteLine($"Total time for {jobs} async jobs with {workers} workers: {timeElapsed.TotalMilliseconds} ms");
*/

JobQueue queue = new JobQueue();

queue.Submit(() =>
{
    Console.WriteLine("Job 1");
});

queue.Submit(() =>
{
    throw new Exception("Job 2 exception");
});

queue.Submit(() =>
{
    Console.WriteLine("Job 3");
});

queue.StartProcess();
Thread.Sleep(1000);
queue.StopProcess();
