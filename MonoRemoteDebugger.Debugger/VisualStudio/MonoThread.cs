using System;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class MonoThread : IDebugThread2
    {
        private readonly AD7Engine _engine;
        private readonly DebuggedMonoProcess debuggedMonoProcess;
        private string _threadName = "Mono Thread";

        public MonoThread(DebuggedMonoProcess debuggedMonoProcess, AD7Engine engine, ThreadMirror threadMirror)
        {
            this.debuggedMonoProcess = debuggedMonoProcess;
            _engine = engine;
            ThreadMirror = threadMirror;
        }

        public ThreadMirror ThreadMirror { get; private set; }

        public int CanSetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_FALSE;
        }

        public int EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 ppEnum)
        {
            StackFrame[] stackFrames = ThreadMirror.GetFrames();
            ppEnum =
                new MonoFrameInfoEnum(
                    stackFrames.Select(x => new MonoStackFrame(this, debuggedMonoProcess, x).GetFrameInfo(dwFieldSpec)));
            return VSConstants.S_OK;
        }

        public int GetLogicalThread(IDebugStackFrame2 pStackFrame, out IDebugLogicalThread2 ppLogicalThread)
        {
            throw new NotImplementedException();
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = _threadName;
            return VSConstants.S_OK;
        }

        public int GetProgram(out IDebugProgram2 ppProgram)
        {
            ppProgram = _engine;
            return VSConstants.S_OK;
        }

        public int GetThreadId(out uint pdwThreadId)
        {
            pdwThreadId = 1234;
            return VSConstants.S_OK;
        }

        public int GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] ptp)
        {
            return VSConstants.S_OK;
        }

        public int Resume(out uint pdwSuspendCount)
        {
            pdwSuspendCount = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int SetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_FALSE;
        }

        public int SetThreadName(string pszName)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int Suspend(out uint pdwSuspendCount)
        {
            pdwSuspendCount = 0;
            return VSConstants.E_NOTIMPL;
        }
    }
}