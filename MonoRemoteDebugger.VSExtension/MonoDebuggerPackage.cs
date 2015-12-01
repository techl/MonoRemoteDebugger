using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using MonoRemoteDebugger.SharedLib;
using MonoRemoteDebugger.SharedLib.Server;
using MonoRemoteDebugger.Debugger;
using MonoRemoteDebugger.VSExtension.Settings;
using MonoRemoteDebugger.VSExtension.Views;
using NLog;
using Process = System.Diagnostics.Process;
using Microsoft.MIDebugEngine;

namespace MonoRemoteDebugger.VSExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(GuidList.guidMonoDebugger_VS2013PkgString)]
    public sealed class MonoDebuggerPackage : Package, IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private MonoVisualStudioExtension monoExtension;
        private MonoDebugServer server = new MonoDebugServer();

        protected override void Initialize()
        {
            var settingsManager = new ShellSettingsManager(this);
            var configurationSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            UserSettingsManager.Initialize(configurationSettingsStore);
            MonoLogger.Setup();
            base.Initialize();
            var dte = (DTE) GetService(typeof (DTE));
            monoExtension = new MonoVisualStudioExtension(dte);
            TryRegisterAssembly();


            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/MonoRemoteDebugger.VSExtension;component/Resources/Resources.xaml", UriKind.Relative)
            });

            var mcs = GetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            InstallMenu(mcs);
        }

        private void InstallMenu(OleMenuCommandService mcs)
        {
            if (mcs != null)
            {
                var debugLocally = new CommandID(GuidList.guidMonoDebugger_VS2013CmdSet,
                    (int) PkgCmdIDList.cmdLocalDebugCode);
                var localCmd = new OleMenuCommand(DebugLocalClicked, debugLocally);
                localCmd.BeforeQueryStatus += cmd_BeforeQueryStatus;
                mcs.AddCommand(localCmd);


                var menuCommandID = new CommandID(GuidList.guidMonoDebugger_VS2013CmdSet,
                    (int) PkgCmdIDList.cmdRemodeDebugCode);
                var cmd = new OleMenuCommand(DebugRemoteClicked, menuCommandID);
                cmd.BeforeQueryStatus += cmd_BeforeQueryStatus;
                mcs.AddCommand(cmd);

                var cmdOpenLogFileId = new CommandID(GuidList.guidMonoDebugger_VS2013CmdSet,
                    (int) PkgCmdIDList.cmdOpenLogFile);
                var openCmd = new OleMenuCommand(OpenLogFile, cmdOpenLogFileId);
                openCmd.BeforeQueryStatus += (o, e) => openCmd.Enabled = File.Exists(MonoLogger.LoggerPath);
                mcs.AddCommand(openCmd);
            }
        }

        private void OpenLogFile(object sender, EventArgs e)
        {
            if (File.Exists(MonoLogger.LoggerPath))
            {
                Process.Start(MonoLogger.LoggerPath);
            }
        }

        private void TryRegisterAssembly()
        {
            try
            {
                RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\{8BF3AB9F-3864-449A-93AB-E7B0935FC8F5}");

                if (regKey != null)
                    return;

                string location = typeof (DebuggedProcess).Assembly.Location;

                string regasm = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe";
                if (!Environment.Is64BitOperatingSystem)
                    regasm = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe";

                var p = new ProcessStartInfo(regasm, location);
                p.Verb = "runas";
                p.RedirectStandardOutput = true;
                p.UseShellExecute = false;
                p.CreateNoWindow = true;

                Process proc = Process.Start(p);
                while (!proc.HasExited)
                {
                    string txt = proc.StandardOutput.ReadToEnd();
                }

                using (RegistryKey config = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_Configuration))
                {
                    MonoDebuggerInstaller.RegisterDebugEngine(location, config);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Failed finish installation of MonoRemoteDebugger - Please run Visual Studio once als Administrator...",
                    "MonoRemoteDebugger", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void cmd_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                var dte = GetService(typeof (DTE)) as DTE;
                var sb = (SolutionBuild2) dte.Solution.SolutionBuild;
                menuCommand.Visible = sb.StartupProjects != null;
                if (menuCommand.Visible)
                    menuCommand.Enabled = ((Array) sb.StartupProjects).Cast<string>().Count() == 1;
            }
        }

        private void DebugLocalClicked(object sender, EventArgs e)
        {
            StartLocalServer();
        }

        private async void StartLocalServer()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }

                monoExtension.BuildSolution();

                using (server = new MonoDebugServer())
                {
                    server.Start();
                    await monoExtension.AttachDebugger(MonoProcess.GetLocalIp().ToString());
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                if (server != null)
                    server.Stop();
                MessageBox.Show(ex.Message, "MonoRemoteDebugger", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DebugRemoteClicked(object sender, EventArgs e)
        {
            StartSearching();
        }

        private async void StartSearching()
        {
            var dlg = new ServersFound();

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    monoExtension.BuildSolution();
                    if (dlg.ViewModel.SelectedServer != null)
                        await monoExtension.AttachDebugger(dlg.ViewModel.SelectedServer.IpAddress.ToString());
                    else if (!string.IsNullOrWhiteSpace(dlg.ViewModel.ManualIp))
                        await monoExtension.AttachDebugger(dlg.ViewModel.ManualIp);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    MessageBox.Show(ex.Message, "MonoRemoteDebugger", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region IDisposable Members
        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.disposed)
                return;

            if (disposing)
            {
                //Dispose managed resources
                this.server.Dispose();
            }

            //Dispose unmanaged resources here.

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MonoDebuggerPackage()
        {
            Dispose(false);
        }
        #endregion

    }
}