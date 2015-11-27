using System;
using System.IO;
using Microsoft.Win32;

namespace MonoRemoteDebugger.SharedLib.Server
{
    internal static class MonoUtils
    {
        public static string GetMonoPath()
        {
            var p = (int) Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
            {
                return "mono";
            }

            return Path.Combine(GetMonoRootPathWindows(), @"bin\mono.exe");
        }

        public static string GetMonoXsp4()
        {
            var p = (int) Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
            {
                return "xsp4";
            }

            return Path.Combine(GetMonoRootPathWindows(), @"bin\Xsp4.bat");
        }

        public static string GetPdb2MdbPath()
        {
            var p = (int) Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128))
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
                RegistryKey monoKey = localMachine.OpenSubKey(@"Software\Novell\Mono\");
                var monoVersion = monoKey.GetValue("DefaultCLR") as string;
                RegistryKey versionKey = localMachine.OpenSubKey(string.Format(@"Software\Novell\Mono\{0}", monoVersion));
                var path = (string) versionKey.GetValue("SdkInstallRoot");
                return path;
            }
            catch
            {
            }
            return string.Empty;
        }
    }
}