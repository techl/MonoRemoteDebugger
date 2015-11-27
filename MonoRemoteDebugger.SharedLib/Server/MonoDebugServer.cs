using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    public class MonoDebugServer : IDisposable
    {
        public const int TcpPort = 13001;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        private Task listeningTask;
        private TcpListener tcp;

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            tcp = new TcpListener(IPAddress.Any, TcpPort);
            tcp.Start();
            listeningTask = Task.Factory.StartNew(() => StartListening(cts.Token), cts.Token);
        }

        private void StartListening(CancellationToken token)
        {
            while (true)
            {
                logger.Info("Waiting for client");
                TcpClient client = tcp.AcceptTcpClient();
                token.ThrowIfCancellationRequested();

                logger.Info("Accepted client: " + client.Client.RemoteEndPoint);
                var clientSession = new ClientSession(client.Client);
                Task.Factory.StartNew(clientSession.HandleSession, token);
            }
        }

        public void Stop()
        {
            cts.Cancel();
            if (tcp != null && tcp.Server != null)
            {
                tcp.Server.Close(0);
                tcp = null;
            }
            if (listeningTask != null)
                Task.WaitAll(listeningTask);
            logger.Info("Closed MonoDebugServer");
        }

        public void StartAnnouncing()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    CancellationToken token = cts.Token;
                    logger.Trace("Start announcing");
                    using (var client = new UdpClient())
                    {
                        var ip = new IPEndPoint(IPAddress.Broadcast, 15000);
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

                        while (true)
                        {
                            token.ThrowIfCancellationRequested();
                            byte[] bytes = Encoding.ASCII.GetBytes("MonoServer");
                            client.Send(bytes, bytes.Length, ip);
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    logger.Trace(ex);
                }
            });
        }

        public void WaitForExit()
        {
            listeningTask.Wait();
        }
    }
}