using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;
using System;
using Techl.Reflection;

namespace MonoRemoteDebugger.Server
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine($"MonoRemoteDebugger.Server v{VersionHelper.GetVersion()}");

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