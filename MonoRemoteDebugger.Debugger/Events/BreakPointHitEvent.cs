using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.VisualStudio;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal class BreakPointHitEvent : StoppingEvent, IDebugBreakpointEvent2
    {
        public const string IID = "501C1E21-C557-48B8-BA30-A1EAB0BC4A74";

        private readonly MonoPendingBreakpoint _breakpoint;

        public BreakPointHitEvent(MonoPendingBreakpoint breakpoint)
        {
            _breakpoint = breakpoint;
        }

        public int EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            return _breakpoint.EnumBoundBreakpoints(out ppEnum);
        }
    }
}