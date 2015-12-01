using System;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.MIDebugEngine;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    public class MonoDebuggerEvents
    {
        private readonly IDebugEventCallback2 _callback;
        private readonly AD7Engine _engine;

        public MonoDebuggerEvents(AD7Engine monoEngine, IDebugEventCallback2 pCallback)
        {
            _engine = monoEngine;
            _callback = pCallback;
        }

        public void EngineCreated()
        {
            var iid = new Guid(AD7EngineCreateEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new AD7EngineCreateEvent(_engine), ref iid,
                AD7AsynchronousEvent.Attributes);
        }

        public void ProgramCreated()
        {
            var iid = new Guid(AD7ProgramCreateEvent.IID);
            _callback.Event(_engine, null, _engine, null, new AD7ProgramCreateEvent(), ref iid,
                AD7AsynchronousEvent.Attributes);
        }

        public void EngineLoaded()
        {
            var iid = new Guid(AD7LoadCompleteEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new AD7LoadCompleteEvent(), ref iid,
                AD7StoppingEvent.Attributes);
        }

        internal void DebugEntryPoint()
        {
            var iid = new Guid(AD7EntryPointEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new AD7EntryPointEvent(), ref iid,
                AD7AsynchronousEvent.Attributes);
        }

        internal void ProgramDestroyed(IDebugProgram2 program)
        {
            var iid = new Guid(AD7ProgramDestroyEvent.IID);
            _callback.Event(_engine, null, program, null, new AD7ProgramDestroyEvent(0), ref iid,
                AD7AsynchronousEvent.Attributes);
        }

        internal void BoundBreakpoint(AD7PendingBreakpoint breakpoint)
        {
            var iid = new Guid(AD7BreakpointBoundEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new AD7BreakpointBoundEvent(breakpoint), ref iid,
                AD7AsynchronousEvent.Attributes);
        }

        internal void BreakpointHit(AD7PendingBreakpoint breakpoint, MonoThread thread)
        {
            var iid = new Guid(AD7BreakpointEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new AD7BreakpointEvent(breakpoint), ref iid,
                AD7StoppingEvent.Attributes);
        }

        internal void ThreadStarted(MonoThread thread)
        {
            var iid = new Guid(AD7ThreadCreateEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new AD7ThreadCreateEvent(), ref iid,
                AD7StoppingEvent.Attributes);
        }

        internal void StepCompleted(MonoThread thread)
        {
            var iid = new Guid(AD7StepCompleteEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new AD7StepCompleteEvent(), ref iid,
                AD7StoppingEvent.Attributes);
        }
    }
}