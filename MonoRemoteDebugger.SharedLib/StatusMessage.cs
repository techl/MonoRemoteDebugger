using System.Runtime.Serialization;

namespace MonoRemoteDebugger.SharedLib
{
    [DataContract]
    public class StatusMessage
    {
        [DataMember]
        public bool Successful { get; set; }
    }
}