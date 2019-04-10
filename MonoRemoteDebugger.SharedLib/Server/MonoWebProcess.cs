using System.Diagnostics;
using System.Threading.Tasks;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    internal class MonoWebProcess : MonoProcess
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Url { get; private set; }

        internal override Process Start(string workingDirectory)
        {
            string monoBin = MonoUtils.GetMonoXsp4();
            string args = GetProcessArgs();
            ProcessStartInfo procInfo = GetProcessStartInfo(workingDirectory, monoBin);

            procInfo.Arguments = Arguments;
            procInfo.CreateNoWindow = true;
            procInfo.UseShellExecute = false;
            procInfo.EnvironmentVariables["MONO_OPTIONS"] = args;
            procInfo.RedirectStandardOutput = true;

            _proc = Process.Start(procInfo);
            Task.Run(() =>
            {
                while (!_proc.StandardOutput.EndOfStream)
                {
                    string line = _proc.StandardOutput.ReadLine();

                    if (line.StartsWith("Listening on address"))
                    {
                        string url = line.Substring(line.IndexOf(":") + 2).Trim();
                        if (url == "0.0.0.0")
                            Url = "localhost";
                        else
                            Url = url;
                    }
                    else if (line.StartsWith("Listening on port"))
                    {
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

            return _proc;
        }
    }
}