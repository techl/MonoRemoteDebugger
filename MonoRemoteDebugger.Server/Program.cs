using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;

namespace MonoRemoteDebugger.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MonoLogger.Setup();

            using (var server = new MonoDebugServer())
            {
                server.StartAnnouncing();
                server.Start();

                server.WaitForExit();
            }
        }
    }
}