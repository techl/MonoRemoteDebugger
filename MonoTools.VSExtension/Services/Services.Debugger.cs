using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MonoTools.Debugger.Library;
using MonoTools.Debugger;
using MonoTools.Debugger.VisualStudio;
using MonoTools.VSExtension.MonoClient;
using MonoTools.VSExtension.Views;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NLog;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;
using Microsoft.MIDebugEngine;
using System.Windows;
using Microsoft.VisualStudio.Web.Application;

namespace MonoTools.VSExtension {

	public partial class Services {

		[System.Diagnostics.Conditional("DEBUG")]
		void Dump(Properties props) {
			foreach (Property p in props) {
				try {
					System.Diagnostics.Debugger.Log(1, "", $"Property: {p.Name}={p.Value.ToString()}\r\n");
				} catch { }
			}
		}

		public async void Start() {
			try {
				BuildSolution();

				string target = GetStartupAssemblyPath();
				string exe = null;
				string outputDirectory = Path.GetDirectoryName(target);
				string url = null;
				string serverurl = null;
				string page = null;
				string workingDirectory = null;
				string arguments = null;
				Project startup = GetStartupProject();
				var props = startup.ConfigurationManager.ActiveConfiguration.Properties;

				Dump(props);

				bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;

				var isNet4 = true;
				var frameworkprop = props.Get("TargetFrameworkMoniker")
					?.Split(',')
					.Where(t => t.StartsWith("Version="))
					.Select(t => t.Substring("Version=".Length))
					.FirstOrDefault();
				isNet4 = (frameworkprop == null || string.Compare(frameworkprop, "v4.0") >= 0);

				var action = "0";
				action = props.Get("StartAction");
				exe = props.Get("StartProgram");
				arguments = props.Get("StartArguments");
				workingDirectory = props.Get("StartWorkingDirectory");
				url = props.Get("StartURL");
				page = props.Get("StartPage");

				if (isWeb) {
					var ext = (WAProjectExtender)startup.Extender["WebApplication"];
					action = ext.DebugStartAction.ToString();
					outputDirectory = new Uri(ext.OpenedURL).LocalPath;
					serverurl = ext.BrowseURL ?? ext.NonSecureUrl ?? ext.SecureUrl ?? ext.IISUrl ?? "http://127.0.0.1:9000";
					var uri = new Uri(serverurl);
					var port = uri.Port;
					var args = $"-v --port={port}";
					var ssl = uri.Scheme.StartsWith("https");
					if (ssl) args += MonoWebProcess.SSLXpsArguments();

					var monoBin = Path.Combine(DetermineMonoPath(), "bin\\xsp" + (isNet4 ? "4" : "") + ".bat");
					var xsp = new ProcessStartInfo(monoBin, args) {
						WorkingDirectory = outputDirectory,
						UseShellExecute = false,
						CreateNoWindow = false
					};
					System.Diagnostics.Process.Start(xsp);

					if (action == "2") {
						var run = new ProcessStartInfo(ext.StartExternalProgram, ext.StartCmdLineArguments) {
							WorkingDirectory = ext.StartWorkingDirectory,
							UseShellExecute = false,
							CreateNoWindow = false,
							WindowStyle = ProcessWindowStyle.Minimized
						};
						System.Diagnostics.Process.Start(run);
						return;
					} else if (action == "3") {
						System.Diagnostics.Process.Start(ext.StartExternalUrl);
						return;
					} else if (action == "0") {
						System.Diagnostics.Process.Start(ext.CurrentDebugUrl);
					} else if (action == "1") {
						System.Diagnostics.Process.Start(ext.StartPageUrl);
					}
				} else {
					if (action == "0" || action == "2") {
						exe = target;
					}

					var monoBin = Path.Combine(DetermineMonoPath(), "bin\\mono.exe");
					var info = new ProcessStartInfo(monoBin, $"\"{exe}\" {arguments}") {
						WorkingDirectory = workingDirectory,
						UseShellExecute = false,
						CreateNoWindow = false
					};
					System.Diagnostics.Process.Start(info);

					if (action == "2") System.Diagnostics.Process.Start(url);
				}
			} catch (Exception ex) {
				logger.Error(ex);
				if (server != null) server.Stop();
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public async void StartDebug() {
			try {
				var startup = GetStartupProject();
				bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;
				string host = null;
				if (isWeb) {
					var ext = (WAProjectExtender)startup.Extender["WebApplication"];
					var serverurl = ext.NonSecureUrl ?? ext.SecureUrl;
					if (serverurl.Contains("*")) host = "*";
					else {
						var uri = new Uri(serverurl);
						host = uri.Host;
						var local = Dns.GetHostAddresses(host).Any(a => a.IsIPv6LinkLocal || a == IPAddress.Parse("127.0.0.1"));
						if (local) host = null;
					}
				} else {
					var props = startup.ConfigurationManager.ActiveConfiguration.Properties;
					try {
						if (props.Get("RemoteDebugEnabled")?.ToLower()  == "true") {
							host = props.Get("RemoteDebugMachine");
						}
					} catch { }
				}
				StartDebug(host);
			} catch (Exception ex) {
				logger.Error(ex);
				if (server != null) server.Stop();
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		MonoDebugServer server = null;

		public async void StartDebug(string host) {
			try {
				if (server != null) {
					server.Stop();
					server = null;
				}

				BuildSolution();

				if (host == null) {
					using (server = new MonoDebugServer(true)) {
						server.Start();
						await AttachDebugger(Debugger.Library.MonoProcess.GetLocalIp().ToString(), true);
					}
				} else if (host == "*") StartSearching();
				else {
					await AttachDebugger(host, false);
				}
			} catch (Exception ex) {
				logger.Error(ex);
				if (server != null) server.Stop();
				MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public async void StartSearching() {
			var dlg = new ServersFound();

			if (dlg.ShowDialog().GetValueOrDefault()) {
				try {
					BuildSolution();
					if (dlg.ViewModel.SelectedServer != null)
						await AttachDebugger(dlg.ViewModel.SelectedServer.IpAddress.ToString());
					else if (!string.IsNullOrWhiteSpace(dlg.ViewModel.ManualIp))
						await AttachDebugger(dlg.ViewModel.ManualIp);
				} catch (Exception ex) {
					logger.Error(ex);
					MessageBox.Show(ex.Message, "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		Task consoleTask;

		public async Task AttachDebugger(string ipAddress, bool local = false) {
			string target = GetStartupAssemblyPath();
			string exe = null, debug = null;
			string outputDirectory = Path.GetDirectoryName(target);
			string url = null;
			string serverurl = null;
			string page = null;
			string workingDirectory = null;
			string arguments = null;
			Project startup = GetStartupProject();
			var props = startup.ConfigurationManager.ActiveConfiguration.Properties;
			//Dump(props);

			bool isWeb = ((object[])startup.ExtenderNames).Any(x => x.ToString() == "WebApplication") || startup.Object is VsWebSite.VSWebSite;

			var isNet4 = true;
			var frameworkprop = props.Get("TargetFrameworkMoniker")
				?.Split(',')
				.Where(t => t.StartsWith("Version="))
				.Select(t => t.Substring("Version=".Length))
				.FirstOrDefault();
			isNet4 = (frameworkprop == null || string.Compare(frameworkprop, "v4.0") >= 0);
			Frameworks framework = isNet4 ? Frameworks.Net4 : Frameworks.Net2;

			var action = "0";
			action = props.Get("StartAction");
			exe = props.Get("StartProgram");
			arguments = props.Get("StartArguments");
			workingDirectory = props.Get("StartWorkingDirectory");
			url = props.Get("StartURL");
			page = props.Get("StartPage");

			Properties monoHelperProperties = dte.Properties["MonoTools", "General"];
			string ports = (string)monoHelperProperties.Item("MonoDebuggerPorts").Value;

			ApplicationTypes appType = isWeb ? ApplicationTypes.WebApplication : ApplicationTypes.DesktopApplication;
			if (isWeb) {
				outputDirectory += @"\..\..\";
				var ext = (WAProjectExtender)startup.Extender["WebApplication"];
				action = ext.DebugStartAction.ToString();
				serverurl = ext.BrowseURL ?? ext.NonSecureUrl ?? ext.SecureUrl ?? ext.IISUrl;

				StartDebugger(ApplicationTypes.WebApplication, target, outputDirectory, local, framework, null, serverurl, null, ipAddress, ports);

				if (action == "2") {
					var run = new ProcessStartInfo(ext.StartExternalProgram, ext.StartCmdLineArguments) {
						WorkingDirectory = ext.StartWorkingDirectory,
						UseShellExecute = false,
						CreateNoWindow = false
					};
					System.Diagnostics.Process.Start(run);
					return;
				} else if (action == "3") {
					System.Diagnostics.Process.Start(ext.StartExternalUrl);
					return;
				} else if (action == "0") {
					System.Diagnostics.Process.Start(ext.CurrentDebugUrl);
				} else if (action == "1") {
					System.Diagnostics.Process.Start(ext.StartPageUrl);
				}
			} else {

				StartDebugger(ApplicationTypes.DesktopApplication, target, outputDirectory, local, framework, arguments, null, workingDirectory, ipAddress, ports);

				if (action == "2") System.Diagnostics.Process.Start(url);
				else if (action == "1") {
					var run = new ProcessStartInfo(exe, arguments) {
						WorkingDirectory = workingDirectory,
						UseShellExecute = false,
						CreateNoWindow = false
					};
					System.Diagnostics.Process.Start(run);
				}
			}
		}

		public async void StartDebugger(ApplicationTypes appType, string targetExe, string outputDir, bool local, Frameworks framework, string arguments, string url, string workingDir, string ip, string ports) {

			IPAddress ipadr;
			if (!IPAddress.TryParse(ip, out ipadr)) {
				IPAddress[] adresses = Dns.GetHostEntry(ip).AddressList;
				ip = adresses.FirstOrDefault(adr => adr.AddressFamily == AddressFamily.InterNetwork)?.ToString();
				if (ip == null) throw new ArgumentException("Remote server not found.");
			}
			var client = new DebugClient(appType, targetExe, Path.GetFullPath(outputDir), local, framework) {
				Arguments = arguments,
				Url = url,
				WorkingDirectory = workingDir
			};
			DebugSession session = await client.ConnectToServerAsync(ip, ports);
			await session.TransferFilesAsync();
			await session.WaitForAnswerAsync();

			consoleTask = Task.Run(async () => { // handle console output
				var msg = await session.WaitForAnswerAsync();
				while (msg is ConsoleOutputMessage) {
					System.Diagnostics.Debugger.Log(1, "", ((ConsoleOutputMessage)msg).Text);
					msg = await session.WaitForAnswerAsync();
				}
			});

			IntPtr pInfo = GetDebugInfo(ip, targetExe, outputDir);
			var sp = new ServiceProvider((IServiceProvider)dte);
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

				if (!string.IsNullOrWhiteSpace(msg)) logger.Error(msg);

				throw;
			} finally {
				if (pInfo != IntPtr.Zero) Marshal.FreeCoTaskMem(pInfo);
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

		public void OpenLogFile() {
			if (File.Exists(MonoLogger.LoggerPath)) {
				System.Diagnostics.Process.Start(MonoLogger.LoggerPath);
			}
		}

	}
}