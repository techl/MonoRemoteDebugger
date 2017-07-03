using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MonoRemoteDebugger.Contracts;
using MonoRemoteDebugger.SharedLib;

namespace MonoRemoteDebugger.VSExtension.MonoClient
{
    public class DebugSession : IDebugSession
    {
        private readonly TcpCommunication communication;
        private readonly ApplicationType type;

        public DebugSession(DebugClient debugClient, ApplicationType type, Socket socket)
        {
            Client = debugClient;
            this.type = type;
            communication = new TcpCommunication(socket);
        }

        public DebugClient Client { get; private set; }

        public void Disconnect()
        {
            communication.Disconnect();
        }

        public void TransferFiles()
        {
            var info = new DirectoryInfo(Client.OutputDirectory);
            if (!info.Exists)
                throw new DirectoryNotFoundException("Directory not found");

            string targetZip = Path.Combine(info.FullName, "DebugContent.zip");
            if (File.Exists(targetZip))
                File.Delete(targetZip);

            ZipFile.CreateFromDirectory(info.FullName, targetZip);

            communication.Send(Command.DebugContent, new StartDebuggingMessage
            {
                AppType = type,
                DebugContent = File.ReadAllBytes(targetZip),
                FileName = Client.TargetExe,
                AppHash = Client.AppHash
            });

            File.Delete(targetZip);
            Console.WriteLine("Finished transmitting");
        }

        public Task TransferFilesAsync()
        {
            return Task.Factory.StartNew(TransferFiles);
        }

        public void RestartDebugging()
        {
            communication.Send(Command.DebugLastContent, new StartDebuggingMessage
            {
                AppType = type,
                DebugContent = new byte[0],
                FileName = Client.TargetExe,
                AppHash = Client.AppHash                
            });

            Console.WriteLine("RestartDebugging transmitting");
        }

        public async Task<bool> RestartDebuggingAsync(int delay)
        {
            await Task.Factory.StartNew(RestartDebugging);

            try
            {
                var msg = await WaitForAnswerAsync(delay);
                return (msg.Command == Command.StartedMono && ((StatusMessage)msg.Payload).Successful);
            }
            catch (Exception ex)
            {
            }

            return false;
        }

        public async Task<MessageBase> WaitForAnswerAsync(int _delay=10000)
        {
            Task delay = Task.Delay(_delay);
            Task msg = await Task.WhenAny(communication.ReceiveAsync(), delay);

            if (msg is Task<MessageBase>)
                return (msg as Task<MessageBase>).Result;

            if (msg == delay)
                throw new Exception("Did not receive an answer in time...");
            throw new Exception("Cant start debugging");
        }
    }
}