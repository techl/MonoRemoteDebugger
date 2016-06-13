using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using NLog;

namespace MonoTools.Debugger.Library {
	internal class ClientSession {
		private readonly TcpCommunication communication;
		private readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly IPAddress remoteEndpoint;
		private readonly string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MonoDebugger");
		private Process process;
		private string targetExe;
		public bool IsLocal = false;


		public ClientSession(Socket socket, bool local = false) {
			IsLocal = local;
			remoteEndpoint = ((IPEndPoint)socket.RemoteEndPoint).Address;
			communication = new TcpCommunication(socket, rootPath, local);
		}

		public void HandleSession() {
			try {
				logger.Trace("New Session from {0}", remoteEndpoint);

				while (communication.IsConnected) {
					if (process != null && process.HasExited)
						return;

					logger.Trace("Receiving content from {0}", remoteEndpoint);
					var msg = communication.Receive() as CommandMessage;

					switch (msg.Command) {
					case Commands.DebugContent:
						StartDebugging((DebugMessage)msg);
						communication.Send(new StatusMessage() { Command = Commands.StartedMono });
						break;
					case Commands.Shutdown:
						logger.Info("Shutdown-Message received");
						return;
					}
				}
			} catch (XmlException xmlException) {
				logger.Info("CommunicationError : " + xmlException);
			} catch (Exception ex) {
				logger.Error(ex);
			} finally {
				if (process != null && !process.HasExited)
					process.Kill();
			}
		}

		private void StartDebugging(DebugMessage msg) {

			targetExe = msg.Executable;

			foreach (string file in Directory.GetFiles(msg.Files.RootPath, "*vshost*")) File.Delete(file);

			logger.Trace("Extracted content from {0} to {1}", remoteEndpoint, msg.Files.RootPath);

			var generator = new Pdb2MdbGenerator();
			string binaryDirectory = msg.ApplicationType == ApplicationTypes.DesktopApplication ? msg.RootPath : Path.Combine(msg.RootPath, "bin");
			generator.GeneratePdb2Mdb(binaryDirectory);

			StartMono(msg.ApplicationType, msg.Framework);
		}

		private void StartMono(ApplicationTypes type, Frameworks framework) {
			MonoProcess proc = MonoProcess.Start(type, targetExe, framework);
			proc.ProcessStarted += MonoProcessStarted;
			process = proc.Start(rootPath);
			process.EnableRaisingEvents = true;
			process.Exited += MonoExited;
			process.OutputDataReceived += (sender, data) => {
				if (data.Data != null) communication.Send(new ConsoleOutputMessage() { Text = data.Data });
			};
			process.BeginOutputReadLine();
		}

		private void MonoProcessStarted(object sender, EventArgs e) {
			var web = sender as MonoWebProcess;
			if (web != null) Process.Start(web.Url);
		}

		private void MonoExited(object sender, EventArgs e) {
			Console.WriteLine("Program closed: " + process.ExitCode);
			try {
				Directory.Delete(rootPath, true);
			} catch (Exception ex) {
				logger.Trace("Cant delete {0} - {1}", rootPath, ex.Message);
			}
		}
	}
}