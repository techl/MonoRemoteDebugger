using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    internal class ClientSession
    {
        private readonly TcpCommunication communication;
        private readonly string directoryName;
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IPAddress remoteEndpoint;
        private readonly string root = Path.Combine(Path.GetTempPath(), "MonoRemoteDebugger");
        private Process proc;
        private string targetExe;

        public ClientSession(Socket socket)
        {
            directoryName = Path.Combine(root, Path.GetRandomFileName());
            remoteEndpoint = ((IPEndPoint)socket.RemoteEndPoint).Address;
            communication = new TcpCommunication(socket);
        }

        private string ZipFileName
        {
            get { return directoryName + ".zip"; }
        }

        public void HandleSession()
        {
            try
            {
                logger.Trace("New Session from {0}", remoteEndpoint);

                while (communication.IsConnected)
                {
                    if (proc != null && proc.HasExited)
                        return;

                    MessageBase msg = communication.Receive();

                    switch (msg.Command)
                    {
                        case Command.DebugContent:
                            StartDebugging((StartDebuggingMessage)msg.Payload);
                            communication.Send(Command.StartedMono, new StatusMessage());
                            break;
                        case Command.Shutdown:
                            logger.Info("Shutdown-Message received");
                            return;
                    }
                }
            }
            catch (XmlException xmlException)
            {
                logger.Info("CommunicationError : " + xmlException);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                if (proc != null && !proc.HasExited)
                    proc.Kill();
            }
        }

        private void StartDebugging(StartDebuggingMessage msg)
        {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            targetExe = msg.FileName;

            logger.Trace("Receiving content from {0}", remoteEndpoint);
            File.WriteAllBytes(ZipFileName, msg.DebugContent);
            ZipFile.ExtractToDirectory(ZipFileName, directoryName);

            foreach (string file in Directory.GetFiles(directoryName, "*vshost*"))
                File.Delete(file);

            File.Delete(ZipFileName);
            logger.Trace("Extracted content from {0} to {1}", remoteEndpoint, directoryName);

            var generator = new Pdb2MdbGenerator();
            string binaryDirectory = msg.AppType == ApplicationType.Desktopapplication
                ? directoryName
                : Path.Combine(directoryName, "bin");
            generator.GeneratePdb2Mdb(binaryDirectory);

            StartMono(msg.AppType);
        }

        private void StartMono(ApplicationType type)
        {
            MonoProcess proc = MonoProcess.Start(type, targetExe);
            proc.ProcessStarted += MonoProcessStarted;
            this.proc = proc.Start(directoryName);
            this.proc.EnableRaisingEvents = true;
            this.proc.Exited += _proc_Exited;
        }

        private void MonoProcessStarted(object sender, EventArgs e)
        {
            var web = sender as MonoWebProcess;
            if (web != null)
            {
                Process.Start(web.Url);
            }
        }

        private void _proc_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Program closed: " + proc.ExitCode);
            try
            {
                Directory.Delete(directoryName, true);
            }
            catch (Exception ex)
            {
                logger.Trace("Cant delete {0} - {1}", directoryName, ex.Message);
            }
        }
    }
}