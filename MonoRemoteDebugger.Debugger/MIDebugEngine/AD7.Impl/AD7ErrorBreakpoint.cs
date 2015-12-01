using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    internal class AD7ErrorBreakpoint : IDebugErrorBreakpoint2
    {
        private AD7PendingBreakpoint _pending;
        private string _error;
        private enum_BP_ERROR_TYPE _errorType;

        public AD7ErrorBreakpoint(AD7PendingBreakpoint pending, string errormsg, enum_BP_ERROR_TYPE errorType = enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING)
        {
            _pending = pending;
            _error = errormsg;
            _errorType = errorType;
        }

        #region IDebugErrorBreakpoint2 Members

        public int GetBreakpointResolution(out IDebugErrorBreakpointResolution2 ppErrorResolution)
        {
            ppErrorResolution = new AD7ErrorBreakpointResolution(_error, _errorType);
            return Constants.S_OK;
        }

        public int GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = _pending;
            return Constants.S_OK;
        }

        #endregion
    }
}
