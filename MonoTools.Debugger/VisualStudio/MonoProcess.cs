using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoTools.Debugger.VisualStudio
{
    public class MonoProcess : IDebugProcess3
    {
        private readonly IDebugPort2 _port;

        public MonoProcess(IDebugPort2 pPort)
        {
            Id = Guid.NewGuid();
            _port = pPort;
        }

        public Guid Id { get; private set; }

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines,
            int[] rghrEngineAttach)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int CanDetach()
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int CauseBreak()
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int Continue(IDebugThread2 pThread)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int Detach()
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int DisableENC(EncUnavailableReason reason)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            DebugHelper.TraceEnteringMethod();
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            DebugHelper.TraceEnteringMethod();
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Execute(IDebugThread2 pThread)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.E_NOTIMPL;
        }

        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetDebugReason(enum_DEBUG_REASON[] pReason)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetENCAvailableState(EncUnavailableReason[] pReason)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetEngineFilter(GUID_ARRAY[] pEngineArray)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetHostingProcessLanguage(out Guid pguidLang)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            DebugHelper.TraceEnteringMethod();
            pProcessId[0].ProcessIdType = (uint) enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pProcessId[0].guidProcessId = Id;
            return VSConstants.S_OK;
        }

        public int GetPort(out IDebugPort2 ppPort)
        {
            DebugHelper.TraceEnteringMethod();
            ppPort = _port;
            return VSConstants.S_OK;
        }

        public int GetProcessId(out Guid pguidProcessId)
        {
            DebugHelper.TraceEnteringMethod();
            pguidProcessId = Id;
            return VSConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int SetHostingProcessLanguage(ref Guid guidLang)
        {
            DebugHelper.TraceEnteringMethod();
            throw new NotImplementedException();
        }

        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int Terminate()
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }
    }
}