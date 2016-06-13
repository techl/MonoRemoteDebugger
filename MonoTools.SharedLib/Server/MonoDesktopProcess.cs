using System.Diagnostics;
using System.IO;

namespace MonoTools.Debugger.Library
{
    internal class MonoDesktopProcess : MonoProcess
    {
        private readonly string _targetExe;

        public MonoDesktopProcess(string targetExe)
        {
            _targetExe = targetExe;
        }

        internal override Process Start(string workingDirectory)
        {
            string monoBin = MonoUtils.GetMonoPath();
            var dirInfo = new DirectoryInfo(workingDirectory);

            string args = GetProcessArgs();
            ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);
            procInfo.Arguments = args + " \"" + _targetExe + "\"";

            process = Process.Start(procInfo);
            RaiseProcessStarted();
            return process;
        }
    }
}