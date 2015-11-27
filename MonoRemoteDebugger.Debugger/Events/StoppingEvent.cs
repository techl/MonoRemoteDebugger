using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal class StoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint) enum_EVENTATTRIBUTES.EVENT_SYNC_STOP;

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }
    }
}