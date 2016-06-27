using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
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
		public int DebuggerPort;


		public ClientSession(Socket socket, bool local = false, int debuggerPort = MonoDebugServer.DefaultDebuggerPort) {
			IsLocal = local;
			DebuggerPort = debuggerPort;
			remoteEndpoint = ((IPEndPoint)socket.RemoteEndPoint).Address;
			communication = new TcpCommunication(socket, rootPath, true, local);
		}

		public void HandleSession() {
			try {
				logger.Trace("New Session from {0}", remoteEndpoint);

				while (communication.IsConnected) {
					if (process != null && process.HasExited)
						return;

					while (communication.socket.Available > 4) System.Threading.Thread.Sleep(0);

					logger.Trace("Receiving content");
					var msg = communication.Receive<CommandMessage>();

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

			if (!Directory.Exists(msg.RootPath)) Directory.CreateDirectory(msg.RootPath);

			logger.Trace("Extracted content to {1}", remoteEndpoint, msg.RootPath);

			if (!msg.HasMdbs) {
				var generator = new Pdb2MdbGenerator();
				string binaryDirectory = msg.ApplicationType == ApplicationTypes.DesktopApplication ? msg.RootPath : Path.Combine(msg.RootPath, "bin");
				generator.GeneratePdb2Mdb(binaryDirectory);
			}

			StartMono(msg.ApplicationType, msg.Framework, msg.Arguments, msg.WorkingDirectory, msg.Url);
		}

		private void StartMono(ApplicationTypes type, Frameworks framework, string arguments, string workingDirectory, string url) {
			MonoProcess proc = MonoProcess.Start(type, targetExe, framework, arguments, url);
			proc.DebuggerPort = DebuggerPort;
			workingDirectory = string.IsNullOrEmpty(workingDirectory) ? rootPath : workingDirectory;
			proc.ProcessStarted += MonoProcessStarted;
			process = proc.Start(workingDirectory);
			logger.Info($"{proc.GetType().Name} started: \"{proc.process.StartInfo.FileName}\" {proc.process.StartInfo.Arguments}");
			process.EnableRaisingEvents = true;
			process.Exited += MonoExited;
			process.ErrorDataReceived += SendOutput;
			process.OutputDataReceived += SendOutput;
			process.BeginOutputReadLine();
		}

		private void SendOutput(object sender, DataReceivedEventArgs data) {
			if (data.Data != null) lock (communication) communication.Send(new ConsoleOutputMessage() { Text = data.Data });
		}

		private void MonoProcessStarted(object sender, EventArgs e) {
			var web = sender as MonoWebProcess;
			if (web != null) Process.Start(web.Url);
		}

		private void MonoExited(object sender, EventArgs e) {
			logger.Info("Program closed: " + process.ExitCode);
			try {
				Directory.Delete(rootPath, true);
			} catch (Exception ex) {
				logger.Trace("Cant delete {0} - {1}", rootPath, ex.Message);
			}
		}
	}
}