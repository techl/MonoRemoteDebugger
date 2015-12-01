using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using MonoRemoteDebugger.Contracts;
using MonoRemoteDebugger.Debugger.VisualStudio;
using NLog;

namespace MonoRemoteDebugger.Debugger
{
    public class DebuggedMonoProcess
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly AD7Engine _engine;
        private readonly IPAddress _ipAddress;
        private readonly List<AD7PendingBreakpoint> _pendingBreakpoints = new List<AD7PendingBreakpoint>();
        private readonly Dictionary<string, TypeSummary> _types = new Dictionary<string, TypeSummary>();
        private volatile bool _isRunning = true;
        private MonoThread _mainThread;
        private VirtualMachine _vm;


        private StepEventRequest currentStepRequest;
        private bool isStepping;
        private IDebugSession session;

        public DebuggedMonoProcess(AD7Engine engine, IPAddress ipAddress)
        {
            _engine = engine;
            _ipAddress = ipAddress;
            Instance = this;
        }

        public static DebuggedMonoProcess Instance { get; private set; }

        public IReadOnlyDictionary<string, TypeSummary> KnownTypes
        {
            get { return _types; }
        }

        public event EventHandler ApplicationClosed;

        internal void StartDebugging()
        {
            if (_vm != null)
                return;

            _vm = VirtualMachineManager.Connect(new IPEndPoint(_ipAddress, 11000));
            _vm.EnableEvents(EventType.AssemblyLoad,
                EventType.ThreadStart,
                EventType.ThreadDeath,
                EventType.AssemblyUnload,
                EventType.UserBreak,
                EventType.Exception,
                EventType.UserLog,
                EventType.KeepAlive,
                EventType.TypeLoad);

            EventSet set = _vm.GetNextEventSet();
            if (set.Events.OfType<VMStartEvent>().Any())
            {
                _mainThread = new MonoThread(this, _engine, set.Events[0].Thread);
                _engine.Callback.ThreadStarted(_mainThread);

                Task.Factory.StartNew(ReceiveThread, TaskCreationOptions.LongRunning);
            }
            else
                throw new Exception("Didnt get VMStart-Event!");
        }

        internal void Attach()
        {
        }

        private void ReceiveThread()
        {
            Thread.Sleep(3000);
            _vm.Resume();

            while (_isRunning)
            {
                try
                {
                    EventSet set = _vm.GetNextEventSet();

                    bool resume = false;
                    foreach (Event ev in set.Events)
                    {
                        logger.Trace(ev);
                        resume = resume || HandleEventSet(ev);
                    }

                    if (resume && _vm != null)
                        _vm.Resume();
                }
                catch (VMNotSuspendedException)
                {
                }
            }
        }

        private bool HandleEventSet(Event ev)
        {
            if (ev.EventType == EventType.Breakpoint)
            {
                HandleBreakPoint((BreakpointEvent) ev);
                return currentStepRequest != null && currentStepRequest.Enabled;
            }
            if (ev.EventType == EventType.Step)
            {
                HandleStep((StepEvent) ev);
                return false;
            }
            switch (ev.EventType)
            {
                case EventType.TypeLoad:
                    var typeEvent = (TypeLoadEvent) ev;
                    RegisterType(typeEvent.Type);
                    if (TryBindBreakpoints() != 0)
                        return false;

                    break;
                case EventType.VMDeath:
                case EventType.VMDisconnect:
                    Disconnect();
                    return false;
                default:
                    logger.Trace(ev);
                    break;
            }

            return true;
        }

        private void HandleStep(StepEvent stepEvent)
        {
            _engine.Callback.StepCompleted(_mainThread);
            logger.Trace("Stepping: {0}:{1}", stepEvent.Method.Name, stepEvent.Location);

            isStepping = false;
        }

        private void HandleBreakPoint(BreakpointEvent bpEvent)
        {
            AD7PendingBreakpoint bp = _pendingBreakpoints.FirstOrDefault(x => x.LastRequest == bpEvent.Request);
            StackFrame[] frames = bpEvent.Thread.GetFrames();
            _engine.Callback.BreakpointHit(bp, _mainThread);
        }

        private int TryBindBreakpoints()
        {
            int countBounded = 0;

            try
            {
                foreach (AD7PendingBreakpoint bp in _pendingBreakpoints.Where(x => !x.IsBound))
                {
                    MonoBreakpointLocation location;
                    if (bp.TryBind(_types, out location))
                    {
                        try
                        {
                            int ilOffset;
                            RoslynHelper.GetILOffset(bp, location.Method, out ilOffset);

                            BreakpointEventRequest request = _vm.SetBreakpoint(location.Method, ilOffset);
                            request.Enable();
                            bp.IsBound = true;
                            bp.LastRequest = request;
                            _engine.Callback.BoundBreakpoint(bp);
                            _vm.Resume();
                            bp.CurrentThread = _mainThread;
                            countBounded++;
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Cant bind breakpoint: " + ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Cant bind breakpoint: " + ex);
            }


            return countBounded;
        }

        private void Disconnect()
        {
            _isRunning = false;
            Terminate();
            if (ApplicationClosed != null)
                ApplicationClosed(this, EventArgs.Empty);
        }

        private void RegisterType(TypeMirror typeMirror)
        {
            if (!_types.ContainsKey(typeMirror.FullName))
            {
                _types.Add(typeMirror.FullName, new TypeSummary
                {
                    TypeMirror = typeMirror,
                });

                string typeName = typeMirror.Name;
                if (!string.IsNullOrEmpty(typeMirror.Namespace))
                    typeName = typeMirror.Namespace + "." + typeMirror.Name;
                logger.Trace("Loaded and registered Type: " + typeName);
            }
        }

        internal void WaitForAttach()
        {
        }

        internal void Break()
        {
        }

        internal void Continue()
        {
        }

        internal void Resume()
        {
            _vm.Resume();
        }

        internal void Execute(MonoThread debuggedMonoThread)
        {
            _vm.Resume();
        }

        internal void Terminate()
        {
            try
            {
                if (_vm != null)
                {
                    _vm.ForceDisconnect();
                    _vm = null;
                }


                session.Disconnect();
            }
            catch
            {
            }
        }

        internal AD7PendingBreakpoint AddPendingBreakpoint(IDebugBreakpointRequest2 pBPRequest)
        {
            var bp = new AD7PendingBreakpoint(_engine, pBPRequest);
            _pendingBreakpoints.Add(bp);
            TryBindBreakpoints();
            return bp;
        }

        internal void Step(MonoThread thread, enum_STEPKIND sk)
        {
            if (isStepping)
                return;

            if (currentStepRequest == null)
                currentStepRequest = _vm.CreateStepRequest(thread.ThreadMirror);
            else
            {
                currentStepRequest.Disable();
            }

            isStepping = true;
            switch (sk)
            {
                case enum_STEPKIND.STEP_INTO:
                    currentStepRequest.Depth = StepDepth.Into;
                    break;
                case enum_STEPKIND.STEP_OUT:
                    currentStepRequest.Depth = StepDepth.Out;
                    break;
                case enum_STEPKIND.STEP_OVER:
                    currentStepRequest.Depth = StepDepth.Over;
                    break;
                default:
                    return;
            }

            currentStepRequest.Size = StepSize.Line;
            currentStepRequest.Enable();
            _vm.Resume();
        }

        public void AssociateDebugSession(IDebugSession session)
        {
            this.session = session;
        }
    }
}