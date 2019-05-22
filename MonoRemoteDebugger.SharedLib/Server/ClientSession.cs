using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Linq;
using NLog;

namespace MonoRemoteDebugger.SharedLib.Server
{
    internal class ClientSession
    {
        private readonly TcpCommunication communication;        
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IPAddress remoteEndpoint;
        private readonly string tempContentDirectory;
        private readonly int skipLastUsedContentDirectories;
        private Process proc;

        private string directoryName;
        private string targetExe;
        private string Arguments;

        public ClientSession(Socket socket)
        {
            var basePath = new FileInfo(typeof(MonoLogger).Assembly.Location).Directory.FullName;
            tempContentDirectory = Path.Combine(basePath, "Temp");
            
            remoteEndpoint = ((IPEndPoint)socket.RemoteEndPoint).Address;            
            communication = new TcpCommunication(socket);

            skipLastUsedContentDirectories = GlobalConfig.Current.SkipLastUsedContentDirectories;
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

                    if (msg == null)
                    {
                        logger.Info("Null-Message received");
                        return;
                    }

                    switch (msg.Command)
                    {
                        case Command.DebugLastContent:
                        case Command.DebugContent:
                            logger.Info($"{msg.Command.ToString()}-Message received");
                            var successful = StartDebugging((StartDebuggingMessage)msg.Payload);
                            communication.Send(Command.StartedMono, new StatusMessage() { Successful = successful });
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
                try
                {
                    if (proc != null && !proc.HasExited)
                        proc.Kill();
                }
                catch { }
            }
        }
        
        private bool StartDebugging(StartDebuggingMessage msg)
        {
            if (!Directory.Exists(tempContentDirectory))
            {
                Directory.CreateDirectory(tempContentDirectory);
            }

            targetExe = msg.FileName;
            Arguments = msg.Arguments;
            
            directoryName = Path.Combine(tempContentDirectory, msg.AppHash);

            if (msg.DebugContent == null || msg.DebugContent.Length == 0)
            {
                logger.Trace($"Check if content is already available from {remoteEndpoint}: {directoryName}");

                if (!Directory.Exists(directoryName))
                {
                    logger.Trace("Content not found. Request new content from {0} ...", remoteEndpoint);
                    return false;
                }
            }
            else
            {
                logger.Trace("Receiving content from {0}", remoteEndpoint);

                var zipFileName = directoryName + ".zip";

                File.WriteAllBytes(zipFileName, msg.DebugContent);
                ZipFile.ExtractToDirectory(zipFileName, directoryName);

                foreach (string file in Directory.GetFiles(directoryName, "*vshost*"))
                {
                    File.Delete(file);
                }

                File.Delete(zipFileName);
                logger.Trace("Extracted content from {0} to {1}", remoteEndpoint, directoryName);

                var generator = new Pdb2MdbGenerator();

                string binaryDirectory = msg.AppType == ApplicationType.Desktopapplication
                    ? directoryName
                    : Path.Combine(directoryName, "bin");

                logger.Trace($"AppType: {msg.AppType} => choosing binaryDirectory={binaryDirectory}");

                generator.GeneratePdb2Mdb(binaryDirectory);
            }            

            StartMono(msg.AppType);

            return true;
        }

        private void StartMono(ApplicationType type)
        {
            MonoProcess proc = MonoProcess.Start(type, targetExe);
            proc.Arguments = Arguments;
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
                var oldTempDirectories = Directory.GetDirectories(tempContentDirectory)
                    .OrderByDescending(x => new DirectoryInfo(x).LastWriteTime)
                    .Skip(skipLastUsedContentDirectories); // keep last X directories

                foreach (string oldDirectory in oldTempDirectories)
                {
                    if (oldDirectory != directoryName)
                    {
                        Directory.Delete(oldDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Can't delete old temp directories from {tempContentDirectory}!");
            }
        }
    }
}