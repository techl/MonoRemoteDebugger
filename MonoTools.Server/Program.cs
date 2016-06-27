using System;
using System.Linq;
using MonoTools.Debugger.Library;

namespace MonoTools.Debugger.Server {

	internal class Program {

		private static void Main(string[] args) {

			Console.WriteLine("MonoDebugger v2.0, © johnshope.com. Pass ? for help.");

			if (args.Any(a => a.Contains("help") || a.Contains("?"))) {
				Console.WriteLine(@"usage: mono MonoDebugger.exe [-ports=message-port;debugger-port]

The ports must be set to free ports, and to the same values
that have been set in the VisualStudio MonoTools options.");
			}

			var ports = args.FirstOrDefault(a => a.StartsWith("-ports="))?.Substring("-ports=".Length);

			MonoLogger.Setup();

			using (var server = new MonoDebugServer(false, ports)) {
				//server.StartAnnouncing();
				server.Start();

				server.WaitForExit();
			}
		}
	}
}