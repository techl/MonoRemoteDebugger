using System;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.MIDebugEngine;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    [ComVisible(true)]
    [Guid(AD7Guids.EngineString)]
    public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3
    {
        private readonly AsyncDispatcher _dispatcher = new AsyncDispatcher();
        private Guid _programId;

        public static AD7Engine Instance { get; private set; }

        public DebuggedProcess DebuggedProcess { get; private set; }
        public EngineCallback Callback { get; private set; }
        public MonoProcess RemoteProcess { get; private set; }

        public AD7Engine()
        {
            //This call is to initialize the global service provider while we are still on the main thread.
            //Do not remove this this, even though the return value goes unused.
            var globalProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;

            Instance = this;
        }

        public int Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms,
            IDebugEventCallback2 pCallback, enum_ATTACH_REASON dwReason)
        {
            DebugHelper.TraceEnteringMethod();

            rgpPrograms[0].GetProgramId(out _programId);
            _dispatcher.Queue(() => DebuggedProcess.Attach());
            _dispatcher.Queue(() => DebuggedProcess.WaitForAttach());

            Callback.EngineCreated();
            Callback.ProgramCreated();

            this.ProgramCreateEventSent = true;

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
            pguidEngine = AD7Guids.EngineGuid;
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

        public int LaunchSuspended(string pszServer, IDebugPort2 port, string exe, string args, string dir,
            string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput, uint hStdOutput,
            uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
        {
            DebugHelper.TraceEnteringMethod();

            Callback = new EngineCallback(this, ad7Callback);
            DebuggedProcess = new DebuggedProcess(this, IPAddress.Parse(args), Callback);
            DebuggedProcess.ApplicationClosed += _debuggedProcess_ApplicationClosed;
            DebuggedProcess.StartDebugging();

            process = RemoteProcess = new MonoProcess(port);
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

            int result = notify.AddProgramNode(new AD7ProgramNode(DebuggedProcess, id));

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
            // VS Code currently isn't providing a thread Id in certain cases. Work around this by handling null values.
            AD7Thread thread = pThread as AD7Thread;
            _dispatcher.Queue(() => DebuggedProcess.Continue(thread?.GetDebuggedThread()));
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

        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT stepUnit)
        {
            var thread = (AD7Thread) pThread;
            _dispatcher.Queue(() => DebuggedProcess.Step(thread, sk, stepUnit));
            return VSConstants.S_OK;
        }

        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            var thread = (AD7Thread) pThread;
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

        internal bool ProgramCreateEventSent
        {
            get;
            private set;
        }
    }
}