using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace MonoTools.Debugger.Library {

	public enum StreamModes { Read, Write };

	public class StreamWrapper : Stream {

		public StreamWrapper(Stream baseStream, long length) : base() {
			BaseStream = baseStream;
			this.length = length;
		}

		public Stream BaseStream;

		long position = 0;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanTimeout => true;
		public override bool CanWrite => false;
		long length = 0;
		public override long Length { get { return length; } }
		public override long Position {
			get { lock (this) return position; }
			set { throw new NotSupportedException(); }
		}
		public override void Close() {
			Flush();
			BaseStream.Close();
			Dispose(false);
		}

		// asnyc methods
		public class ReadAsyncResult : AsyncResult<int> {

			public ReadAsyncResult(StreamWrapper stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(callback, state) {
				if (stream.position+count >= stream.length) count = (int)(stream.length - stream.position);
				if (count <= 0) SetAsCompleted(null, true);
				else {
					Monitor.Enter(stream);
					stream.BaseStream.BeginRead(buffer, offset, count, res => {
						try {
							var m = stream.BaseStream.EndRead(res);
							stream.position += m;
							Monitor.Exit(stream);
							SetAsCompleted(m, res.CompletedSynchronously);
						} catch (Exception ex) {
							Monitor.Exit(stream);
							SetAsCompleted(ex, res.CompletedSynchronously);
						}
					}, state);
				}
			}
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			return new ReadAsyncResult(this, buffer, offset, count, callback, state);
		}
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			throw new NotSupportedException();
		}

		public override int EndRead(IAsyncResult asyncResult) {
			return ((ReadAsyncResult)asyncResult).EndInvoke();
		}

		public override void EndWrite(IAsyncResult asyncResult) {
			throw new NotSupportedException();
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count) {
			if (position+count >= length) count = (int)(length - position);
			if (count <= 0) return 0;
			else {
				lock (this) {
					var m = BaseStream.Read(buffer, offset, count);
					position += m;
					return m;
				}
			}
		}

		public override int ReadByte() {
			if (position+1 >= length) return -1;
			lock (this) {
				var b = BaseStream.ReadByte();
				if (b >= 0) position++;
				return b;
			}
		}

		public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
		public override void WriteByte(byte value) { throw new NotSupportedException(); }
		protected override void Dispose(bool disposing) { }
		public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
		public override void SetLength(long value) { throw new NotSupportedException(); }
		public override int ReadTimeout {
			get { return BaseStream.ReadTimeout; }
			set { BaseStream.ReadTimeout = value; }
		}
	}
}