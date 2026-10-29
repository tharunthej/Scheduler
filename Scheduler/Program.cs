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

