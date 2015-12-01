using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    internal class AD7ErrorBreakpointResolution : IDebugErrorBreakpointResolution2
    {
        public AD7ErrorBreakpointResolution(string msg, enum_BP_ERROR_TYPE errorType = enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING)
        {
            _message = msg;
            _errorType = errorType;
        }

        #region IDebugErrorBreakpointResolution2 Members

        private string _message;
        private enum_BP_ERROR_TYPE _errorType;

        int IDebugErrorBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            pBPType[0] = enum_BP_TYPE.BPT_CODE;

            return Constants.S_OK;
        }

        int IDebugErrorBreakpointResolution2.GetResolutionInfo(enum_BPERESI_FIELDS dwFields, BP_ERROR_RESOLUTION_INFO[] info)
        {
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_BPRESLOCATION) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_PROGRAM) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_THREAD) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_MESSAGE) != 0)
            {
                info[0].dwFields |= enum_BPERESI_FIELDS.BPERESI_MESSAGE;
                info[0].bstrMessage = _message;
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_TYPE) != 0)
            {
                info[0].dwFields |= enum_BPERESI_FIELDS.BPERESI_TYPE;
                info[0].dwType = _errorType;
            }

            return Constants.S_OK;
        }

        #endregion
    }
}
