using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.Events;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class ThreadCreateEvent : AsynchronousEvent, IDebugThreadCreateEvent2
    {
        public const string IID = "2090CCFC-70C5-491D-A5E8-BAD2DD9EE3EA";
    }
}