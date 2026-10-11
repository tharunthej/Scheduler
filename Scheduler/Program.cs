using Scheduler;

Console.WriteLine("Hello, World!");

CountdownEvent countdown = new CountdownEvent(100);

int count = 0;

JobQueue queue = new JobQueue();
Thread[] threads = new Thread[4];

for (int j=0; j< 4; j++)
{
    Thread thread = new Thread(() =>
    {
        for (int i = 0; i < 25; i++)
        {
            queue.Submit(() =>
            {
                count++;
                countdown.Signal();
            });
        }
    });
    threads[j] = thread;
    thread.Start();
}

foreach (Thread thread in threads) 
{
    thread.Join();
}

queue.StartProcess();

countdown.Wait();

Console.WriteLine(count);

queue.StopProcess();