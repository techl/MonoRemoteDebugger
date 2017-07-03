using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NLog;

namespace MonoRemoteDebugger.SharedLib
{
    public class TcpCommunication
    {
        private readonly DataContractSerializer _serializer;
        private readonly Socket _socket;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public TcpCommunication(Socket socket)
        {
            _socket = socket;
            List<Type> contracts =
                GetType()
                    .Assembly.GetTypes()
                    .Where(x => x.GetCustomAttributes(typeof (DataContractAttribute), true).Any())
                    .ToList();
            _serializer = new DataContractSerializer(typeof (MessageBase), contracts);
        }

        public bool IsConnected
        {
            get { return _socket.IsSocketConnected(); }
        }

        public void Send(Command cmd, object payload)
        {
            using (var ms = new MemoryStream())
            {
                _serializer.WriteObject(ms, new MessageBase {Command = cmd, Payload = payload});
                byte[] buffer = ms.ToArray();
                _socket.Send(BitConverter.GetBytes(buffer.Length));
                _socket.Send(buffer);
            }
        }

        public MessageBase Receive()
        {
            var buffer = new byte[sizeof (int)];
            int received = _socket.Receive(buffer);
            int size = BitConverter.ToInt32(buffer, 0);
            return ReceiveContent(size);
        }

        private MessageBase ReceiveContent(int size)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    int totalReceived = 0;
                    while (totalReceived != size)
                    {
                        var buffer = new byte[Math.Min(1024 * 10, size - totalReceived)];
                        int received = _socket.Receive(buffer);
                        totalReceived += received;
                        ms.Write(buffer, 0, received);
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    return _serializer.ReadObject(ms) as MessageBase;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ReceiveContent({size}) failed!");
                return null;
            }            
        }

        public Task<MessageBase> ReceiveAsync()
        {
            return Task.Factory.StartNew(() => Receive());
        }

        public void Disconnect()
        {
            if (_socket != null)
            {
                _socket.Close(1);
                _socket.Dispose();
            }
        }
    }
}