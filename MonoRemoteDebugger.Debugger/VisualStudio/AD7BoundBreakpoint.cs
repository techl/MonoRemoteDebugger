using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class AD7BoundBreakpoint : IDebugBoundBreakpoint2, IDebugBreakpointResolution2
    {
        private readonly AD7Engine _engine;
        private readonly AD7PendingBreakpoint _pendingBreakpoint;

        public AD7BoundBreakpoint(AD7Engine engine, AD7PendingBreakpoint pendingBreakpoint)
        {
            _engine = engine;
            _pendingBreakpoint = pendingBreakpoint;
        }

        public int Delete()
        {
            return _pendingBreakpoint.Delete();
        }

        public int Enable(int fEnable)
        {
            return _pendingBreakpoint.Enable(fEnable);
        }

        public int GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution)
        {
            ppBPResolution = this;
            return VSConstants.S_OK;
        }

        public int GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = 0;
            return VSConstants.S_OK;
        }

        public int GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = _pendingBreakpoint;
            return VSConstants.S_OK;
        }

        public int GetState(enum_BP_STATE[] pState)
        {
            pState[0] = 0;
            if (_pendingBreakpoint.Deleted)
            {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            }
            else if (_pendingBreakpoint.Enabled)
            {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            }
            else if (!_pendingBreakpoint.Enabled)
            {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }
            return VSConstants.S_OK;
        }

        public int SetCondition(BP_CONDITION bpCondition)
        {
            throw new NotImplementedException();
        }

        public int SetHitCount(uint dwHitCount)
        {
            throw new NotImplementedException();
        }

        public int SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            throw new NotImplementedException();
        }

        public int GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            pBPType[0] = enum_BP_TYPE.BPT_CODE;
            return VSConstants.S_OK;
        }

        public int GetResolutionInfo(enum_BPRESI_FIELDS dwFields, BP_RESOLUTION_INFO[] pBPResolutionInfo)
        {
            if (dwFields == enum_BPRESI_FIELDS.BPRESI_ALLFIELDS)
            {
                pBPResolutionInfo[0].dwFields = enum_BPRESI_FIELDS.BPRESI_PROGRAM;
                pBPResolutionInfo[0].pProgram = _engine;
            }

            return VSConstants.S_OK;
        }
    }
}