using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;

namespace MonoTools.Debugger.Library {

	internal class Pdb2MdbGenerator {
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		internal void GeneratePdb2Mdb(string directoryName) {
			logger.Trace(directoryName);
			IEnumerable<string> files =
				 Directory.GetFiles(directoryName, "*.dll")
					  .Concat(Directory.GetFiles(directoryName, "*.exe"))
					  .Where(x => !x.Contains("vshost"));
			logger.Trace(files.Count());

			var dirInfo = new DirectoryInfo(directoryName);

			Parallel.ForEach(files, file => {
				try {
					string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
					string pdbFile = Path.Combine(Path.GetDirectoryName(file), fileNameWithoutExt + ".pdb");
					if (File.Exists(pdbFile)) {
						logger.Trace("Generate mdb for: " + file);
						Pdb2Mdb.Converter.Convert(file);
					}
				} catch (Exception ex) {
					logger.Trace(ex);
				}
			});

			logger.Trace("Transformed Debuginformation pdb2mdb");
		}
	}
}