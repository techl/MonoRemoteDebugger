using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    [ComVisible(true)]
    [Guid(AD7Guids.ProgramProviderString)]
    public class AD7ProgramProvider : IDebugProgramProvider2
    {
        public int GetProviderProcessData(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId,
            CONST_GUID_ARRAY EngineFilter, PROVIDER_PROCESS_DATA[] pProcess)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int GetProviderProgramNode(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId,
            ref Guid guidEngine, ulong programId, out IDebugProgramNode2 ppProgramNode)
        {
            DebugHelper.TraceEnteringMethod();
            ppProgramNode = null;
            return VSConstants.S_OK;
        }

        public int SetLocale(ushort wLangID)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int WatchForProviderEvents(enum_PROVIDER_FLAGS Flags, IDebugDefaultPort2 pPort, AD_PROCESS_ID ProcessId,
            CONST_GUID_ARRAY EngineFilter, ref Guid guidLaunchingEngine, IDebugPortNotify2 pEventCallback)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }
    }
}