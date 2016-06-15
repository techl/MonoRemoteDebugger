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
		private readonly Socket socket;
		public NetworkStream Stream;
		public string RootPath;
		public bool Compressed = false;
		public bool IsLocal = false;

		public TcpCommunication(Socket socket, string rootPath = null, bool compressed = false, bool local = false) {
			this.socket = socket;
			serializer = new BinaryFormatter();
			serializer.AssemblyFormat = FormatterAssemblyStyle.Simple;
			serializer.TypeFormat = FormatterTypeStyle.TypesAlways;
			serializer.Binder = new SimpleDeserializationBinder();
			Stream = new NetworkStream(socket);
			Compressed = compressed;
			IsLocal = local;
			RootPath = rootPath;
		}

		public bool IsConnected {
			get { return socket.IsSocketConnected(); }
		}

		public virtual void Send(Message msg) {
			serializer.Serialize(Stream, msg);
			if (msg is IExtendedMessage) {
				((IExtendedMessage)msg).Send(this);
			}
		}


		public virtual Message Receive() {
			var msg = (Message)serializer.Deserialize(Stream);
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