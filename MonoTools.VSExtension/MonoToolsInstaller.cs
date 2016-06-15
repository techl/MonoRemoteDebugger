using System.Reflection;
using Microsoft.Win32;
using MonoTools.Debugger.VisualStudio;

namespace MonoTools.VSExtension
{
    public static class MonoToolsInstaller
    {
        private const string ENGINE_PATH = @"AD7Metrics\Engine\";
        private const string CLSID_PATH = @"CLSID\";

        public static void RegisterDebugEngine(string dllPath, RegistryKey rootKey)
        {
            using (RegistryKey engine = rootKey.OpenSubKey(@"AD7Metrics\Engine\", true))
            {
                string engineGuid = AD7Guids.EngineGuid.ToString("B").ToUpper();
                using (RegistryKey engineKey = engine.CreateSubKey(engineGuid))
                {
                    engineKey.SetValue("CLSID", AD7Guids.EngineGuid.ToString("B").ToUpper());
                    engineKey.SetValue("ProgramProvider", AD7Guids.ProgramProviderGuid.ToString("B").ToUpper());
                    engineKey.SetValue("Attach", 1, RegistryValueKind.DWord);
                    engineKey.SetValue("AddressBP", 0, RegistryValueKind.DWord);
                    engineKey.SetValue("AutoSelectPriority", 4, RegistryValueKind.DWord);
                    engineKey.SetValue("CallstackBP", 1, RegistryValueKind.DWord);
                    engineKey.SetValue("Name", AD7Guids.EngineName);
                    engineKey.SetValue("PortSupplier", AD7Guids.ProgramProviderGuid.ToString("B").ToUpper());
                    engineKey.SetValue("AlwaysLoadLocal", 1, RegistryValueKind.DWord);
                }
            }

            using (RegistryKey clsid = rootKey.OpenSubKey(CLSID_PATH, true))
            {
                using (RegistryKey clsidKey = clsid.CreateSubKey(AD7Guids.EngineGuid.ToString("B").ToUpper()))
                {
                    clsidKey.SetValue("Assembly", Assembly.GetExecutingAssembly().GetName().Name);
                    clsidKey.SetValue("Class", typeof(AD7Engine).FullName);
                    clsidKey.SetValue("InprocServer32", @"c:\windows\system32\mscoree.dll");
                    clsidKey.SetValue("CodeBase", dllPath);
                }

                using (
                    RegistryKey programProviderKey =
                        clsid.CreateSubKey(AD7Guids.ProgramProviderGuid.ToString("B").ToUpper()))
                {
                    programProviderKey.SetValue("Assembly", Assembly.GetExecutingAssembly().GetName().Name);
                    programProviderKey.SetValue("Class", typeof(AD7ProgramProvider).FullName);
                    programProviderKey.SetValue("InprocServer32", @"c:\windows\system32\mscoree.dll");
                    programProviderKey.SetValue("CodeBase", dllPath);
                }
            }
        }
    }
}