using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.IO;
using NLog;

namespace MonoTools.Debugger.Library {

	public class MonoWebProcess : MonoProcess {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public string Url { get; private set; }
		Frameworks Framework { get; set; } = Frameworks.Net4;

		public MonoWebProcess(Frameworks framework = Frameworks.Net4, string url = null) { Framework = framework; Url = url; }

		public static string SSLXpsArguments() {
			var a = Assembly.GetExecutingAssembly();
			var path = Path.GetTempPath();
			var cer = Path.Combine(path, "MonoTools.CARoot.cer");
			var pvk = Path.Combine(path, "MonoTools.CARoot.pvk");
			using (var r = a.GetManifestResourceStream("MonoTools.Debugger.Library.Server.CARoot.cer"))
			using (var f = new FileStream(cer, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			using (var r = a.GetManifestResourceStream("MonoTools.Debugger.Library.Server.CARoot.pvk"))
			using (var f = new FileStream(pvk, FileMode.Create, FileAccess.Write, FileShare.None)) {
				r.CopyTo(f);
			}
			return $" --https --cert=\"{cer}\" --pkfile=\"{pvk}\" --pkpwd=0192iw0192IW";
		}

		internal override Process Start(string workingDirectory) {
			string monoBin = MonoUtils.GetMonoXsp(Framework);
			string args = GetProcessArgs();
			ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);

			procInfo.CreateNoWindow = true;
			procInfo.UseShellExecute = false;
			procInfo.EnvironmentVariables["MONO_OPTIONS"] = args;
			procInfo.RedirectStandardOutput = true;
			if (Url != null) {
				var uri = new Uri(Url);
				var port = uri.Port;
				var ssl = uri.Scheme.StartsWith("https");
				procInfo.Arguments += $" --port={port}";
				if (ssl) {
					procInfo.Arguments += SSLXpsArguments();
				}
			}

			process = Process.Start(procInfo);
			Task.Run(() => {
				while (!process.StandardOutput.EndOfStream) {
					string line = process.StandardOutput.ReadLine();

					if (line.StartsWith("Listening on address")) {
						string url = line.Substring(line.IndexOf(":") + 2).Trim();
						if (url == "0.0.0.0")
							Url = "localhost";
						else
							Url = url;
					} else if (line.StartsWith("Listening on port")) {
						string port = line.Substring(line.IndexOf(":") + 2).Trim();
						port = port.Substring(0, port.IndexOf(" "));
						Url += ":" + port;

						if (line.Contains("non-secure"))
							Url = "http://" + Url;
						else
							Url = "https://" + Url;

						RaiseProcessStarted();
					}


					logger.Trace(line);
				}
			});

			return process;
		}
	}
}