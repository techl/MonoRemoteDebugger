using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using MonoRemoteDebugger.VSExtension.MonoClient;
using MonoRemoteDebugger.VSExtension.Settings;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;

namespace MonoRemoteDebugger.VSExtension.Views
{
    public class ServersFoundViewModel : IDisposable
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public ServersFoundViewModel()
        {
            Servers = new ObservableCollection<MonoServerInformation>();
            UserSettings settings = UserSettingsManager.Instance.Load();
            ManualIp = settings.LastIp;
            LookupServers(cts.Token);
        }

        public ObservableCollection<MonoServerInformation> Servers { get; set; }
        public MonoServerInformation SelectedServer { get; set; }
        public string ManualIp { get; set; }

        private async void LookupServers(CancellationToken token)
        {
            var discovery = new MonoServerDiscovery();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    MonoServerInformation server = await discovery.SearchServer(token);
                    if (server != null)
                    {
                        MonoServerInformation exists = Servers.FirstOrDefault(x => Equals(x.IpAddress, server.IpAddress));
                        if (exists == null)
                        {
                            Servers.Add(server);
                            server.LastMessage = DateTime.Now;
                        }
                        else
                        {
                            exists.LastMessage = DateTime.Now;
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }

                    foreach (
                        MonoServerInformation deadServer in
                            Servers.Where(x => ((DateTime.Now - x.LastMessage).TotalSeconds > 5)).ToList())
                        Servers.Remove(deadServer);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    MessageBox.Show("Port 15000 is in use.");
                else
                    MessageBox.Show(ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void StopLooking()
        {
            UserSettings settings = UserSettingsManager.Instance.Load();
            settings.LastIp = ManualIp;
            UserSettingsManager.Instance.Save(settings);

            cts.Cancel();
        }

        #region IDisposable Members
        protected bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                //Dispose managed resources
                cts.Dispose();
            }

            //Dispose unmanaged resources here.

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ServersFoundViewModel()
        {
            Dispose(false);
        }
        #endregion

    }
}