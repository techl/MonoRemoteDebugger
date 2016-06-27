﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using MonoTools.Debugger.Library;
using MonoTools.Debugger;
using MonoTools.VSExtension.Settings;
using NLog;
using Process = System.Diagnostics.Process;
using Microsoft.MIDebugEngine;

namespace MonoTools.VSExtension {

	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string)]
	[ProvideOptionPage(typeof(MonoToolsOptionsDialogPage), "MonoTools", "General", 0, 0, true)]
	[Guid(Guids.MonoToolsPkgString)]
	public sealed class VSPackage : Package, IDisposable {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private MonoDebugServer server = new MonoDebugServer();
		private Services services;

		protected override void Initialize() {
			var settingsManager = new ShellSettingsManager(this);
			var configurationSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
			UserSettingsManager.Initialize(configurationSettingsStore);
			MonoLogger.Setup();
			base.Initialize();
			var dte = (DTE)GetService(typeof(DTE));
			services = new Services(dte);
			TryRegisterAssembly();

			ErrorsWindow.Initialize(this);

			/* Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
				Source = new Uri("/MonoTools;component/Resources/Resources.xaml", UriKind.Relative)
			}); */

			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			InstallMenu(mcs);
		}

		private void InstallMenu(OleMenuCommandService mcs) {
			if (mcs != null) {
				// Create the commands for the menu item.
				var xBuildMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XBuildSolution);
				var xBuildMenuItem = new OleMenuCommand(XBuildMenuItemClicked, xBuildMenuID);
				xBuildMenuItem.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(xBuildMenuItem);

				var xRebuildMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XRebuildSolution);
				var xRebuildMenuItem = new OleMenuCommand(XRebuildMenuItemClicked, xRebuildMenuID);
				xRebuildMenuItem.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(xRebuildMenuItem);

				var xBuildProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XBuildProject);
				var xBuildProjectMenuItem = new OleMenuCommand(XBuildProjectMenuItemClicked, xBuildProjectMenuID);
				xBuildProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(xBuildProjectMenuItem);

				var xRebuildProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.XRebuildProject);
				var xRebuildProjectMenuItem = new OleMenuCommand(XRebuildProjectMenuItemClicked, xRebuildProjectMenuID);
				xRebuildProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(xRebuildProjectMenuItem);

				var addPdb2MdbToProjectMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.AddPdb2MdbToProject);
				var addPdb2MdbToProjectMenuItem = new OleMenuCommand(AddPdb2MdbToProjectMenuItemClicked, addPdb2MdbToProjectMenuID);
				addPdb2MdbToProjectMenuItem.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(addPdb2MdbToProjectMenuItem);

				var startMonoMenuID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.StartMono);
				var startMonoMenuItem = new OleMenuCommand(StartMonoMenuItemClicked, startMonoMenuID);
				startMonoMenuItem.BeforeQueryStatus += HasStartupProject;
				mcs.AddCommand(startMonoMenuItem);

				var debugMonoLocallyID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.DebugMonoLocal);
				var debugMonoCmd = new OleMenuCommand(DebugMonoClicked, debugMonoLocallyID);
				debugMonoCmd.BeforeQueryStatus += HasStartupProject;
				mcs.AddCommand(debugMonoCmd);

				/* var debugMonoRemoteID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.DebugMonoRemote);
				var remoteCmd = new OleMenuCommand(DebugRemoteClicked, debugMonoRemoteID);
				remoteCmd.BeforeQueryStatus += HasStartupProject;
				mcs.AddCommand(remoteCmd); */

				var logFileID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.OpenLogFile);
				var logFileCmd = new OleMenuCommand(OpenLogFile, logFileID);
				logFileCmd.BeforeQueryStatus += (o, e) => logFileCmd.Enabled = File.Exists(MonoLogger.LoggerPath);
				mcs.AddCommand(logFileCmd);

				var MoMAID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.MoMASolution);
				var MoMACmd = new OleMenuCommand(MoMAClicked, MoMAID);
				MoMACmd.BeforeQueryStatus += HasSolution;
				mcs.AddCommand(MoMACmd);

				var MoMAProjectID = new CommandID(Guids.MonoToolsCmdSet, (int)PkgCmdID.MoMAProject);
				var MoMAProjectCmd = new OleMenuCommand(MoMAProjectClicked, MoMAProjectID);
				MoMAProjectCmd.BeforeQueryStatus += HasCurrentProject;
				mcs.AddCommand(MoMAProjectCmd);
			}
		}

		private void OpenLogFile(object sender, EventArgs e) {
			services.OpenLogFile();
		}

		private void TryRegisterAssembly() {
			try {
				RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\{8BF3AB9F-3864-449A-93AB-E7B0935FC8F5}");

				if (regKey != null)
					return;

				string location = typeof(DebuggedProcess).Assembly.Location;

				string regasm = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe";
				if (!Environment.Is64BitOperatingSystem)
					regasm = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe";

				var p = new ProcessStartInfo(regasm, location);
				p.Verb = "runas";
				p.RedirectStandardOutput = true;
				p.UseShellExecute = false;
				p.CreateNoWindow = true;

				Process proc = Process.Start(p);
				while (!proc.HasExited) {
					string txt = proc.StandardOutput.ReadToEnd();
				}

				using (RegistryKey config = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration)) {
					MonoToolsInstaller.RegisterDebugEngine(location, config);
				}
			} catch (UnauthorizedAccessException) {
				MessageBox.Show(
					 "Failed finish installation of MonoTools.Debugger - Please run Visual Studio once als Administrator...",
					 "MonoTools.Debugger", MessageBoxButton.OK, MessageBoxImage.Error);
			} catch (Exception ex) {
				logger.Error(ex);
			}
		}

		private void HasStartupProject(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				var dte = GetService(typeof(DTE)) as DTE;
				var sb = (SolutionBuild2)dte.Solution.SolutionBuild;
				menuCommand.Visible = true;
				if (menuCommand.Visible)
					menuCommand.Enabled = ((Array)sb.StartupProjects).Cast<string>().Count() == 1;
			}
		}

		private void HasSolution(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				menuCommand.Visible = true;
				var dte = GetService(typeof(DTE)) as DTE;
				menuCommand.Enabled = dte.Solution != null;
			}
		}

		private void HasCurrentProject(object sender, EventArgs e) {
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null) {
				menuCommand.Visible = true;
				var dte = GetService(typeof(DTE)) as DTE;
				Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
				var proj = activeSolutionProjects.OfType<Project>().FirstOrDefault();
				menuCommand.Enabled = activeSolutionProjects.Length > 0;
				string cmd;
				switch ((uint)menuCommand.CommandID.ID) {
				case PkgCmdID.AddPdb2MdbToProject: cmd = "Add pdb2mdb to"; break;
				case PkgCmdID.MoMAProject: cmd = "MoMA"; break;
				case PkgCmdID.XBuildProject: cmd = "XBuild"; break;
				case PkgCmdID.XRebuildProject: cmd = "XRebuild"; break;
				default: throw new NotSupportedException();
				}
				menuCommand.Text = cmd + " " + (proj != null ? proj.Name : "Project");
			}
		}


		private readonly CancellationTokenSource cts = new CancellationTokenSource();

		private DTE2 GetDTE() {
			return GetService(typeof(DTE)) as DTE2;
		}

		private void StartMonoMenuItemClicked(object sender, EventArgs e) {
			services.Start();
		}
		private async void DebugMonoClicked(object sender, EventArgs e) {
			services.StartDebug();
		}

		private void XBuildMenuItemClicked(object sender, EventArgs e) {
			services.XBuild();
		}

		private void XRebuildMenuItemClicked(object sender, EventArgs e) {
			services.XBuild(true);
		}

		private void XBuildProjectMenuItemClicked(object sender, EventArgs e) {
			services.XBuildProject();
		}

		private void XRebuildProjectMenuItemClicked(object sender, EventArgs e) {
			services.XBuildProject(true);
		}

		private void AddPdb2MdbToProjectMenuItemClicked(object sender, EventArgs e) {
			services.AddPdb2MdbToProject();
		}

		private void MoMAClicked(object sender, EventArgs e) {
			services.MoMASolution();
		}

		private void MoMAProjectClicked(object sender, EventArgs e) {
			services.MoMAProject();
		}
		
		private void DebugRemoteClicked(object sender, EventArgs e) {
			services.StartSearching();
		}
		
		#region IDisposable Members
		private bool disposed = false;
		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);

			if (this.disposed)
				return;

			if (disposing) {
				//Dispose managed resources
				this.server.Dispose();
			}

			//Dispose unmanaged resources here.

			disposed = true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~VSPackage() {
			Dispose(false);
		}
		#endregion

	}
}