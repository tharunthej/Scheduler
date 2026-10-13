using Scheduler;

Console.WriteLine("Hello, World!");

// CountdownEvent countdown = new CountdownEvent(100);

int count = 0;

JobQueue queue = new JobQueue(2);
Thread[] threads = new Thread[4];

//for (int j=0; j< 4; j++)
//{
//    Thread thread = new Thread(() =>
//    {
//        for (int i = 0; i < 25; i++)
//        {
//            queue.Submit(() =>
//            {
//                count++;
//                countdown.Signal();
//            });
//        }
//    });
//    threads[j] = thread;
//    thread.Start();
//}

//foreach (Thread thread in threads) 
//{
//    thread.Join();
//}

queue.Submit(() => { Console.WriteLine($"Job 1 on {Environment.CurrentManagedThreadId}"); });
queue.Submit(() => { Console.WriteLine($"Job 2 on {Environment.CurrentManagedThreadId}"); });
queue.Submit(() => { Console.WriteLine($"Job 3 on {Environment.CurrentManagedThreadId}"); });
queue.Submit(() => { Console.WriteLine($"Job 4 on {Environment.CurrentManagedThreadId}"); });

// Stop First before Start
// queue.StopProcess();

// First Start
queue.StartProcess();

Thread.Sleep(500);

//Second Start
// queue.StartProcess();

// countdown.Wait();

// Console.WriteLine(count);

queue.StopProcess();

Console.ReadLine();