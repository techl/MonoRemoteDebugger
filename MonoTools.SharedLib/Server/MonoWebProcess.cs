using System.Diagnostics;
using System.Threading.Tasks;
using NLog;

namespace MonoTools.Debugger.Library {
	internal class MonoWebProcess : MonoProcess {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public string Url { get; private set; }
		Frameworks Framework { get; set; } = Frameworks.Net4;

		public MonoWebProcess(Frameworks framework = Frameworks.Net4) { Framework = framework; }

		internal override Process Start(string workingDirectory) {
			string monoBin = MonoUtils.GetMonoXsp(Framework);
			string args = GetProcessArgs();
			ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);

			procInfo.CreateNoWindow = true;
			procInfo.UseShellExecute = false;
			procInfo.EnvironmentVariables["MONO_OPTIONS"] = args;
			procInfo.RedirectStandardOutput = true;

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