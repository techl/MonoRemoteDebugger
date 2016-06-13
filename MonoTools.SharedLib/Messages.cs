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
	public class FilesCollection: IEnumerable<StreamedFile> {

		static readonly string[] Compressed = new string[] { ".cs", ".vb", ".md", ".config", ".aspx", ".asmx", ".cshtml", ".vbhtml", ".asax", ".sitemap", ".xml", ".ashx", ".txt", ".htm", ".html", ".ascx", ".dll", ".exe", ".bmp" }; 

		List<string> Files { get; set; }
		List<string> Directories { get; set; }
		public string RootPath { get; set; }
		[NonSerialized]
		Stream stream;
		[NonSerialized]
		StreamModes mode;
		[NonSerialized]
		TcpCommunication con;

		[OnSerializing]
		public void RelativePaths(StreamingContext context) {
			for (int i=0; i<Directories.Count;i++) {
				var name = Directories[i];
				if (name.StartsWith(RootPath)) Directories[i] = name.Substring(RootPath.Length).Replace(Path.DirectorySeparatorChar, '/');
			}
			for (int i = 0; i<Files.Count; i++) {
				var name = Files[i];
				if (name.StartsWith(RootPath)) Files[i] = name.Substring(RootPath.Length).Replace(Path.DirectorySeparatorChar, '/'); ;
			}
		}

		[OnDeserialized]
		public void AbsolutePaths(StreamingContext context) {
			for (int i = 0; i<Directories.Count; i++) {
				var name = Directories[i];
				if (!Path.IsPathRooted(name)) Directories[i] = Path.Combine(RootPath, name.Replace('/', Path.DirectorySeparatorChar));
			}
			for (int i = 0; i<Files.Count; i++) {
				var name = Files[i];
				if (!Path.IsPathRooted(name)) Files[i] = Path.Combine(RootPath, name.Replace('/', Path.DirectorySeparatorChar));
			}

		}

		bool NeedsCompression(TcpCommunication connection, string file) {
			return connection.Compressed && Compressed.Any(ext => file.EndsWith(ext));
		}

		public void Add(string file) {
			Files.Add(file);
		}
		public void AddFolder(string path) {
			Files.AddRange(Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories));
			Directories.AddRange(Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories));
		}

		public void Send(TcpCommunication connection) {
			con = connection;
			mode = StreamModes.Write;
			stream = connection.Stream;
			byte[] len;
			foreach (var file in EnumerateFiles()) {
				if (!string.IsNullOrEmpty(file.Name)) {
					len = BitConverter.GetBytes(file.Content.Length); // write file content
					stream.Write(len, 0, len.Length); // write length of file content
					Stream writer;
					if (NeedsCompression(connection, file.Name)) writer = new DeflateStream(stream, CompressionLevel.Fastest, true);
					else writer = stream; 
					file.Content.CopyTo(writer);
				}
			}
			len = BitConverter.GetBytes((int)0);
			stream.Write(len, 0, len.Length); // write 0 length
		}

		public void Receive(TcpCommunication connection) {
			con = connection;
			stream = connection.Stream;
			foreach (var file in EnumerateFiles()) { // save files contents
				using (var w = new FileStream(file.Name, FileMode.Create, FileAccess.Write, FileShare.None)) {
					file.Content.CopyTo(w);
				}
			}
		}

		IEnumerable<StreamedFile> EnumerateFiles() {
			if (mode == StreamModes.Write) { // write
				foreach (var file in Files) {
					yield return new StreamedFile { Name = file, Content = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read) };
				}
			} else { // read
				var lenbuf = BitConverter.GetBytes((int)0);
				var n = stream.Read(lenbuf, 0, lenbuf.Length);
				long len;
				byte[] str;
				while (n == lenbuf.Length && (len = BitConverter.ToInt32(lenbuf, 0)) > 0) {
					str = new byte[len];
					n = stream.Read(str, 0, (int)len);
					if (n != len) break;
					var name = Encoding.UTF8.GetString(str);
					lenbuf = BitConverter.GetBytes((long)0);
					n = stream.Read(lenbuf, 0, lenbuf.Length);
					stream.Flush();
					if (n != lenbuf.Length) break;
					Stream reader;
					long pos;
					if (NeedsCompression(con, name)) {
						reader = new DeflateStream(stream, CompressionMode.Decompress, true);
						pos = 0;
					} else {
						reader = stream;
						pos = stream.Position;
					}
					yield return new StreamedFile { Name = name, Content = new BufferedStreamWrapper(reader, StreamModes.Read, pos, BitConverter.ToInt64(lenbuf, 0)) };
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
	public class CommandMessage: Message {
		public Commands Command { get; set; }
	}

	[Serializable]
	public class DebugMessage : CommandMessage, IMessageWithFiles, IExtendedMessage {
		public ApplicationTypes ApplicationType { get; set; }
		public Frameworks Framework { get; set; }
		public string Executable { get; set; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public FilesCollection Files { get; protected set; } = new FilesCollection();
		public void Send(TcpCommunication con) { if (!con.IsLocal) Files.Send(con); }
		public void Receive(TcpCommunication con) { if (!con.IsLocal) Files.Receive(con); }
		public string RootPath { get { return Files.RootPath; } set { Files.RootPath = value; } }
	}

	[Serializable]
	public class StatusMessage: CommandMessage {	}

	[Serializable]
	public class ConsoleOutputMessage: Message {
		public string Text { get; set; }
	}

}