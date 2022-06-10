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
        private BlockingCollection<Action<ResourceManager>> queue = new BlockingCollection<Action<ResourceManager>>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Random rnd = new Random();
        public void Start() => Task.Run(ThreadMain);
        public Task<T> Enqueue<T>(Func<ResourceManager, T> method)
        {
            var tcs = new TaskCompletionSource<T>();
            queue.Add(r => tcs.SetResult(method(r)));
            return tcs.Task;
        }
        private void ThreadMain()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                while (queue.TryTake(out var action, 100))
                {
                    action(resource);
                }

                // notify the threads subscribed to 
                resource.UpdateStatus(rnd.Next(0, 100));

                Thread.Sleep(100);
            }
        }

        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.Start(); // start the thread managing the resource

            // subscribe to get the status of the resource and print it using the main thread (or another thread eventually)
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
        }

        static void OnResourceStatusUpdated(object sender, ResourceStatusEventArgs e)
        {
            // "e" members should be immutable (like strings) so that it remains coherent
            // in a multithreaded context as long as we don't change the values of the members
            // and we just use them
            Console.WriteLine("Resource status has been updated at {0}: {1}.", e.Timestamp, e.SomeValue);
        }
    }
}
