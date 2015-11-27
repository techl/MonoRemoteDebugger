using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal sealed class LoadCompleteEvent : StoppingEvent, IDebugLoadCompleteEvent2
    {
        public const string IID = "B1844850-1349-45D4-9F12-495212F5EB0B";
    }
}