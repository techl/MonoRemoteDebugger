using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MonoTools.Debugger.Library {

	public class PipeQueue<T>: ConcurrentQueue<T>, IEnumerable<T>, IEnumerable, IDisposable {

		public TimeSpan StandardTimeout = TimeSpan.FromMinutes(10);

		public PipeQueue() { Closed = false; }

		public override T Dequeue() { return DequeueOrBlock(StandardTimeout.Milliseconds); }

		public bool Closed { get; set; }
		public void Close() {
			Closed = true;
			Signal.Set();
		}
	
		public void Dispose() { Close(); }

		public class QueueEnumerator: IEnumerator<T>, IEnumerator {
			PipeQueue<T> Queue;
			bool hasValue = false;
			T Value;

			public T Current { get { if (!hasValue) { Value = Queue.Dequeue(); hasValue = true; } return Value; } }
			public void Dispose() { }
			object IEnumerator.Current { get { if (!hasValue) { Value = Queue.Dequeue(); hasValue = true; } return Value; } }
			public bool MoveNext() { Value = Queue.Dequeue(); hasValue = true; return Queue.Closed; }
			public void Reset() { throw new NotSupportedException(); }
			bool IEnumerator.MoveNext() { Value = Queue.Dequeue(); hasValue = true; return Value != null || !Queue.Closed; }
			void IEnumerator.Reset() { throw new NotImplementedException(); }

			public QueueEnumerator(PipeQueue<T> queue) { Queue = queue; }
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() {	 return new QueueEnumerator(this); }
		IEnumerator IEnumerable.GetEnumerator() { return new QueueEnumerator(this); }
	}
}