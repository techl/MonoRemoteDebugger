using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

namespace MonoTools.Debugger.Library {

	public class TcpCommunication {
		private readonly BinaryFormatter serializer;
		private readonly Socket socket;
		public NetworkStream Stream;
		private string rootPath;
		public bool Compressed = false;
		public bool IsLocal = false;

		public TcpCommunication(Socket socket, string rootPath = null, bool compressed = false, bool local = false) {
			this.socket = socket;
			serializer = new BinaryFormatter();
			Stream = new NetworkStream(socket);
			Compressed = compressed;
			IsLocal = local;
			this.rootPath = rootPath;
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
				if (msg is IMessageWithFiles) ((IMessageWithFiles)msg).Files.RootPath = rootPath;
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
}