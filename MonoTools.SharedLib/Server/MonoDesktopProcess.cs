using System.Diagnostics;
using System.IO;

namespace MonoTools.Debugger.Library {

	internal class MonoDesktopProcess : MonoProcess {

		private readonly string _targetExe;
		string arguments;

		  public MonoDesktopProcess(string targetExe, string arguments) {
			_targetExe = targetExe;
			this.arguments = arguments;
		}

		internal override Process Start(string workingDirectory) {
			string monoBin = MonoUtils.GetMonoPath();
			var dirInfo = new DirectoryInfo(workingDirectory);

			string args = GetProcessArgs();
			ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);
			procInfo.Arguments = args + " \"" + _targetExe + "\"";
			if (!string.IsNullOrEmpty(arguments)) procInfo.Arguments += " " + arguments;

			process = Process.Start(procInfo);
			RaiseProcessStarted();
			return process;
		}
	}
}