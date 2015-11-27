using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonoRemoteDebugger.VSExtension.MonoClient
{
    public class MonoServerDiscovery
    {
        public async Task<MonoServerInformation> SearchServer(CancellationToken token)
        {
            using (var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 15000)))
            {
                Task result = await Task.WhenAny(udp.ReceiveAsync(), Task.Delay(500, token));
                var task = result as Task<UdpReceiveResult>;
                if (task != null)
                {
                    UdpReceiveResult udpResult = task.Result;
                    string msg = Encoding.Default.GetString(udpResult.Buffer);
                    return new MonoServerInformation {Message = msg, IpAddress = udpResult.RemoteEndPoint.Address};
                }

                return null;
            }
        }
    }
}