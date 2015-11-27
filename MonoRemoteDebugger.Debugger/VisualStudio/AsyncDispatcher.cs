using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class AsyncDispatcher
    {
        private readonly BlockingCollection<Action> actions = new BlockingCollection<Action>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public AsyncDispatcher()
        {
            Task.Factory.StartNew(Run);
        }

        private void Run()
        {
            foreach (Action action in actions.GetConsumingEnumerable(cts.Token))
            {
                action();
            }
        }

        public void Queue(Action action)
        {
            actions.Add(action);
        }

        internal void Stop()
        {
            cts.Cancel();
        }
    }
}