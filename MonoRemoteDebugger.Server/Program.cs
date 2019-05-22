using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;
using System;
using System.Reflection;

namespace MonoRemoteDebugger.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine($"MonoRemoteDebugger.Server v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

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