using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace MonoTools.Debugger.Library {

	public enum StreamModes { Read, Write };

	public class BufferedStreamWrapper : Stream {

		const int M = 1024;

		public BufferedStreamWrapper(Stream baseStream, StreamModes mode, long start = 0, long length = -1): base() {
			BaseStream = baseStream;
			Mode = mode;
			Start = start;
			if (length != -1) this.length = length;
		}

		public StreamModes Mode;
		public Stream BaseStream;
		bool eof = false;


		int bufferSize = M;
		public int BufferSize {
			get { return bufferSize; }
			set {
				if (bufferSize != value) {
					lock (this) {
						if (Mode == StreamModes.Write) Flush();
						else n = 0;
						bufferSize = value;
						buffer = new byte[bufferSize];
					}
				}
			}
		}
		int p = 0;
		int n = 0;
		byte[] buffer = new byte[M];

		public virtual void OnWrite(byte[] buffer, int offset, int count) { }
		public virtual void OnRead(byte[] buffer, int offset, int count) { }

		public override bool CanRead => Mode == StreamModes.Read;
		public override bool CanSeek => BaseStream.CanSeek;
		public override bool CanTimeout => false;
		public override bool CanWrite => Mode == StreamModes.Write;
		long? length = null;
		public override long Length { get { lock (this) return length ?? BaseStream.Length; } }
		public virtual long Start { get; set; } = 0;
		public override long Position {
			get { lock (this) return BaseStream.Position-n-Start; }
			set {
				if (!CanSeek) throw new NotSupportedException();
				else {
					lock (this) {
						if (Mode == StreamModes.Write) Flush();
						else n = 0;
						BaseStream.Position = value+Start;
					}
				}
			}
		}
		public override void Close() {
			Flush();
			BaseStream.Close();
			Dispose(false);
		}

		// asnyc methods
		public class ReadAsyncResult : AsyncResult<int> {

			public ReadAsyncResult(BufferedStreamWrapper stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(callback, state) {
				if (offset+count > buffer.Length) count = buffer.Length - offset;
				if (count <= 0) SetAsCompleted(null, true);
				else {
					Monitor.Enter(stream);
					int n, p = stream.p, m, M = stream.BufferSize;
					if (count <= stream.n) { // count smaller than buffer size, only read from buffer
						n = count;
						count = 0;
					} else { // read buffer and reduce count
						n = stream.n;
						count -= n;
					}
					// read buffer
					//for (int i = 0; i < n; i++) buffer[offset+i] = stream.buffer[p+i];
					Array.Copy(stream.buffer, p, buffer, offset, n);
					stream.n -= n;
					stream.p = p+n;
					offset += n;
					if (count == 0) { // fill buffer from base stream
						if (stream.n == 0 && !stream.eof) {
							stream.BaseStream.BeginRead(stream.buffer, 0, M, res => {
								m = stream.BaseStream.EndRead(res);
								stream.eof = m < M;
								stream.OnRead(stream.buffer, 0, m);
								stream.n = m;
								stream.p = 0;
								Monitor.Exit(stream);
								SetAsCompleted(n, res.CompletedSynchronously);
							}, state);
						}
						Monitor.Exit(stream);
						SetAsCompleted(n, true);
					} else {
						if (count > M) { // read directly from BaseStream
							if (!stream.eof) {
								stream.BaseStream.BeginRead(buffer, offset, count, res => {
									try {
										m = stream.BaseStream.EndRead(res);
										stream.eof = m < count;
										stream.OnRead(buffer, offset, m);
										Monitor.Exit(stream);
										SetAsCompleted(n+m, res.CompletedSynchronously);
									} catch (Exception ex) {
										Monitor.Exit(stream);
										SetAsCompleted(ex, res.CompletedSynchronously);
									}
								}, state);
							} else {
								Monitor.Exit(stream);
								SetAsCompleted(0, true);
							}
						} else { // read into buffer
							if (!stream.eof) {
								stream.BaseStream.BeginRead(stream.buffer, 0, M, res => {
									try {
										stream.n = m = stream.BaseStream.EndRead(res);
										stream.eof = m < M;
										stream.OnRead(stream.buffer, 0, m);
										if (count > m) count = m;
										// copy from buffer
										//for (int i = 0; i < count; i++) buffer[offset+i] = stream.buffer[i];
										Array.Copy(stream.buffer, 0, buffer, offset, count);
										stream.p = count;
										Monitor.Exit(stream);
										SetAsCompleted(n+count, res.CompletedSynchronously);
									} catch (Exception ex) {
										Monitor.Exit(stream);
										SetAsCompleted(ex, res.CompletedSynchronously);
									}
								}, state);
							} else {
								Monitor.Exit(stream);
								SetAsCompleted(0, true);
							}
						}
					}
				}
			}
		}

		public class WriteAsyncResult : AsyncResultNoResult {

			bool sync = true;

			public WriteAsyncResult(BufferedStreamWrapper stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(callback, state) {
				if (offset+count > buffer.Length) count = buffer.Length - offset;
				if (count <= 0) SetAsCompleted(null, true);
				else {
					Monitor.Enter(stream);
					int n, m = stream.n;
					int M = stream.BufferSize;
					if (count <= M-m) { // job fits into buffer
						n = count;
						count = 0;
						//for (int i = 0; i < n; i++) stream.buffer[m+i] = buffer[offset+i];
						Array.Copy(buffer, offset, stream.buffer, m, n);
						stream.n += n;
						offset += n;
						Monitor.Exit(stream);
						SetAsCompleted(null, true);
					} else {
						if (m > 0) { // write buffer to base stream
							stream.OnWrite(stream.buffer, 0, m);
							stream.BaseStream.BeginWrite(stream.buffer, 0, m, res => {
								try {
									stream.BaseStream.EndWrite(res);
									stream.n = 0;
									sync &= res.CompletedSynchronously;
									stream.OnWrite(buffer, offset, count);
									stream.BaseStream.BeginWrite(buffer, offset, count, res2 => { // write job to base stream
										try {
											stream.BaseStream.EndWrite(res2);
											sync &= res.CompletedSynchronously;
											Monitor.Exit(stream);
											SetAsCompleted(null, sync);
										} catch (Exception ex) {
											Monitor.Exit(stream);
											SetAsCompleted(ex, sync);
										}
									}, state);
									Monitor.Exit(stream);
									SetAsCompleted(null, sync);
								} catch (Exception ex) {
									Monitor.Exit(stream);
									SetAsCompleted(ex, sync);
								}
							}, state);
						} else {
							stream.OnWrite(buffer, offset, count);
							stream.BaseStream.BeginWrite(buffer, offset, count, res => { // write job to base stream
								try {
									stream.BaseStream.EndWrite(res);
									sync &= res.CompletedSynchronously;
									Monitor.Exit(stream);
									SetAsCompleted(null, sync);
								} catch (Exception ex) {
									Monitor.Exit(stream);
									SetAsCompleted(ex, sync);
								}
							}, state);
						}
					}
				}
			}
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			return new ReadAsyncResult(this, buffer, offset, count, callback, state);
		}
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			return new WriteAsyncResult(this, buffer, offset, count, callback, state);
		}

		public override int EndRead(IAsyncResult asyncResult) {
			return ((ReadAsyncResult)asyncResult).EndInvoke();
		}

		public override void EndWrite(IAsyncResult asyncResult) {
			((WriteAsyncResult)asyncResult).EndInvoke();
		}

		public override void Flush() {
			lock (this) {
				if (Mode == StreamModes.Write && n > 0) {
					OnWrite(buffer, 0, n);
					BaseStream.Write(buffer, 0, n);
					n = 0;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count) {
			if (offset+count > buffer.Length) count = buffer.Length - offset;
			if (count <= 0) return 0;
			lock (this) {
				int m, l;
				if (count < n) m = count; // read all from buffer
				else m = n; // read n from buffer, reduce count
				// read from buffer
				//for (int i = 0; i < m; i++) buffer[offset+i] = this.buffer[p+i];
				Array.Copy(this.buffer, p, buffer, offset, m);
				n -= m;
				p += m;
				offset += m;
				count -= m;
				if (count == 0) {
					if (n == 0 && !eof) { // fill buffer from base stream
						n = BaseStream.Read(this.buffer, p, BufferSize);
						OnRead(this.buffer, p, n);
						eof = n < BufferSize;
					}
					return m;
				} else {
					if (count > BufferSize) { // read directly from base stream
						if (!eof) {
							l = BaseStream.Read(buffer, offset, count);
							OnRead(buffer, offset, count);
							eof = l < count;
						} else l = 0;
						return m+l;
					} else {
						if (!eof) {
							n = BaseStream.Read(this.buffer, 0, BufferSize); // read into buffer
							OnRead(this.buffer, 0, n);
							eof = n < BufferSize;
						}
						if (count > n) l = n;
						else l = count;
						//for (int i = 0; i < l; i++) buffer[offset+i] = this.buffer[i];
						Array.Copy(this.buffer, 0, buffer, offset, l);
						n -= l;
						p = l;
						return m+l;
					}
				}
			}
		}

		public override int ReadByte() {
			lock (this) {
				if (n > 0) {
					n--;
					return buffer[p++];
				} else {
					if (!eof) {
						n = BaseStream.Read(buffer, 0, BufferSize);
						OnRead(buffer, 0, n);
						eof = n < BufferSize;
						p = 0;
						n--;
						return buffer[p++];
					}
					return -1;
				}
			}
		}

		public override void Write(byte[] buffer, int offset, int count) {
			if (offset+count > buffer.Length) count = buffer.Length - offset;
			if (count <= 0) return;
			lock (this) {
				if (count < BufferSize-n) {
					//for (int i = 0; i < count; i++) this.buffer[n+i] = buffer[offset+i];
					Array.Copy(buffer, offset, this.buffer, n, count);
					n += count;
				} else {
					if (n > 0) {
						OnWrite(this.buffer, 0, n);
						BaseStream.Write(this.buffer, 0, n);
						n = 0;
					}
					OnWrite(buffer, offset, count);
					BaseStream.Write(buffer, offset, count);
				}
			}
		}

		public override void WriteByte(byte value) {
			lock (this) {
				if (n < BufferSize) {
					buffer[n++] = value;
				}
				if (n == BufferSize) {
					OnWrite(buffer, 0, BufferSize);
					BaseStream.Write(buffer, 0, BufferSize);
					n = 0;
				}
			}
		}

		protected override void Dispose(bool disposing) { }
		public override long Seek(long offset, SeekOrigin origin) {
			if (!CanSeek) throw new NotSupportedException();
			lock (this) {
				if (Mode == StreamModes.Write) Flush();
				else n = 0;
				if (origin == SeekOrigin.Begin) return BaseStream.Seek(Start+offset, origin);
				else return BaseStream.Seek(offset, origin);
			}
		}
		public override void SetLength(long value) { length = value; }

	}
}
