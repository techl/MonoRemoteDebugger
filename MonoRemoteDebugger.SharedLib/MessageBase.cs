using System.Runtime.Serialization;

namespace MonoRemoteDebugger.SharedLib
{
    [DataContract]
    public class MessageBase
    {
        [DataMember]
        public Command Command { get; set; }

        [DataMember]
        public object Payload { get; set; }
    }
}