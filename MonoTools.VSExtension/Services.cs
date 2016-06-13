namespace MonoTools {
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
	using Microsoft.Win32;

	public static class Services {

		public static void MoMA(DTE2 dte, string path, IEnumerable<string> files, bool gui, OutputWindowPane output) {
			string str = DetermineMonoPath(dte);
			string fileName = string.Format(@"{0}\MoMA\MoMA.exe", str);
			string outpath = path + @"\MoMA Report.html";
			string[] textArray1 = new string[] { gui ? "" : "--nogui ", "--out \"", outpath, "\" ", string.Join(" ", files.Select(file => "\"" + file + "\"")) };
			string arguments = string.Concat(textArray1);
			output.OutputString("\r\n\r\nMonoTools: MoMA");
			Task task = new TaskFactory().StartNew(delegate {
				System.Diagnostics.Process process1 = new System.Diagnostics.Process();
				ProcessStartInfo info1 = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				process1.StartInfo = info1;
				System.Diagnostics.Process process = process1;
				process.Start();
				process.WaitForExit();
				System.Diagnostics.Process.Start(outpath);
			});
		}

		public static void MoMAProject(DTE2 dte) {
			Project project = null;
			OutputWindowPane output = PrepareOutputWindowPane(dte);
			output.OutputString("MoMA Project\r\n\r\n");
			Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
			if ((activeSolutionProjects != null) && (activeSolutionProjects.Length > 0)) {
				project = activeSolutionProjects.GetValue(0) as Project;
			}
			if (project == null) {
				output.OutputString("No project selected.\r\n\r\n");
			} else {
				string absoluteOutputPath = GetAbsoluteOutputPath(project);
				IEnumerable<string> files = Directory.EnumerateFiles(absoluteOutputPath, "*.exe").Concat<string>(Directory.EnumerateFiles(absoluteOutputPath, "*.dll"));
				MoMA(dte, Path.GetDirectoryName(project.FileName), files, false, output);
			}
		}

		public static void MoMASolution(DTE2 dte) {
			OutputWindowPane output = PrepareOutputWindowPane(dte);
			output.OutputString("MoMA Solution\r\n\r\n");
			if (string.IsNullOrEmpty((dte.Solution == null) ? null : dte.Solution.FileName)) {
				output.OutputString("No solution.\r\n\r\n");
			}


			IEnumerable<string> source = dte.Solution.Projects.OfType<Project>()
				.SelectMany(proj => {
					var absoluteOutputPath = GetAbsoluteOutputPath(proj);
					if (absoluteOutputPath == null) return new string[0];
					return Directory.EnumerateFiles(absoluteOutputPath, "*.exe").Concat(Directory.EnumerateFiles(absoluteOutputPath, "*.dll"));
				});
			if (source.Count() > 0) MoMA(dte, Path.GetDirectoryName(dte.Solution.FileName), source, false, output);
		}

		public static void XBuild(DTE2 dte, bool rebuild = false) {
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane(dte);

			if (!rebuild) {
				outputWindowPane.OutputString("MonoTools: XBuild Solution Start\r\n\r\n");
			} else {
				outputWindowPane.OutputString("MonoTools: XRebuild Solution Start\r\n\r\n");
			}

			outputWindowPane.OutputString(string.Format("MonoTools: Saving Documents\r\n"));
			dte.ExecuteCommand("File.SaveAll");

			string monoPath = DetermineMonoPath(dte);

			// Get current configuration
			string configurationName = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
			string platformName = ((SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration).PlatformName;
			string fileName = string.Format(@"{0}\bin\xbuild.bat", monoPath);
			string arguments = string.Format(@"""{0}"" /p:Configuration=""{1}"" /p:Platform=""{2}"" {3}", dte.Solution.FileName,
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

			while (!proc.StandardOutput.EndOfStream) {
				string line = proc.StandardOutput.ReadLine();

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

					GenerateDebugSymbols(absoluteOutputPath, outputWindowPane);
				}
			}

			if (!rebuild) {
				outputWindowPane.OutputString("\r\nMonoTools: XBuild Solution End");
			} else {
				outputWindowPane.OutputString("\r\nMonoTools: XRebuild Solution End");
			}
		}

		public static void XBuildProject(DTE2 dte, bool rebuild = false) {
			System.Diagnostics.Process proc;
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane(dte);
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
				string str = DetermineMonoPath(dte);
				string name = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
				string platformName = ((SolutionConfiguration2)dte.Solution.SolutionBuild.ActiveConfiguration).PlatformName;
				string str4 = string.Format(@"{0}\bin\xbuild.bat", str);
				object[] args = new object[] { project.FileName, name, platformName, rebuild ? " /t:Rebuild" : string.Empty };
				string str5 = string.Format("\"{0}\" /p:Configuration=\"{1}\" /p:Platform=\"{2}\" {3}", args);
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
					while (!proc.StandardOutput.EndOfStream) {
						string text = proc.StandardOutput.ReadLine();
						outputWindowPane.OutputString(text);
						outputWindowPane.OutputString("\r\n");
					}
					if (proc.ExitCode > 0) {
						if (!rebuild) {
							outputWindowPane.OutputString("\r\n\r\nMonoHelper: XBuild Solution End");
						} else {
							outputWindowPane.OutputString("\r\n\r\nMonoHelper: XRebuild Solution End");
						}
					} else {
						if ((project.ConfigurationManager != null) && (project.ConfigurationManager.ActiveConfiguration != null)) {
							Property property = GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "DebugSymbols");
							if (((property != null) && (property.Value is bool)) && ((bool)property.Value)) {
								outputWindowPane.OutputString(string.Format("\r\nMonoHelper: Generating DebugSymbols and injecting DebuggableAttributes for project {0}\r\n", project.Name));
								GenerateDebugSymbols(GetAbsoluteOutputPath(project), outputWindowPane);
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
		const string MSBuildExtensionsPath = @"$(MSBuildExtensionsPath)\johnshope.com\MonoTools";

		public static void SetupMSBuildExtension() {
			var dll = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var src = Path.GetDirectoryName(dll);

			var msbuild = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), MSBuildExtensionsPath);
			Directory.CreateDirectory(msbuild);
			File.Copy(dll, Path.Combine(msbuild, Path.GetFileName(dll)));
			File.Copy(Path.Combine(src, targets), Path.Combine(msbuild, targets));
		}

		public static void AddPdb2MdbToProject(DTE2 dte) {
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



		private static string DetermineMonoPath(DTE2 dte) {
			OutputWindowPane outputWindowPane = PrepareOutputWindowPane(dte);

			Properties monoHelperProperties = dte.Properties["MonoTools", "General"];
			string monoPath = (string)monoHelperProperties.Item("MonoInstallationPath").Value;

			if (!string.IsNullOrEmpty(monoPath)) {
				outputWindowPane.OutputString("MonoTools: Mono Installation Path is set.\r\n");
			} else {
				outputWindowPane.OutputString("MonoTools: Mono Installation Path is not set. Trying to get it from registry.\r\n");

				RegistryKey openSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Novell\\Mono");

				if (openSubKey == null) {
					openSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Novell\\Mono");
				}

				if (openSubKey == null) {
					monoPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Mono");
					if (!Directory.Exists(monoPath))
						throw new Exception(
							"Mono Runtime not found. Please install Mono and ensure that Mono Installation Path is set via Tools \\ Options \\ Mono Helper or that the necessary registry settings are existing.");
				} else {
					string value = openSubKey.GetSubKeyNames().OrderByDescending(x => x).First();
					monoPath = (string)openSubKey.OpenSubKey(value).GetValue("SdkInstallRoot");
				}
			}

			return monoPath;
		}

		private static void GenerateDebugSymbols(string absoluteOutputPath, OutputWindowPane outputWindowPane) {
			FileInfo[] files = (new DirectoryInfo(absoluteOutputPath)).GetFiles();

			foreach (FileInfo file in files) {
				if (file.Name.EndsWith(".dll") || file.Name.EndsWith(".exe")) {
					if (files.Any(x => x.Name.EndsWith(".mdb") && x.Name.Substring(0, x.Name.Length - 4) == file.Name)) {
						outputWindowPane.OutputString(string.Format("MonoTools: Assembly {0}\r\n", file.Name));

						string assemblyPath = file.FullName;

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

		private static string GetAbsoluteOutputPath(Project project) {
			Property property = null;
			try {
				property = GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "OutputPath");
			} catch {
				return null;
			}
			string str = ((string)property.Value as string) ?? "";
			if (str.StartsWith(Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString())) {
				return str;
			}
			if ((str.Length >= 2) && (str[1] == Path.VolumeSeparatorChar)) {
				return str;
			}
			if (str.IndexOf(@"..\") != -1) {
				string directoryName = Path.GetDirectoryName(project.FullName);
				while (str.StartsWith(@"..\")) {
					str = str.Substring(3);
					directoryName = Path.GetDirectoryName(directoryName);
				}
				return Path.Combine(directoryName, str);
			}
			return Path.Combine(Path.GetDirectoryName(project.FullName), str);
		}

		private static string GetProgramFileName(Project project) {
			switch (((int)GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "StartAction").Value)) {
			case 0: {
					Property property = GetProperty(project.Properties, "OutputFileName");
					return Path.Combine(GetAbsoluteOutputPath(project), (string)property.Value);
				}
			case 1:
				return (string)GetProperty(project.ConfigurationManager.ActiveConfiguration.Properties, "StartProgram").Value;

			case 2:
				return GetAbsoluteOutputPath(project);
			}
			throw new InvalidOperationException("Unknown StartAction");
		}

		private static Property GetProperty(Properties properties, string propertyName) {
			if (properties != null) {
				foreach (Property property in properties) {
					if ((property != null) && (property.Name == propertyName)) {
						return property;
					}
				}
			}
			return null;
		}

		private static OutputWindowPane PrepareOutputWindowPane(DTE2 dte) {
			dte.ExecuteCommand("View.Output");

			OutputWindow outputWindow = dte.ToolWindows.OutputWindow;

			OutputWindowPane outputWindowPane = null;

			foreach (OutputWindowPane pane in outputWindow.OutputWindowPanes) {
				if (pane.Name == "MonoTools") {
					outputWindowPane = pane;
					break;
				}
			}

			if (outputWindowPane == null) {
				outputWindowPane = outputWindow.OutputWindowPanes.Add("MonoTools");
			}

			outputWindowPane.Activate();

			outputWindowPane.Clear();
			outputWindowPane.OutputString("MonoTools, Version 1.0\r\n");
			outputWindowPane.OutputString("Copyright © Christopher Dresel,  Simon Egli, Giesswein-Apps 2016\r\n");
			outputWindowPane.OutputString("\r\n");

			return outputWindowPane;
		}

		public static System.Diagnostics.Process StartMono(DTE2 dte /* , OutputWindowPane outputWindowPane */) {
			Project startupProject = dte.Solution.Item(((object[])dte.Solution.SolutionBuild.StartupProjects)[0]);

			string fileName = GetProgramFileName(startupProject);
			string arguments = string.Empty;

			Property startArguments = GetProperty(startupProject.ConfigurationManager.ActiveConfiguration.Properties,
				"StartArguments");
			arguments = (string)startArguments.Value;

			string monoPath = DetermineMonoPath(dte);

			/* outputWindowPane.OutputString(string.Format("MonoTools: Running {0}\\bin\\mono.exe \"{1}\" {2}\r\n", monoPath,
				fileName, arguments)); */

			System.Diagnostics.Process process = new System.Diagnostics.Process {
				StartInfo =
					new ProcessStartInfo {
						FileName = string.Format(@"{0}\bin\mono.exe", monoPath),
						Arguments = string.Format(@"""{0}"" {1}", fileName, arguments),
						UseShellExecute = true,
						WorkingDirectory = Path.GetDirectoryName(fileName)
					}
			};

			process.Start();

			return process;
		}

		public static System.Diagnostics.Process StartNet(DTE2 dte, OutputWindowPane outputWindowPane) {
			Project startupProject = dte.Solution.Item(((object[])dte.Solution.SolutionBuild.StartupProjects)[0]);
			Property startArguments = GetProperty(startupProject.ConfigurationManager.ActiveConfiguration.Properties,
				"StartArguments");

			string fileName = GetProgramFileName(startupProject);
			string arguments = (string)startArguments.Value;

			outputWindowPane.OutputString(string.Format("MonoTools: Running {0} {1}\r\n", fileName, arguments));

			System.Diagnostics.Process process = new System.Diagnostics.Process {
				StartInfo =
					new ProcessStartInfo {
						FileName = string.Format(@"{0}", fileName),
						Arguments = string.Format(@"{0}", arguments),
						UseShellExecute = true,
						WorkingDirectory = Path.GetDirectoryName(fileName)
					}
			};

			process.Start();

			return process;
		}
	}
}