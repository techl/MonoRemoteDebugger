using System;
using System.Net;

namespace MonoTools.Debugger.VSExtension.MonoClient
{
    public class MonoServerInformation
    {
        public IPAddress IpAddress { get; set; }

        public string Message { get; set; }

        public DateTime LastMessage { get; set; }
    }
}