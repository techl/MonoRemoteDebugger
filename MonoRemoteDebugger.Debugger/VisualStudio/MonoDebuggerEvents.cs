using System;
using Microsoft.VisualStudio.Debugger.Interop;
using MonoRemoteDebugger.Debugger.Events;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    public class MonoDebuggerEvents
    {
        private readonly IDebugEventCallback2 _callback;
        private readonly AD7Engine _engine;

        public MonoDebuggerEvents(AD7Engine engine, IDebugEventCallback2 pCallback)
        {
            _engine = engine;
            _callback = pCallback;
        }

        public void EngineCreated()
        {
            var iid = new Guid(EngineCreateEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new EngineCreateEvent(_engine), ref iid,
                AsynchronousEvent.Attributes);
        }

        public void ProgramCreated()
        {
            var iid = new Guid(ProgramCreateEvent.IID);
            _callback.Event(_engine, null, _engine, null, new ProgramCreateEvent(), ref iid,
                AsynchronousEvent.Attributes);
        }

        public void EngineLoaded()
        {
            var iid = new Guid(LoadCompleteEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new LoadCompleteEvent(), ref iid,
                StoppingEvent.Attributes);
        }

        internal void DebugEntryPoint()
        {
            var iid = new Guid(DebugEntryPointEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new DebugEntryPointEvent(_engine), ref iid,
                AsynchronousEvent.Attributes);
        }

        internal void ProgramDestroyed(IDebugProgram2 program)
        {
            var iid = new Guid(ProgramDestroyedEvent.IID);
            _callback.Event(_engine, null, program, null, new ProgramDestroyedEvent(), ref iid,
                AsynchronousEvent.Attributes);
        }

        internal void BoundBreakpoint(MonoPendingBreakpoint breakpoint)
        {
            var iid = new Guid(BreakPointEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, null, new BreakPointEvent(breakpoint), ref iid,
                AsynchronousEvent.Attributes);
        }

        internal void BreakpointHit(MonoPendingBreakpoint breakpoint, MonoThread thread)
        {
            var iid = new Guid(BreakPointHitEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new BreakPointHitEvent(breakpoint), ref iid,
                StoppingEvent.Attributes);
        }

        internal void ThreadStarted(MonoThread thread)
        {
            var iid = new Guid(ThreadCreateEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new ThreadCreateEvent(), ref iid,
                StoppingEvent.Attributes);
        }

        internal void StepCompleted(MonoThread thread)
        {
            var iid = new Guid(StepCompleteEvent.IID);
            _callback.Event(_engine, _engine.RemoteProcess, _engine, thread, new StepCompleteEvent(), ref iid,
                StoppingEvent.Attributes);
        }
    }
}