using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Mdb;
using System.Text.RegularExpressions;
using MonoTools.Debugger.Library;

namespace MonoTools.VSExtension {

	public partial class Services {

		public void MarkErrors(string text) {
			ErrorsWindow.Clear();
			var errors = new HashSet<string>();
			var regex = new Regex(@"(?<=Target Build.*Project ""(?<proj>[^""]*)"" \(default target\(s\)\):.*?)(?<doc>[^ \t\n\r]*)\((?<line>[0-9]+),(?<col>[0-9]+)\):\s*(?<type>error|warning)\s+(?<key>[A-Z0-9]+):(?<msg>[^\r\n]*)\r?\n", RegexOptions.Singleline);
			foreach (Match m in regex.Matches(text)) {
				if (errors.Contains(m.Value)) continue;
				errors.Add(m.Value);
				var proj = m.Groups["proj"].Value;
				var doc = m.Groups["doc"].Value;
				var type = m.Groups["type"].Value;
				var line = m.Groups["line"].Value;
				var col = m.Groups["col"].Value;
				var key = m.Groups["key"].Value;
				var msg = m.Groups["msg"].Value;
				//proj = Path.GetFileNameWithoutExtension(proj);
				if (type == "error") ErrorsWindow.AddError(type + " " + key + ": " + msg, type + " " + key, doc, int.Parse(line), int.Parse(col), proj);
				else ErrorsWindow.AddWarning(msg, key, doc, int.Parse(line), int.Parse(col), proj);
			}
		}

