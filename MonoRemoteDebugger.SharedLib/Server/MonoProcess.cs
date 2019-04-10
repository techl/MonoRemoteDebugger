using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Techl;

namespace MonoRemoteDebugger.SharedLib.Server
{
    public abstract class MonoProcess
    {
        public string Arguments { get; set; }
        protected Process _proc;
        public event EventHandler ProcessStarted;
        internal abstract Process Start(string workingDirectory);

        protected void RaiseProcessStarted()
        {
            EventHandler handler = ProcessStarted;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected string GetProcessArgs()
        {
            IPAddress ip = IPAddress.Any;
            string args =
                string.Format(
                    @"--debugger-agent=address={0}:{1},transport=dt_socket,server=y --debug=mdb-optimizations", ip, GlobalConfig.Current.DebuggerAgentPort);
            return args;
        }

        protected ProcessStartInfo GetProcessStartInfo(string workingDirectory, string monoBin)
        {
            var dirInfo = new DirectoryInfo(workingDirectory);
            var procInfo = new ProcessStartInfo(monoBin);
            procInfo.WorkingDirectory = dirInfo.FullName;
            return procInfo;
        }

        public static IPAddress GetLocalIp()
        {
            IPAddress[] adresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            IPAddress adr = adresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return adr;
        }

        internal static MonoProcess Start(ApplicationType type, string _targetExe)
        {
            if (type == ApplicationType.Desktopapplication)
                return new MonoDesktopProcess(_targetExe);
            if (type == ApplicationType.Webapplication)
                return new MonoWebProcess();

            throw new Exception("Unknown ApplicationType");
        }
    }
}