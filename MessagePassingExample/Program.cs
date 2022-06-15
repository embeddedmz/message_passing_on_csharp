using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePassingExample
{
    class Program
    {
        public Program()
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                // Tell .NET to not terminate the process
                e.Cancel = true;

                _cts.Cancel();
            };
        }

        public ResourceManager resource = new ResourceManager(); // should be a singleton/static class...
        public readonly BlockingCollection<Action<ResourceManager>> queue = new BlockingCollection<Action<ResourceManager>>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Random rnd = new Random();
        private Timer refreshStatusTimer;
        public Task Start() => Task.Run(ThreadMain);
        public Task<T> Enqueue<T>(Func<ResourceManager, T> method)
        {
            var tcs = new TaskCompletionSource<T>();
            queue.Add(r => tcs.SetResult(method(r)));
            return tcs.Task;
        }
        private void ThreadMain()
        {
            foreach (var action in queue.GetConsumingEnumerable())
            {
                try
                {
                    action(resource);
                }
                catch
                {
                    Console.WriteLine("[Error] Caught an exception in ThreadMain !");
                }
            }

            /*while (!_cts.Token.IsCancellationRequested)
            {
                while (queue.TryTake(out var action, 100))
                {
                    action(resource);
                }

                // notify the threads subscribed to 
                resource.UpdateStatus(rnd.Next(0, 100));

                Thread.Sleep(100);
            }*/
        }

        
        static void Main(string[] args)
        {
            Program prog = new Program();
            Task mainTask = prog.Start(); // start the thread managing the resource

            // subscribe to get the status of the resource
            prog.resource.ResourceStatusUpdated += OnResourceStatusUpdated;

            // spawn one thread here that will communicate with the thread managing the resource using prog.Enqueue(...)
            Thread webServerThread = new Thread(() => {
                Console.WriteLine("Starting web server thread !");

                int thisThreadId = Thread.CurrentThread.ManagedThreadId; // do not use this in the lambda otherwise you will get the resource manager thread id
                while (!prog._cts.Token.IsCancellationRequested)
                {
                    var currentTask = prog.Enqueue((ResourceManager r) => r.SendData(thisThreadId));
                    string res = currentTask.Result;
                    Console.WriteLine("[Thread {0}] Task result = {1}", Thread.CurrentThread.ManagedThreadId, res);
                    Thread.Sleep(250);
                }
            });
            webServerThread.Start();

            prog.refreshStatusTimer = new Timer((o) =>
            {
                prog.Enqueue((ResourceManager r) =>
                {
                    r.UpdateStatus(prog.rnd.Next(0, 100));
                    return true; // we can't enqueue lambdas that have void as a return type :\
                });
            }, null, 1000, 1000);

            // and use the main thread too
            int mainThreadId = Thread.CurrentThread.ManagedThreadId; // do not use this in the lambda otherwise you will get the resource manager thread id
            while (!prog._cts.Token.IsCancellationRequested)
            {
                var currentTask = prog.Enqueue((ResourceManager r) => r.SendData(mainThreadId));
                string res = currentTask.Result;
                Console.WriteLine("[Thread {0}] Task result = {1}", Thread.CurrentThread.ManagedThreadId, res);
                Thread.Sleep(250);
            }

            webServerThread.Join();

            prog.refreshStatusTimer.Change(Timeout.Infinite, Timeout.Infinite);
            prog.refreshStatusTimer.Dispose();
            prog.refreshStatusTimer = null;

            prog.queue.CompleteAdding();
            mainTask.Wait();
        }

        static ResourceStatusEventArgs s_lastResourceStatus;
        static readonly object s_lastResourceStatusLock = new object();
        static void OnResourceStatusUpdated(object sender, ResourceStatusEventArgs e)
        {
            // "e" members should be immutable (like strings) so that it remains coherent
            // in a multithreaded context as long as we don't change the values of the members
            // and we just use them
            Console.WriteLine("[Thread {0}] Resource status has been updated at {1}: {2}.",
                Thread.CurrentThread.ManagedThreadId, e.Timestamp, e.SomeValue);

            lock (s_lastResourceStatusLock)
            {
                s_lastResourceStatus = e;
            }
        }
    }
}
