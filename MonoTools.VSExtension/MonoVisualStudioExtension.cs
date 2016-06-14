using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MonoTools.Debugger.Library;
using MonoTools.Debugger.Debugger;
using MonoTools.Debugger.Debugger.VisualStudio;
using MonoTools.Debugger.VSExtension.MonoClient;
using NLog;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;
using Microsoft.MIDebugEngine;

namespace MonoTools.VSExtension {

	internal class MonoVisualStudioExtension {
		private readonly DTE _dte;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public MonoVisualStudioExtension(DTE dTE) {
			_dte = dTE;
		}

		internal void BuildSolution() {
			var sb = (SolutionBuild2)_dte.Solution.SolutionBuild;
			sb.Build(true);
		}

		internal string GetStartupAssemblyPath() {
			Project startupProject = GetStartupProject();
			return GetAssemblyPath(startupProject);
		}

		public Project GetStartupProject() {
			var sb = (SolutionBuild2)_dte.Solution.SolutionBuild;
			string project = ((Array)sb.StartupProjects).Cast<string>().First();
			Project startupProject;
			try {
				startupProject = _dte.Solution.Item(project);
			} catch (ArgumentException aex) {
				throw new ArgumentException($"The parameter '{project}' is incorrect.", aex);
			}

			return startupProject;
		}

		internal string GetAssemblyPath(Project vsProject) {
			string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
			string outputPath =
				 vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
			string outputDir = Path.Combine(fullPath, outputPath);
			string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
			string assemblyPath = Path.Combine(outputDir, outputFileName);
			return assemblyPath;
		}

		Task consoleTask;

		internal async Task AttachDebugger(string ipAddress, bool local = false) {
			string path = GetStartupAssemblyPath();
			string targetExe = null;
			string outputDirectory = Path.GetDirectoryName(path);
			string url = null;
			string workingDirectory = null;
			string arguments = null;
			Project startup = GetStartupProject();
			Properties monoHelperProperties = _dte.Properties["MonoTools", "General"];
			string ports = (string)monoHelperProperties.Item("MonoDebuggerPorts").Value;

			bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication");

			var props = startup.ConfigurationManager.ActiveConfiguration.Properties;
			foreach (var p in props.OfType<Property>()) {
				try {
					System.Diagnostics.Debugger.Log(1, "", $"Property: {p.Name}={p.Value.ToString()}\r\n");
				} catch { }
			}

			ApplicationTypes appType = isWeb ? ApplicationTypes.WebApplication : ApplicationTypes.DesktopApplication;
			if (isWeb) {
				outputDirectory += @"\..\..\";
				var ext = startup.Extender["WebApplication"];
				//TODO read url from extender
			} else {
				string action = "0";
				try {
					action = props.Item("StartAction")?.Value?.ToString();
				} catch { }
				if (action == "1") {
					try {
						targetExe = props.Item("StartProgram")?.Value?.ToString();
					} catch { }
				} else if (action == "2") {
					try {
						url = props.Item("StartURL")?.Value?.ToString();
						targetExe = null;
					} catch { }
				} else {
					if (!local) targetExe = Path.GetFileName(path);
					else targetExe = path;
				}
				try {
					arguments = props.Item("StartArguments")?.Value?.ToString();
					if (arguments == "") arguments = null;
				} catch { }
				try {
					workingDirectory = props.Item("StartWorkingDirectory")?.Value?.ToString();
					if (workingDirectory == "") workingDirectory = null;
				} catch { }
			}
			Frameworks framework = Frameworks.Net4;
			try {
				var frameworkprop = startup.Properties.Item("TargetFrameworkMoniker")?.Value?.ToString().Split(',').Where(t => t.StartsWith("Version=")).Select(t => t.Substring("Version=".Length)).FirstOrDefault();
				if (frameworkprop != null && string.Compare(frameworkprop, "v4.0") < 0) framework = Frameworks.Net2;
			} catch { }
			var client = new DebugClient(appType, targetExe, Path.GetFullPath(outputDirectory), local, framework) {
				Arguments = arguments,
				Url = url,
				WorkingDirectory = workingDirectory
			};
			DebugSession session = await client.ConnectToServerAsync(ipAddress, ports);
			await session.TransferFilesAsync();
			await session.WaitForAnswerAsync();

			consoleTask = Task.Run(async () => { // handle console output
				var msg = await session.WaitForAnswerAsync();
				while (msg is ConsoleOutputMessage) {
					System.Diagnostics.Debugger.Log(1, "", ((ConsoleOutputMessage)msg).Text);
					msg = await session.WaitForAnswerAsync();
				}
			});

			IntPtr pInfo = GetDebugInfo(ipAddress, targetExe, outputDirectory);
			var sp = new ServiceProvider((IServiceProvider)_dte);
			try {
				var dbg = (IVsDebugger)sp.GetService(typeof(SVsShellDebugger));
				int hr = dbg.LaunchDebugTargets(1, pInfo);
				Marshal.ThrowExceptionForHR(hr);

				DebuggedProcess.Instance.AssociateDebugSession(session);
			} catch (Exception ex) {
				logger.Error(ex);
				string msg;
				var sh = (IVsUIShell)sp.GetService(typeof(SVsUIShell));
				sh.GetErrorInfo(out msg);

				if (!string.IsNullOrWhiteSpace(msg)) {
					logger.Error(msg);
				}
				throw;
			} finally {
				if (pInfo != IntPtr.Zero)
					Marshal.FreeCoTaskMem(pInfo);
			}
		}

		private IntPtr GetDebugInfo(string args, string targetExe, string outputDirectory) {
			var info = new VsDebugTargetInfo();
			info.cbSize = (uint)Marshal.SizeOf(info);
			info.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

			info.bstrExe = Path.Combine(outputDirectory, targetExe);
			info.bstrCurDir = outputDirectory;
			info.bstrArg = args; // no command line parameters
			info.bstrRemoteMachine = null; // debug locally
			info.grfLaunch = (uint)__VSDBGLAUNCHFLAGS.DBGLAUNCH_StopDebuggingOnEnd;
			info.fSendStdoutToOutputWindow = 0;
			info.clsidCustom = AD7Guids.EngineGuid;
			info.grfLaunch = 0;

			IntPtr pInfo = Marshal.AllocCoTaskMem((int)info.cbSize);
			Marshal.StructureToPtr(info, pInfo, false);
			return pInfo;
		}
	}
}