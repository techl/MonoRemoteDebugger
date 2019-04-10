using System.Runtime.Serialization;

namespace MonoRemoteDebugger.SharedLib
{
    [DataContract]
    public class StartDebuggingMessage
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string Arguments { get; set; }

        [DataMember]
        public ApplicationType AppType { get; set; }

        [DataMember]
        public string AppHash { get; set; }

        [DataMember]
        public byte[] DebugContent { get; set; }
    }
}