using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoTools.Debugger.Library {

	public class TcpCommunication {
		private readonly BinaryFormatter serializer;
		public readonly Socket socket;
		public NetworkStream Stream;
		public string RootPath;
		public bool Compressed = false;
		public bool IsLocal = false;
		public static PipeQueue<Message> queue = new PipeQueue<Message>();
		public BinaryWriter writer;
		public BinaryReader reader;

		public TcpCommunication(Socket socket, string rootPath, bool compressed, bool local) {
			this.socket = socket;
			if (!OS.IsMono) {
				socket.ReceiveTimeout = -1;
				socket.SendTimeout = -1;
			}
			serializer = new BinaryFormatter();
			serializer.AssemblyFormat = FormatterAssemblyStyle.Simple;
			serializer.TypeFormat = FormatterTypeStyle.TypesAlways;
			serializer.Binder = new SimpleDeserializationBinder();
			Compressed = compressed;
			IsLocal = local;
			RootPath = rootPath;
			Stream = new NetworkStream(socket, FileAccess.ReadWrite, false);
			writer = new BinaryWriter(Stream);
			reader = new BinaryReader(Stream);
		}

		public bool IsConnected {
			get { return IsLocal || socket.IsSocketConnected(); }
		}

		public virtual void Send(Message msg) {
			if (IsLocal) queue.Enqueue(msg);
			else {
				var m = new MemoryStream();
				serializer.Serialize(m, msg);
				writer.Write((Int32)m.Length);
				writer.Write(m.ToArray());
				if (msg is IExtendedMessage) {
					((IExtendedMessage)msg).Send(this);
				}
			}
		}


		public virtual Message Receive() {
			if (IsLocal) return queue.Dequeue();
			var len = reader.ReadInt32();
			var buf = new byte[len];
			reader.Read(buf, 0, len);
			var m = new MemoryStream(buf); 
			var msg = (Message)serializer.Deserialize(m);
			if (msg is IExtendedMessage) {
				if (msg is IMessageWithFiles) ((IMessageWithFiles)msg).Files.RootPath = RootPath;
				((IExtendedMessage)msg).Receive(this);
			}
			return msg;
		}
		public T Receive<T>() where T : Message, new() => Receive() as T;

		public async void SendAsync(Message msg) {
			await Task.Run(() => Send(msg));
		}

		public async Task<Message> ReceiveAsync() {
			return await Task.Run(() => Receive());
		}
		public async Task<T> ReceiveAsync<T>() where T : Message, new() {
			return await Task.Run(() => Receive<T>());
		}

		public void Disconnect() {
			if (socket != null) {
				socket.Close(1);
				socket.Dispose();
			}
		}
	}

	sealed class SimpleDeserializationBinder : SerializationBinder {
		public override Type BindToType(string assemblyName, string typeName) {
			var assembly = Assembly.Load(assemblyName);
			return assembly.GetType(typeName);
		}
	}

}