		public void XBuild(bool rebuild = false) {
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane();

			if (!rebuild) {
				outputWindowPane.OutputString("MonoTools: XBuild Solution Start\r\n\r\n");
			} else {
				outputWindowPane.OutputString("MonoTools: XRebuild Solution Start\r\n\r\n");
			}

			outputWindowPane.OutputString(string.Format("MonoTools: Saving Documents\r\n"));
			dte.ExecuteCommand("File.SaveAll");

			string monoPath = DetermineMonoPath();

			// Get current configuration
			string configurationName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
			string platformName = ((SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration).PlatformName;
			string fileName = string.Format(@"{0}\bin\xbuild.bat", monoPath);
			string arguments = string.Format(@"""{0}"" /p:Configuration=""{1}"" /p:Platform=""{2}"" /v:n {3}", dte.Solution.FileName,
				configurationName, platformName, rebuild ? " /t:Rebuild" : string.Empty);

			// Run XBuild and show in output
			System.Diagnostics.Process proc = new System.Diagnostics.Process {
				StartInfo =
					new ProcessStartInfo {
						FileName = fileName,
						Arguments = arguments,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true
					}
			};

			outputWindowPane.OutputString(string.Format("MonoTools: Running {0} {1}\r\n\r\n", fileName, arguments));

			proc.Start();

			var text = new StringWriter();

			while (!proc.StandardOutput.EndOfStream) {
				string line = proc.StandardOutput.ReadLine();
				text.WriteLine(line);
				outputWindowPane.OutputString(line);
				outputWindowPane.OutputString("\r\n");
			}

			// XBuild returned with error, stop processing XBuild Command
			if (proc.ExitCode != 0) {
				if (!rebuild) {
					outputWindowPane.OutputString("\r\n\r\nMonoTools: XBuild Solution End");
				} else {
					outputWindowPane.OutputString("\r\n\r\nMonoTools: XRebuild Solution End");
				}

				MarkErrors(text.ToString());

				return;
			}

			foreach (Project project in dte.Solution.Projects) {
				if (project.ConfigurationManager == null || project.ConfigurationManager.ActiveConfiguration == null) {
					continue;
				}

				Property debugSymbolsProperty = GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties,
					"DebugSymbols");

				// If DebugSymbols is true, generate pdb symbols for all assemblies in output folder
				if (debugSymbolsProperty != null && debugSymbolsProperty.Value is bool && (bool)debugSymbolsProperty.Value) {
					outputWindowPane.OutputString(
						string.Format("\r\nMonoTools: Generating DebugSymbols and injecting DebuggableAttributes for project {0}\r\n",
							project.Name));

					// Determine Outputpath, see http://www.mztools.com/articles/2009/MZ2009015.aspx
					string absoluteOutputPath = GetAbsoluteOutputPath(project);

					GeneratePdbs(absoluteOutputPath, outputWindowPane);
				}
			}

			if (!rebuild) {
				outputWindowPane.OutputString("\r\nMonoTools: XBuild Solution End");
			} else {
				outputWindowPane.OutputString("\r\nMonoTools: XRebuild Solution End");
			}
		}

		public void XBuildProject(bool rebuild = false) {
			System.Diagnostics.Process proc;
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane();
			if (!rebuild) {
				outputWindowPane.OutputString("MonoHelper: XBuild Solution Start\r\n\r\n");
			} else {
				outputWindowPane.OutputString("MonoHelper: XRebuild Solution Start\r\n\r\n");
			}
			outputWindowPane.OutputString(string.Format("MonoHelper: Saving Documents\r\n", new object[0]));
			dte.ExecuteCommand("File.SaveAll", "");
			Project project = null;
			Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
			if ((activeSolutionProjects != null) && (activeSolutionProjects.Length > 0)) {
				project = activeSolutionProjects.GetValue(0) as Project;
			}
			if (project == null) {
				outputWindowPane.OutputString("No project selected.");
			} else {
				string str = DetermineMonoPath();
				string name = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
				string platformName = ((SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration).PlatformName;
				string str4 = string.Format(@"{0}\bin\xbuild.bat", str);
				object[] args = new object[] { project.FileName, name, platformName, rebuild ? " /t:Rebuild" : string.Empty };
				string str5 = string.Format("\"{0}\" /p:Configuration=\"{1}\" /p:Platform=\"{2}\" /v:n {3}", args);
				System.Diagnostics.Process process1 = new System.Diagnostics.Process();
				ProcessStartInfo info1 = new ProcessStartInfo {
					FileName = str4,
					Arguments = str5,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				process1.StartInfo = info1;
				proc = process1;
				outputWindowPane.OutputString(string.Format("MonoHelper: Running {0} {1}\r\n\r\n", str4, str5));
				Task task = new TaskFactory().StartNew(delegate {
					proc.Start();
					var text = new StringWriter();
					while (!proc.StandardOutput.EndOfStream) {
						string line = proc.StandardOutput.ReadLine();

						text.WriteLine(line);
						outputWindowPane.OutputString(line);
						outputWindowPane.OutputString("\r\n");
					}
					if (proc.ExitCode > 0) {
						if (!rebuild) {
							outputWindowPane.OutputString("\r\n\r\nMonoHelper: XBuild Solution End");
						} else {
							outputWindowPane.OutputString("\r\n\r\nMonoHelper: XRebuild Solution End");
						}

						MarkErrors(text.ToString());
					} else {
						if ((project.ConfigurationManager != null) && (project.ConfigurationManager.ActiveConfiguration != null)) {
							Property property = GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "DebugSymbols");
							if (((property != null) && (property.Value is bool)) && ((bool)property.Value)) {
								outputWindowPane.OutputString(string.Format("\r\nMonoHelper: Generating DebugSymbols and injecting DebuggableAttributes for project {0}\r\n", project.Name));
								GeneratePdbs(GetAbsoluteOutputPath(project), outputWindowPane);
							}
						}
						if (!rebuild) {
							outputWindowPane.OutputString("\r\nMonoHelper: XBuild Solution End");
						} else {
							outputWindowPane.OutputString("\r\nMonoHelper: XRebuild Solution End");
						}
					}
				});
			}
		}

		const string targets = "Pdb2Mdb.targets";
		const string MSBuildExtensionsPath = @"MSBuild\johnshope.com\MonoTools";

		public void SetupMSBuildExtension() {
			var dll = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var src = Path.GetDirectoryName(dll);

			var msbuild = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), MSBuildExtensionsPath);
			Directory.CreateDirectory(msbuild);
			File.Copy(dll, Path.Combine(msbuild, Path.GetFileName(dll)), true);
			File.Copy(Path.Combine(src, targets), Path.Combine(msbuild, targets), true);
		}

		public void AddPdb2MdbToProject() {
			dte.ExecuteCommand("File.SaveAll", "");

			SetupMSBuildExtension();

			Project project = null;
			Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
			if ((activeSolutionProjects != null) && (activeSolutionProjects.Length > 0)) {
				project = activeSolutionProjects.GetValue(0) as Project;
			}
			if (project == null) return;

			var filename = project.FullName;
			var bproj = new Microsoft.Build.BuildEngine.Project(new Microsoft.Build.BuildEngine.Engine());
			bproj.Load(filename);

			var imppath = Path.Combine(MSBuildExtensionsPath, targets);

			Microsoft.Build.BuildEngine.Import sbimp = null;
			foreach (Microsoft.Build.BuildEngine.Import imp in bproj.Imports) {
				if (imp.ProjectPath.Contains(targets)) {
					sbimp = imp;
					if (sbimp.ProjectPath == imppath) return; // import is already there, so no need to modify project any further.
					break;
				}
			}

			dte.ExecuteCommand("Project.UnloadProject", string.Empty);

			if (sbimp != null) {
				if (sbimp.ProjectPath != imppath) {
					bproj.Imports.RemoveImport(sbimp);
					sbimp = null;
				}
			}
			if (sbimp == null) bproj.Imports.AddNewImport(imppath, null);
			bproj.Save(filename);

			dte.ExecuteCommand("Project.ReloadProject", string.Empty);

		}

		private void GeneratePdbs(string absoluteOutputPath, OutputWindowPane outputWindowPane) {

			var files = new HashSet<string>(
				new DirectoryInfo(absoluteOutputPath)
				.GetFiles()
				.Select(file => file.FullName));

			foreach (string file in files) {
				if ((file.EndsWith(".dll") || file.EndsWith(".exe")) && files.Contains(file + ".mdb")) {

					outputWindowPane.OutputString(string.Format("MonoTools: Assembly {0}\r\n", Path.GetFileName(file)));

					string assemblyPath = file;

					AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath,
						new ReaderParameters { SymbolReaderProvider = new MdbReaderProvider(), ReadSymbols = true });

					CustomAttribute debuggableAttribute =
						new CustomAttribute(
							assemblyDefinition.MainModule.Import(
								typeof(DebuggableAttribute).GetConstructor(new[] { typeof(DebuggableAttribute.DebuggingModes) })));

					debuggableAttribute.ConstructorArguments.Add(
						new CustomAttributeArgument(assemblyDefinition.MainModule.Import(typeof(DebuggableAttribute.DebuggingModes)),
							DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints |
								DebuggableAttribute.DebuggingModes.EnableEditAndContinue |
								DebuggableAttribute.DebuggingModes.DisableOptimizations));

					if (assemblyDefinition.CustomAttributes.Any(x => x.AttributeType.Name == typeof(DebuggableAttribute).Name)) {
						// Replace existing attribute
						int indexOf =
							assemblyDefinition.CustomAttributes.IndexOf(
								assemblyDefinition.CustomAttributes.Single(x => x.AttributeType.Name == typeof(DebuggableAttribute).Name));
						assemblyDefinition.CustomAttributes[indexOf] = debuggableAttribute;
					} else {
						assemblyDefinition.CustomAttributes.Add(debuggableAttribute);
					}

					assemblyDefinition.Write(assemblyPath,
						new WriterParameters { SymbolWriterProvider = new PdbWriterProvider(), WriteSymbols = true });
				}
			}
		}
	}
}