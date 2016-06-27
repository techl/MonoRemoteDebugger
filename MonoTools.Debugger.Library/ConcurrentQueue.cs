using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;

namespace MonoTools.Debugger.Library {

	public class ConcurrentQueue<T>: System.Collections.Generic.Queue<T> {

		protected AutoResetEvent Signal = new AutoResetEvent(false);

		public new virtual void Enqueue(T entry) {
			lock (this) {
				base.Enqueue(entry);
			}
			Signal.Set();
		}

		public new virtual T Dequeue() {
			lock (this) {
				if (base.Count > 0) return base.Dequeue();
				else return default(T);
			}
		}
	
		public event EventHandler Blocking;
		public event EventHandler Blocked;

		public virtual T DequeueOrBlock(int Timeout = -1) {
			do {
				lock (this) {
					if (base.Count > 0) return base.Dequeue();
				}
				OnBlocking();
				Signal.WaitOne(Timeout);
				OnBlocked();
			} while (true);
		}

		protected void OnBlocking() { if (Blocking != null) Blocking(this, EventArgs.Empty); }
		protected void OnBlocked() {if (Blocked != null) Blocked(this, EventArgs.Empty); }

		public bool IsEmpty { get { lock (this) return base.Count == 0; } }
		public new int Count { get { lock (this) return base.Count; } }
	}
}
