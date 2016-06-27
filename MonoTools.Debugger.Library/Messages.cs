using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.Serialization;
using System.Linq;


namespace MonoTools.Debugger.Library {

	public class StreamedFile {
		public string Name { get; set; }
		public virtual Stream Content { get; set; }
	}

	[Serializable]
	public class FilesCollection : IEnumerable<StreamedFile> {

		static readonly HashSet<string> Compressed = new HashSet<string>(new string[] { ".cs", ".vb", ".md", ".config", ".aspx", ".asmx", ".cshtml",
			".vbhtml", ".asax", ".sitemap", ".xml", ".ashx", ".txt", ".htm", ".html", ".ascx", ".dll", ".exe", ".bmp", ".svc", ".master", ".axd",".browser",
			".xmlc", ".asp+", ".armx", ".asbx", ".asp", ".vsdisco", ".pdf", ".xps", ".ps", ".wav", ".svc", ".pdb", ".mdb", ".doc", ".docx", ".dot",
			".docm", ".dotx", ".xls", ".xlt", ".xlsx", ".xlsm", ".xltx", ".ppt", ".pot", ".pps", ".pptx", ".pptm", ".potx", ".ppsx", ".pub", ".accdb",
			".accdt", ".fnt", ".fon", ".wof", ".ttf", ".otf", ".woff", ".css", ".js", ".ts", ".coffee", ".less", ".iso", ".bin", ".img", ".vhd", ".vhdx", ".vdi",
			".vmdk", ".hdd", ".qed", ".qcow", ".vbs", ".ps1" });

		List<string> Files { get; set; }
		List<string> Directories { get; set; }
		public string RootPath { get; set; }
		public bool HasMdbs = false;

		[NonSerialized]
		Stream stream;
		[NonSerialized]
		StreamModes mode;
		[NonSerialized]
		TcpCommunication con;
		[NonSerialized]
		string[] absoluteFiles;

		public FilesCollection() {
			Files = new List<string>(); Directories = new List<string>();
		}

		[OnSerializing]
		public void RelativePaths(StreamingContext context) {
			var root = RootPath;
			if (!root.EndsWith(Path.DirectorySeparatorChar.ToString())) root += Path.DirectorySeparatorChar; // append dir separator char
			for (int i = 0; i<Directories.Count; i++) { // make directories relative
				var name = Directories[i];
				if (name.StartsWith(root)) Directories[i] = name.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/');
				if (name == RootPath) Directories.RemoveAt(i--);
			}
			absoluteFiles = Files.ToArray(); // save absolute file paths in absoluteFiles
			for (int i = 0; i<Files.Count; i++) { // make files relative
				var name = Files[i];
				if (name.StartsWith(root)) Files[i] = name.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/'); ;
			}
		}

		[OnDeserialized]
		public void AbsolutePaths(StreamingContext context) {
			for (int i = 0; i<Directories.Count; i++) { // make directories absolute
				var name = Directories[i];
				if (!Path.IsPathRooted(name)) Directories[i] = Path.Combine(RootPath, name.Replace('/', Path.DirectorySeparatorChar));
			}
			for (int i = 0; i<Files.Count; i++) { // make files absolute
				var name = Files[i];
				if (!Path.IsPathRooted(name)) Files[i] = Path.Combine(RootPath, name.Replace('/', Path.DirectorySeparatorChar));
			}
		}

		bool NeedsCompression(TcpCommunication connection, string file) {
			return connection.Compressed && Compressed.Contains(Path.GetExtension(file));
		}

		public void Add(string file) {
			var ismdb = file.EndsWith(".dll.mdb") || file.EndsWith(".exe.mdb");
			if (ismdb) { // check if mdb file is up to date
				var pdb = file.Substring(0, file.Length-".dll.mdb".Length) + ".pdb";
				if (File.Exists(pdb) && File.GetLastWriteTimeUtc(pdb) > File.GetLastWriteTimeUtc(file)) return; // outdated mdb file, do not transfer.
			}
			if (!file.Contains(".vshost.exe")) Files.Add(file); // omit vshost.exe
			HasMdbs |= ismdb;
		}

