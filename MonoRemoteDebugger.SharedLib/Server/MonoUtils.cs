using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    public static class MonoUtils
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        const PlatformID PlatFormIDUnixUnderNET1 = (PlatformID)128;

        public static void EnsurePdb2MdbCallWorks()
        {
            var platform = Environment.OSVersion.Platform;
            logger.Trace($"Platform={platform}, GetMonoPath={GetMonoPath()}, GetMonoXsp4={GetMonoXsp4()}, GetPdb2MdbPath={GetPdb2MdbPath()}");

            var fileName = MonoUtils.GetPdb2MdbPath();

            try
            {                
                StartProcess(fileName, string.Empty);
            }
            catch (Exception ex)
            {
                logger.Error($"Can not find and run {fileName}!", ex);
                AddShellScriptToMonoApp();
            }
        }

        private static void AddShellScriptToMonoApp()
        {
            try
            {
                var libMonoApplicationPath = GlobalConfig.Current.LibMonoApplicationPath;
                if (string.IsNullOrWhiteSpace(libMonoApplicationPath) || !Directory.Exists(libMonoApplicationPath))
                {
                    logger.Error($"{nameof(AddShellScriptToMonoApp)}: Path {libMonoApplicationPath} from 'App.config/configuration/appSettings/{nameof(GlobalConfig.Current.LibMonoApplicationPath)}' not found!");
                    return;
                }

                var shellScriptInstallPath = GlobalConfig.Current.ShellScriptInstallPath;
                if (string.IsNullOrWhiteSpace(shellScriptInstallPath) || !Directory.Exists(shellScriptInstallPath))
                {
                    logger.Error($"{nameof(AddShellScriptToMonoApp)}: Path {shellScriptInstallPath} from 'App.config/configuration/appSettings/{nameof(GlobalConfig.Current.ShellScriptInstallPath)}' not found!");
                    return;
                }
            
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    AddShellScriptToMonoApp(libMonoApplicationPath, shellScriptInstallPath, MonoUtils.GetPdb2MdbPath());
                    AddShellScriptToMonoApp(libMonoApplicationPath, shellScriptInstallPath, MonoUtils.GetMonoXsp4());
                }
                else
                {
                    throw new NotImplementedException($"Workaround for missing {MonoUtils.GetPdb2MdbPath()} is implemented only for unix (support for embedded linux)!");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{nameof(AddShellScriptToMonoApp)} failed!");
            }
        }

        private static void AddShellScriptToMonoApp(string libMonoApplicationPath, string shellScriptInstallPath, string programExeName)
        {
            var programExePath = Path.Combine(libMonoApplicationPath, $"{programExeName}.exe");
            logger.Trace($"Add shell script {shellScriptInstallPath}{programExeName} for {programExePath} ...");
            File.WriteAllText($"{shellScriptInstallPath}{programExeName}", $@"#!/bin/sh{Environment.NewLine}{shellScriptInstallPath}mono {programExePath} ""$@""");
            StartProcess("chmod", $"+x {shellScriptInstallPath}{programExeName}");
        }

        private static void StartProcess(string fileName, string args)
        {
            var procInfo = new ProcessStartInfo(fileName, args);
            procInfo.UseShellExecute = false;
            procInfo.CreateNoWindow = true;
            Process proc = Process.Start(procInfo);
            proc.WaitForExit();
        }

        public static string GetMonoPath()
        {
            var p = Environment.OSVersion.Platform;
            if (p == PlatformID.Unix || p == PlatformID.MacOSX || p == PlatFormIDUnixUnderNET1)
            {
                return "mono";
            }

            return Path.Combine(GetMonoRootPathWindows(), @"bin\mono.exe");
        }

        public static string GetMonoXsp4()
        {
            var p = Environment.OSVersion.Platform;
            if (p == PlatformID.Unix || p == PlatformID.MacOSX || p == PlatFormIDUnixUnderNET1)
            {
                return "xsp4";
            }

            return Path.Combine(GetMonoRootPathWindows(), @"bin\Xsp4.bat");
        }

        public static string GetPdb2MdbPath()
        {
            var p = Environment.OSVersion.Platform;
            if (p == PlatformID.Unix || p == PlatformID.MacOSX || p == PlatFormIDUnixUnderNET1)
            {
                return "pdb2mdb";
            }

            return Path.Combine(GetMonoRootPathWindows(), @"bin\pdb2mdb.bat");
        }

        private static string GetMonoRootPathWindows()
        {
            try
            {
                RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                RegistryKey monoKey = localMachine.OpenSubKey(@"Software\Novell\Mono\"); //legacy
                if (monoKey == null)
                {
                    monoKey = localMachine.OpenSubKey(@"Software\Mono\"); // on windows 10 x64 with Mono 64-Bit
                }
                if (monoKey == null)
                {
                    monoKey = localMachine.OpenSubKey(@"Software\WOW6432Node\Mono\"); // on windows 10 x64 with Mono 32Bit
                }
                if(monoKey == null)
                {
                    logger.Error("Cannot find monoKey in Windows registry. pdb2mdb not found. Please install Mono!");
                    return String.Empty;
                }

                var monoVersion = monoKey.GetValue("DefaultCLR") as string; //legacy
                if (string.IsNullOrEmpty(monoVersion)) // on windows 10
                {
                    monoVersion = monoKey.GetValue("Version") as string;

                    return (string)monoKey.GetValue("SdkInstallRoot");
                }
                else
                {
                    RegistryKey versionKey = localMachine.OpenSubKey(string.Format(@"Software\Novell\Mono\{0}", monoVersion));
                    var path = (string)versionKey.GetValue("SdkInstallRoot");
                    return path;
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex.ToString());
            }
            return string.Empty;
        }
    }
}