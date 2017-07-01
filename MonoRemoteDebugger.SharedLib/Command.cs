using System.Runtime.Serialization;

namespace MonoRemoteDebugger.SharedLib
{
    [DataContract]
    public enum Command : byte
    {
        [EnumMember]
        DebugContent,
        [EnumMember]
        StartedMono,
        [EnumMember] 
        Shutdown,
        [EnumMember]
        DebugLastContent
    }
}