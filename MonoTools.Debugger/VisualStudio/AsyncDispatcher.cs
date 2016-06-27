using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonoTools.Debugger.VisualStudio
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
            try
            {
                foreach (Action action in actions.GetConsumingEnumerable(cts.Token))
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
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