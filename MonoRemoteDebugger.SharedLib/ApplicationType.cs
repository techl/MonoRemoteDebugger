using System.Runtime.Serialization;

namespace MonoRemoteDebugger.SharedLib
{
    [DataContract]
    public enum ApplicationType
    {
        [EnumMember] Desktopapplication,
        [EnumMember] Webapplication
    }
}