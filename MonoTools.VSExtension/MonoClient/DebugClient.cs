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

		public async Task<DebugSession> ConnectToServerAsync(string ipAddressOrHostname) {

			IPAddress server;
			if (IPAddress.TryParse(ipAddressOrHostname, out server)) {
				CurrentServer = server;
			} else {
				CurrentServer = Dns.GetHostByName(ipAddressOrHostname).AddressList.FirstOrDefault();
			}

			bool compress = TraceRoute.GetTraceRoute(CurrentServer.ToString()).Count() > 1;

			var tcp = new TcpClient();
			await tcp.ConnectAsync(CurrentServer, MonoDebugServer.TcpPort);
			return new DebugSession(this, type, tcp.Client, OutputDirectory, Framework, compress, IsLocal);
		}
	}
}