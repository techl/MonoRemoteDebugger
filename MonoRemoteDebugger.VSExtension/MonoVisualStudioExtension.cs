using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.Debugger;
using MonoRemoteDebugger.Debugger.VisualStudio;
using MonoRemoteDebugger.VSExtension.MonoClient;
using NLog;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace MonoRemoteDebugger.VSExtension
{
    internal class MonoVisualStudioExtension
    {
        private readonly DTE _dte;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public MonoVisualStudioExtension(DTE dTE)
        {
            _dte = dTE;
        }

        internal void BuildSolution()
        {
            var sb = (SolutionBuild2) _dte.Solution.SolutionBuild;
            sb.Build(true);
        }

        internal string GetStartupAssemblyPath()
        {
            Project startupProject = GetStartupProject();
            return GetAssemblyPath(startupProject);
        }

        private Project GetStartupProject()
        {
            var sb = (SolutionBuild2) _dte.Solution.SolutionBuild;
            string project = ((Array) sb.StartupProjects).Cast<string>().First();
            Project startupProject = _dte.Solution.Item(project);
            return startupProject;
        }

        internal string GetAssemblyPath(Project vsProject)
        {
            string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            string outputPath =
                vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);
            return assemblyPath;
        }


        internal async Task AttachDebugger(string ipAddress)
        {
            string path = GetStartupAssemblyPath();
            string targetExe = Path.GetFileName(path);
            string outputDirectory = Path.GetDirectoryName(path);

            Project startup = GetStartupProject();

            bool isWeb = ((object[]) startup.ExtenderNames).Any(x => x.ToString() == "WebApplication");
            ApplicationType appType = isWeb ? ApplicationType.Webapplication : ApplicationType.Desktopapplication;
            if (appType == ApplicationType.Webapplication)
                outputDirectory += @"\..\..\";

            var client = new DebugClient(appType, targetExe, outputDirectory);
            DebugSession session = await client.ConnectToServerAsync(ipAddress);
            await session.TransferFilesAsync();
            await session.WaitForAnswerAsync();

            IntPtr pInfo = GetDebugInfo(ipAddress, targetExe, outputDirectory);
            var sp = new ServiceProvider((IServiceProvider) _dte);
            try
            {
                var dbg = (IVsDebugger) sp.GetService(typeof (SVsShellDebugger));
                int hr = dbg.LaunchDebugTargets(1, pInfo);
                Marshal.ThrowExceptionForHR(hr);

                DebuggedMonoProcess.Instance.AssociateDebugSession(session);
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                string msg;
                var sh = (IVsUIShell) sp.GetService(typeof (SVsUIShell));
                sh.GetErrorInfo(out msg);

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    logger.Error(msg);
                }
                throw;
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pInfo);
            }
        }

        private IntPtr GetDebugInfo(string args, string targetExe, string outputDirectory)
        {
            var info = new VsDebugTargetInfo();
            info.cbSize = (uint) Marshal.SizeOf(info);
            info.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

            info.bstrExe = Path.Combine(outputDirectory, targetExe);
            info.bstrCurDir = outputDirectory;
            info.bstrArg = args; // no command line parameters
            info.bstrRemoteMachine = null; // debug locally
            info.grfLaunch = (uint) __VSDBGLAUNCHFLAGS.DBGLAUNCH_StopDebuggingOnEnd;
            info.fSendStdoutToOutputWindow = 0;
            info.clsidCustom = MonoGuids.EngineGuid;
            info.grfLaunch = 0;

            IntPtr pInfo = Marshal.AllocCoTaskMem((int) info.cbSize);
            Marshal.StructureToPtr(info, pInfo, false);
            return pInfo;
        }
    }
}