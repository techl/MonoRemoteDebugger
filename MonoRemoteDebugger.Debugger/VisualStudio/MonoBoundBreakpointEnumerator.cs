using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class MonoBoundBreakpointEnumerator : MonoEnumerator<IDebugBoundBreakpoint2, IEnumDebugBoundBreakpoints2>,
        IEnumDebugBoundBreakpoints2
    {
        public MonoBoundBreakpointEnumerator(IEnumerable<AD7BoundBreakpoint> data)
            : base(data)
        {
        }

        public int Next(uint celt, IDebugBoundBreakpoint2[] rgelt, ref uint celtFetched)
        {
            return Next(celt, rgelt, out celtFetched);
        }
    }
}