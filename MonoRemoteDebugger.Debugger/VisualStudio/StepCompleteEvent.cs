using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.Events;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal sealed class StepCompleteEvent : StoppingEvent, IDebugStepCompleteEvent2
    {
        public const string IID = "0F7F24C1-74D9-4EA6-A3EA-7EDB2D81441D";
    }
}