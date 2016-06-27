// Creates Mono mdb debug files from pdb files. 

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace MonoTools.Tasks {

	public class Pdb2Mdb: Task {

		const string ToolExe = "pdb2mdb.bat";
		const string DefaultToolPath = "Mono\\bin";

		public Pdb2Mdb() { }

		[Required]
		public ITaskItem[] Files { get; set; }

		[Output]
		public ITaskItem[] Output{ get; set; }

		public bool WarnOnError { get; set; }

		//public string ToolPath { get; set; }

		public bool Convert(string file) {
			try {
				global::Pdb2Mdb.Converter.Convert(file);
				lock (Log) Log.LogMessage(MessageImportance.Normal, "Generated \"{0}.mdb\".", file);
				return true;
			} catch (Exception ex) {
				lock (Log) {
					if (WarnOnError) Log.LogWarning("Error generating mdb for \"{0}\".", Path.GetFileNameWithoutExtension(file));
					else Log.LogError("Error generating mdb for \"{0}\".", Path.GetFileNameWithoutExtension(file));
					Log.LogErrorFromException(ex);
				}
				return false;
			}
		}

		public override bool Execute() {
			if (Type.GetType("Mono.Runtime") != null || Files == null) return true; // Don't execute under Mono.

			//if (!(ToolPath.EndsWith(".exe") || ToolPath.EndsWith(".bat"))) ToolPath = Path.Combine(ToolPath, ToolExe);
			var output = new List<TaskItem>();
			System.Threading.Tasks.Parallel.ForEach(Files, item => {
			//foreach (var item in Files) {
				var pdbfile = Path.ChangeExtension(item.ItemSpec, "pdb");
				if ((item.ItemSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || item.ItemSpec.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					&& File.Exists(item.ItemSpec) && File.Exists(pdbfile)) {
					var mdbfile = item.ItemSpec + ".mdb";
					if (!File.Exists(mdbfile) || (File.GetLastWriteTimeUtc(pdbfile) > File.GetLastWriteTimeUtc(mdbfile))) {
						if (Convert(item.ItemSpec)) {
							lock (output) output.Add(new TaskItem(mdbfile));
						}
					}
				}
			//}
			});

			Output = output.ToArray();
		
			return !Log.HasLoggedErrors;
		}
	}

}
