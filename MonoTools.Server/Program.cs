using MonoTools.Debugger.Library;

namespace MonoTools.Debugger.Server
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