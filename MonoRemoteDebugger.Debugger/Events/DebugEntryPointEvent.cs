using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal class DebugEntryPointEvent : AsynchronousEvent, IDebugEntryPointEvent2
    {
        public const string IID = "E8414A3E-1642-48EC-829E-5F4040E16DA9";

        public DebugEntryPointEvent(IDebugEngine2 mEngine)
        {
            Engine = mEngine;
        }

        public object Engine { get; set; }
    }
}