		public void AddFolder(string path) {
			var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
				.Where(f => !f.Contains(".vshost.exe")); // omit vshost.exe
			foreach (var file in files) Add(file);
			Directories.AddRange(Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories));
		}

		public void Send(TcpCommunication connection) {
			con = connection;
			mode = StreamModes.Write;
			stream = connection.Stream;
			var w = connection.writer;
			w.Write(connection.Compressed);
			foreach (var file in EnumerateFiles()) {
				if (!string.IsNullOrEmpty(file.Name)) {
					w.Write(file.Name);
					var comp = NeedsCompression(connection, file.Name);
					w.Write(comp);
					Stream writer;
					if (comp) writer = new DeflateStream(stream, CompressionLevel.Fastest, true);
					else writer = stream;
					w.Write((Int64)file.Content.Length);
					file.Content.CopyTo(writer); System.Diagnostics.Debugger.Log(1, "", ".");
				}
			}
			w.Write("");
		}

		public void Receive(TcpCommunication connection) {
			con = connection;
			stream = connection.Stream;
			if (Directory.Exists(RootPath)) Directory.Delete(RootPath, true);
			foreach (var file in EnumerateFiles()) { // save files contents
				var path = Path.Combine(RootPath, file.Name);
				var dir = Path.GetDirectoryName(path);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				using (var w = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
					file.Content.CopyTo(w); Console.Write(".");
				}
			}
			Console.WriteLine();
		}

		IEnumerable<StreamedFile> EnumerateFiles() {
			if (mode == StreamModes.Write) { // write
				int i = 0;
				foreach (var file in Files) {
					var absFile = absoluteFiles[i++];
					yield return new StreamedFile { Name = file, Content = new FileStream(absFile, FileMode.Open, FileAccess.Read, FileShare.Read) };
				}
			} else { // read
				var r = con.reader;
				var name = r.ReadString();
				var comp = r.ReadBoolean();
				while (name != "") {
					HasMdbs |= name.EndsWith(".dll.mdb") || name.EndsWith(".exe.mdb");
					Stream reader;
					var len = r.ReadInt64();
					if (comp) {
						reader = new DeflateStream(stream, CompressionMode.Decompress, true);
					} else {
						reader = stream;
					}
					yield return new StreamedFile { Name = name, Content = new SubStream(reader, len) };
					name = r.ReadString();
				}
			}
		}

		public IEnumerator<StreamedFile> GetEnumerator() => EnumerateFiles().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)EnumerateFiles()).GetEnumerator();
	}

	public enum ApplicationTypes { DesktopApplication, WebApplication }
	public enum Commands : byte { DebugContent, StartedMono, Shutdown }
	public enum Frameworks { Net2, Net4 }

	[Serializable]
	public class Message { }

	public interface IMessageWithFiles {
		FilesCollection Files { get; }
	}

	public interface IExtendedMessage {
		void Send(TcpCommunication con);
		void Receive(TcpCommunication con);
	}


	[Serializable]
	public class CommandMessage : Message {
		public Commands Command { get; set; }
	}

	[Serializable]
	public class DebugMessage : CommandMessage, IMessageWithFiles, IExtendedMessage {
		public ApplicationTypes ApplicationType { get; set; }
		public Frameworks Framework { get; set; }
		public string Executable { get; set; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public string Url { get; set; }
		public bool IsLocal { get; set; }
		public string LocalPath { get; set; }
		public bool HasMdbs => Files.HasMdbs;
		public FilesCollection Files { get; protected set; } = new FilesCollection();
		public void Send(TcpCommunication con) { if (!IsLocal) Files.Send(con); }
		public void Receive(TcpCommunication con) {
			if (!IsLocal) {
				var oldroot = RootPath; 
				RootPath = con.RootPath;
				Files.Receive(con);
			} else {
				RootPath = LocalPath;
			}
			WorkingDirectory = WorkingDirectory ?? RootPath;
		}
		public string RootPath { get { return Files.RootPath; } set { Files.RootPath = value; } }
	}

	[Serializable]
	public class StatusMessage : CommandMessage { }

	[Serializable]
	public class ConsoleOutputMessage : Message {
		public string Text { get; set; }
	}

}