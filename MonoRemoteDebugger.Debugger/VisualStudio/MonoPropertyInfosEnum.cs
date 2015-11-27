using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class MonoPropertyInfosEnum : MonoEnumerator<DEBUG_PROPERTY_INFO, IEnumDebugPropertyInfo2>,
        IEnumDebugPropertyInfo2
    {
        public MonoPropertyInfosEnum(IEnumerable<DEBUG_PROPERTY_INFO> enumerator)
            : base(enumerator)
        {
        }
    }
}