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
            queue.Add(() => tcs.SetResult(method()));
            return tcs.Task;
        }
        private void ThreadMain()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                foreach (var action in queue.GetConsumingEnumerable())
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
            prog.Start();

            prog.resource.ResourceStatusUpdated += OnResourceStatusUpdated;

            // TODO : spawn threads here that will communicate with the thread managing the resource
            // using  prog.Enqueue(...)
            //Thread webServerThread = new Thread(() => ...);
            //webServerThread.Start();
            //webServerThread.Join();
        }

        static void OnResourceStatusUpdated(object sender, ResourceStatusEventArgs e)
        {
            Console.WriteLine("Resource status has been updated at {0}: {1}.", e.Timestamp, e.SomeValue);
        }
    }
}
