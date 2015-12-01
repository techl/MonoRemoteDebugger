using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.VisualStudio;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal class BreakPointEvent : AsynchronousEvent, IDebugBreakpointBoundEvent2
    {
        public const string IID = "1DDDB704-CF99-4B8A-B746-DABB01DD13A0";

        private readonly MonoPendingBreakpoint _breakpoint;

        public BreakPointEvent(MonoPendingBreakpoint breakpoint)
        {
            _breakpoint = breakpoint;
        }

        #region IDebugBreakpointBoundEvent2 Members

        int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            return _breakpoint.EnumBoundBreakpoints(out ppEnum);
        }

        int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = _breakpoint;
            return VSConstants.S_OK;
        }

        #endregion
    }
}