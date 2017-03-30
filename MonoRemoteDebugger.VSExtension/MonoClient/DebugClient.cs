using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;

namespace MonoRemoteDebugger.VSExtension.MonoClient
{
    public class DebugClient
    {
        private readonly ApplicationType _type;

        public DebugClient(ApplicationType type, string targetExe, string outputDirectory)
        {
            _type = type;
            TargetExe = targetExe;
            OutputDirectory = outputDirectory;
        }

        public string TargetExe { get; set; }
        public string OutputDirectory { get; set; }
        public IPAddress CurrentServer { get; private set; }

        public async Task<DebugSession> ConnectToServerAsync(string ipAddress)
        {
            CurrentServer = IPAddress.Parse(ipAddress);

            var tcp = new TcpClient();
            await tcp.ConnectAsync(CurrentServer, GlobalConfig.Current.ServerPort);
            return new DebugSession(this, _type, tcp.Client);
        }
    }
}