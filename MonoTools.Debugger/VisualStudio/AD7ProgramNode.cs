using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.MIDebugEngine;

namespace MonoTools.Debugger.VisualStudio
{
    public class AD7ProgramNode : IDebugProgramNode2
    {
        private readonly DebuggedProcess _process;
        private readonly Guid _processId;

        public AD7ProgramNode(DebuggedProcess process, Guid processId)
        {
            _process = process;
            _processId = processId;
        }

        public int Attach_V7(IDebugProgram2 pMDMProgram, IDebugEventCallback2 pCallback, uint dwReason)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int DetachDebugger_V7()
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int GetHostMachineName_V7(out string pbstrHostMachineName)
        {
            DebugHelper.TraceEnteringMethod();
            pbstrHostMachineName = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
        {
            DebugHelper.TraceEnteringMethod();
            pbstrEngine = AD7Guids.EngineName;
            pguidEngine = AD7Guids.EngineGuid;
            return VSConstants.S_OK;
        }

        public int GetHostName(enum_GETHOSTNAME_TYPE dwHostNameType, out string pbstrHostName)
        {
            DebugHelper.TraceEnteringMethod();
            pbstrHostName = null;
            _process.StartDebugging();
            return VSConstants.S_OK;
        }

        public int GetHostPid(AD_PROCESS_ID[] pHostProcessId)
        {
            DebugHelper.TraceEnteringMethod();
            pHostProcessId[0].ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pHostProcessId[0].guidProcessId = _processId;
            return VSConstants.S_OK;
        }

        public int GetProgramName(out string pbstrProgramName)
        {
            DebugHelper.TraceEnteringMethod();
            pbstrProgramName = null;
            return VSConstants.E_NOTIMPL;
        }
    }
}