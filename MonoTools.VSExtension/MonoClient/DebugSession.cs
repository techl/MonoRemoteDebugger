using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonoTools.Debugger.Contracts;
using MonoTools.Debugger.Library;

namespace MonoTools.Debugger.VSExtension.MonoClient {

	public class DebugSession : IDebugSession {

		private readonly TimeSpan Delay = TimeSpan.FromMinutes(5);
		private readonly TcpCommunication communication;
		private readonly ApplicationTypes type;
		string rootPath;
		public bool IsLocal = false;
		Frameworks framework;

		public DebugSession(DebugClient debugClient, ApplicationTypes type, Socket socket, string rootPath, Frameworks framework, bool compress = false, bool local = false) {
			Client = debugClient;
			this.type = type;
			IsLocal = local;
			this.rootPath = rootPath;
			this.framework = framework;
			communication = new TcpCommunication(socket, rootPath, compress, local);
		}

		public DebugClient Client { get; private set; }

		public void Disconnect() {
			communication.Disconnect();
		}

		public void TransferFiles() {
			var info = new DirectoryInfo(Client.OutputDirectory);
			if (!info.Exists)
				throw new DirectoryNotFoundException("Directory not found");

			var msg = new DebugMessage() {
				Command = Commands.DebugContent,
				ApplicationType = type,
				Framework = framework,
				Executable = Client.TargetExe,
				Arguments = Client.Arguments,
				WorkingDirectory = Client.WorkingDirectory,
				Url = Client.Url,
				RootPath = rootPath,
				IsLocal = IsLocal,
				LocalPath = Client.OutputDirectory
			};
			if (!IsLocal) msg.Files.AddFolder(rootPath);

			communication.Send(msg);

			Console.WriteLine("Finished transmitting");
		}

		public async Task TransferFilesAsync() {
			await Task.Run(() => TransferFiles());
		}

		public async Task<Message> WaitForAnswerAsync() {
			Task delay = Task.Delay(Delay);
			Task res = await Task.WhenAny(communication.ReceiveAsync(), delay);

			if (res is Task<Message>) return ((Task<Message>)res).Result;

			if (res == delay) throw new Exception("Did not receive an answer in time...");
			throw new Exception("Cant start debugging");
		}
	}
}