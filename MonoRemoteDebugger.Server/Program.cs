using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;

namespace MonoRemoteDebugger.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MonoLogger.Setup();

            MonoUtils.EnsurePdb2MdbCallWorks();

            using (var server = new MonoDebugServer())
            {
                server.StartAnnouncing();
                server.Start();

                server.WaitForExit();
            }
        }
    }
}