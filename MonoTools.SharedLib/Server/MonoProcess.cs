using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace MonoTools.Debugger.Library
{
    public abstract class MonoProcess
    {
        private int monoDebugPort = 11000;
        protected Process process;
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
            //IPAddress ip = GetLocalIp();
            IPAddress ip = IPAddress.Any;
            string args =
                string.Format(
                    @"--debugger-agent=address={0}:{1},transport=dt_socket,server=y --debug=mdb-optimizations", ip, monoDebugPort);
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

        internal static MonoProcess Start(ApplicationTypes type, string targetExe, Frameworks framework)
        {
            if (type == ApplicationTypes.DesktopApplication)
                return new MonoDesktopProcess(targetExe);
            if (type == ApplicationTypes.WebApplication)
                return new MonoWebProcess(framework);

            throw new Exception("Unknown ApplicationType");
        }
    }
}