using System;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using MonoRemoteDebugger.Debugger.VisualStudio;

namespace Microsoft.MIDebugEngine
{
    internal class AD7Thread : IDebugThread2
    {
        private readonly AD7Engine _engine;
        private readonly DebuggedThread _debuggedThread;
        private string _threadName = "Mono Thread";

        public ThreadMirror ThreadMirror { get; private set; }

        public AD7Thread(AD7Engine engine, DebuggedThread debuggedThread)//ThreadMirror threadMirror)
        {
            _engine = engine;
            
            ThreadMirror = debuggedThread.Thread;
            _debuggedThread = debuggedThread;
        }

        public int CanSetNextStatement(IDebugStackFrame2 pStackFrame, IDebugCodeContext2 pCodeContext)
        {
            return VSConstants.S_FALSE;
        }

        public int EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 ppEnum)
        {
            StackFrame[] stackFrames = ThreadMirror.GetFrames();
            ppEnum = new AD7FrameInfoEnum(stackFrames.Select(x => new AD7StackFrame(_engine, this, x).GetFrameInfo(dwFieldSpec)).ToArray());
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

        internal DebuggedThread GetDebuggedThread()
        {
            return _debuggedThread;
        }
    }
}