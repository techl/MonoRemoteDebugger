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

        public DebugClient(ApplicationType type, string targetExe, string arguments, string outputDirectory, string appHash)
        {
            _type = type;
            TargetExe = targetExe;
            Arguments = arguments;
            OutputDirectory = outputDirectory;
            AppHash = appHash;
        }

        public string TargetExe { get; set; }
        public string Arguments { get; set; }
        public string OutputDirectory { get; set; }
        public string AppHash { get; set; }
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