using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MessagePassingExample
{
    class Program
    {
        private ResourceManager resource = new ResourceManager(); // should be a singleton/static class...
        private BlockingCollection<Action<ResourceManager>> queue = new BlockingCollection<Action<ResourceManager>>();
        public void Start() => Task.Run(ThreadMain);
        public Task<T> Enqueue<T>(Func<ResourceManager, T> method)
        {
            var tcs = new TaskCompletionSource<T>();
            queue.Add(() => tcs.SetResult(method()));
            return tcs.Task;
        }
        private void ThreadMain()
        {
            foreach (var action in queue.GetConsumingEnumerable())
            {
                action(resource);
            }
        }

        static void Main(string[] args)
        {
            Program prog = new Program();
            prog.Start();

            // TODO : spawn threads here that will communicate with the thread managing the resource
            // using  prog.Enqueue(...)
            //Thread webServerThread = new Thread(() => ...);
            //webServerThread.Start();
            //webServerThread.Join();
        }
    }
}
