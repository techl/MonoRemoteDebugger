using System.Diagnostics;
using System.IO;

namespace MonoRemoteDebugger.SharedLib.Server
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
            procInfo.Arguments = args + string.Format(" --config \"{0}.config\" \"{0}\" {1}", _targetExe, Arguments);

            _proc = Process.Start(procInfo);
            RaiseProcessStarted();
            return _proc;
        }
    }
}
