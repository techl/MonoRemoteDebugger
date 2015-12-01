using System;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    [ComVisible(true)]
    [Guid(MonoGuids.EngineString)]
    public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3
    {
        private readonly AsyncDispatcher _dispatcher = new AsyncDispatcher();
        private Guid _programId;

        public AD7Engine()
        {
            Instance = this;
        }

        public static AD7Engine Instance { get; private set; }

        public DebuggedMonoProcess DebuggedProcess { get; private set; }
        public MonoDebuggerEvents Callback { get; private set; }
        public MonoProgramNode Node { get; private set; }
        public MonoProcess RemoteProcess { get; private set; }

        public int Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms,
            IDebugEventCallback2 pCallback, enum_ATTACH_REASON dwReason)
        {
            DebugHelper.TraceEnteringMethod();

            rgpPrograms[0].GetProgramId(out _programId);
            _dispatcher.Queue(() => DebuggedProcess.Attach());
            _dispatcher.Queue(() => DebuggedProcess.WaitForAttach());

            Callback.EngineCreated();
            Callback.ProgramCreated();


            return VSConstants.S_OK;
        }

        public int CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            DebugHelper.TraceEnteringMethod();

            AD7PendingBreakpoint breakpoint = DebuggedProcess.AddPendingBreakpoint(pBPRequest);
            ppPendingBP = breakpoint;

            return VSConstants.S_OK;
        }

        public int CauseBreak()
        {
            DebugHelper.TraceEnteringMethod();
            _dispatcher.Queue(() => DebuggedProcess.Break());
            return VSConstants.S_OK;
        }

        public int ContinueFromSynchronousEvent(IDebugEvent2 pEvent)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int DestroyProgram(IDebugProgram2 pProgram)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            DebugHelper.TraceEnteringMethod();
            ppEnum = null;
            return VSConstants.S_OK;
        }

        public int GetEngineId(out Guid pguidEngine)
        {
            DebugHelper.TraceEnteringMethod();
            pguidEngine = MonoGuids.EngineGuid;
            return VSConstants.S_OK;
        }

        public int RemoveAllSetExceptions(ref Guid guidType)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int RemoveSetException(EXCEPTION_INFO[] pException)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int SetException(EXCEPTION_INFO[] pException)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int SetLocale(ushort wLangID)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int SetMetric(string pszMetric, object varValue)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int SetRegistryRoot(string pszRegistryRoot)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int LaunchSuspended(string pszServer, IDebugPort2 pPort, string pszExe, string pszArgs, string pszDir,
            string bstrEnv, string pszOptions, enum_LAUNCH_FLAGS dwLaunchFlags, uint hStdInput, uint hStdOutput,
            uint hStdError, IDebugEventCallback2 pCallback, out IDebugProcess2 ppProcess)
        {
            DebugHelper.TraceEnteringMethod();

            Callback = new MonoDebuggerEvents(this, pCallback);
            DebuggedProcess = new DebuggedMonoProcess(this, IPAddress.Parse(pszArgs));
            DebuggedProcess.ApplicationClosed += _debuggedProcess_ApplicationClosed;

            ppProcess = RemoteProcess = new MonoProcess(pPort);
            return VSConstants.S_OK;
        }

        public int ResumeProcess(IDebugProcess2 pProcess)
        {
            DebugHelper.TraceEnteringMethod();
            IDebugPort2 port;
            pProcess.GetPort(out port);
            Guid id;
            pProcess.GetProcessId(out id);
            var defaultPort = (IDebugDefaultPort2) port;
            IDebugPortNotify2 notify;
            defaultPort.GetPortNotify(out notify);

            int result = notify.AddProgramNode(Node = new MonoProgramNode(DebuggedProcess, id));

            return VSConstants.S_OK;
        }

        public int TerminateProcess(IDebugProcess2 pProcess)
        {
            DebugHelper.TraceEnteringMethod();
            _dispatcher.Queue(() => DebuggedProcess.Terminate());
            _dispatcher.Stop();
            Callback.ProgramDestroyed(this);
            return VSConstants.S_OK;
        }

        public int CanTerminateProcess(IDebugProcess2 pProcess)
        {
            DebugHelper.TraceEnteringMethod();
            return VSConstants.S_OK;
        }

        public int CanDetach()
        {
            return VSConstants.S_OK;
        }

        public int Continue(IDebugThread2 pThread)
        {
            _dispatcher.Queue(() => DebuggedProcess.Continue());
            return VSConstants.S_OK;
        }

        public int Detach()
        {
            _dispatcher.Queue(() => DebuggedProcess.Terminate());
            return VSConstants.S_OK;
        }

        public int Terminate()
        {
            _dispatcher.Queue(() => DebuggedProcess.Terminate());
            return VSConstants.S_OK;
        }

        public int Attach(IDebugEventCallback2 pCallback)
        {
            throw new NotImplementedException();
        }

        public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int EnumCodePaths(string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource,
            out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
        {
            ppEnum = null;
            ppSafety = null;
            return VSConstants.E_NOTIMPL;
        }

        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int Execute()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            var thread = (MonoThread) pThread;
            _dispatcher.Queue(() => DebuggedProcess.Step(thread, sk));
            return VSConstants.S_OK;
        }

        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            var thread = (MonoThread) pThread;
            _dispatcher.Queue(() => DebuggedProcess.Execute(thread));
            return VSConstants.S_OK;
        }

        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new NotImplementedException();
        }

        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext,
            out IDebugDisassemblyStream2 ppDisassemblyStream)
        {
            ppDisassemblyStream = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetENCUpdate(out object ppUpdate)
        {
            ppUpdate = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            ppMemoryBytes = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetProcess(out IDebugProcess2 ppProcess)
        {
            ppProcess = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetProgramId(out Guid pguidProgramId)
        {
            pguidProgramId = _programId;
            return VSConstants.S_OK;
        }

        public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            throw new NotImplementedException();
        }

        private void _debuggedProcess_ApplicationClosed(object sender, EventArgs e)
        {
            _dispatcher.Stop();
            Callback.ProgramDestroyed(this);
        }
    }
}