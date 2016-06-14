using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonoTools.Debugger.Library;

namespace MonoTools.Debugger.VSExtension.MonoClient {

	public class DebugClient {
		private readonly ApplicationTypes type;
		bool IsLocal = false;
		Frameworks framework;
		public int MessagePort;
		public int DebuggerPort;
		public int DiscoveryPort;

		public DebugClient(ApplicationTypes type, string targetExe, string outputDirectory, bool local = false, Frameworks framework = Frameworks.Net4) {
			this.type = type;
			TargetExe = targetExe;
			OutputDirectory = outputDirectory;
			IsLocal = local;
			this.framework = framework;
		}

		public string TargetExe { get; set; }
		public string OutputDirectory { get; set; }
		public Frameworks Framework { get; set; } = Frameworks.Net4;
		public IPAddress CurrentServer { get; private set; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public string Url { get; set; }

		public async Task<DebugSession> ConnectToServerAsync(string ipAddressOrHostname, string ports = null) {

			IPAddress server;
			if (IPAddress.TryParse(ipAddressOrHostname, out server)) {
				CurrentServer = server;
			} else {
				IPAddress[] adresses = Dns.GetHostEntry(ipAddressOrHostname).AddressList;
				CurrentServer = adresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			}

			bool compress = TraceRoute.GetTraceRoute(CurrentServer.ToString()).Count() > 1;

			var tcp = new TcpClient();
			MonoDebugServer.ParsePorts(ports, out MessagePort, out DebuggerPort, out DiscoveryPort);

			await tcp.ConnectAsync(CurrentServer, MessagePort);
			return new DebugSession(this, type, tcp.Client, OutputDirectory, Framework, compress, IsLocal);
		}
	}
}