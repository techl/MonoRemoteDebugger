using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    internal class Pdb2MdbGenerator
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal void GeneratePdb2Mdb(string directoryName)
        {
            logger.Trace(directoryName);
            IEnumerable<string> files =
                Directory.GetFiles(directoryName, "*.dll")
                    .Concat(Directory.GetFiles(directoryName, "*.exe"))
                    .Where(x => !x.Contains("vshost"));
            logger.Trace(files.Count());

            var dirInfo = new DirectoryInfo(directoryName);

            Parallel.ForEach(files, file =>
            {
                try
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                    string pdbFile = Path.Combine(Path.GetDirectoryName(file), fileNameWithoutExt + ".pdb");
                    if (File.Exists(pdbFile))
                    {
                        logger.Trace("Generate mdp for: " + file);
                        var procInfo = new ProcessStartInfo(MonoUtils.GetPdb2MdbPath(), $"\"{Path.GetFileName(file)}\"");
                        procInfo.WorkingDirectory = dirInfo.FullName;
                        procInfo.UseShellExecute = false;
                        procInfo.CreateNoWindow = true;
                        Process proc = Process.Start(procInfo);
                        proc.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    logger.Trace(ex);
                }
            });

            logger.Trace("Transformed Debuginformation pdb2mdb");
        }
    }
}