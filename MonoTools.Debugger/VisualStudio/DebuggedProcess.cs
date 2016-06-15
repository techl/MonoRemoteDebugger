using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using MonoTools.Debugger.Contracts;
using MonoTools.Debugger.VisualStudio;
using NLog;
using MonoTools.Debugger;
using System.Globalization;
using MICore;
using MonoTools.Debugger.DebugEngineHost;
using System.Diagnostics;
using Techl;

namespace Microsoft.MIDebugEngine
{
    public class DebuggedProcess
    {
        public AD7Engine Engine
        {
            get
            {
                return _engine;
            }
        }

        private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private readonly AD7Engine _engine;
        private readonly IPAddress _ipAddress;
        private readonly List<AD7PendingBreakpoint> _pendingBreakpoints = new List<AD7PendingBreakpoint>();
        private readonly Dictionary<string, TypeSummary> _types = new Dictionary<string, TypeSummary>();
        private volatile bool _isRunning = true;
        private AD7Thread _mainThread;
        private VirtualMachine _vm;

        private EngineCallback _callback;
        public AD_PROCESS_ID Id { get; private set; }
        public ProcessState ProcessState { get; private set; }

        private StepEventRequest currentStepRequest;
        private bool isStepping;
        private IDebugSession session;

        public DebuggedProcess(AD7Engine engine, IPAddress ipAddress, EngineCallback callback)
        {
            _engine = engine;
            _ipAddress = ipAddress;
            Instance = this;

            // we do NOT have real Win32 process IDs, so we use a guid
            AD_PROCESS_ID pid = new AD_PROCESS_ID();
            pid.ProcessIdType = (int)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pid.guidProcessId = Guid.NewGuid();
            this.Id = pid;

            _callback = callback;
        }

        public static DebuggedProcess Instance { get; private set; }

        public IReadOnlyDictionary<string, TypeSummary> KnownTypes
        {
            get { return _types; }
        }

        public VirtualMachine VM
        {
            get
            {
                return _vm;
            }
            set
            {
                _vm = value;
            }
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
                //TODO: review by techcap
                _mainThread = new AD7Thread(_engine, new DebuggedThread(set.Events[0].Thread, _engine));
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

                    var type = set.Events.First().EventType;
                    if (type != EventType.TypeLoad)
                        Debug.Print($"Event : {set.Events.Select(e => e.EventType).StringJoin(",")}");

                    foreach (Event ev in set.Events)
                    {
                        HandleEventSet(ev);
                    }
                }
                catch (VMNotSuspendedException)
                {
                }
            }
        }

        private void HandleEventSet(Event ev)
        {
            var type = ev.EventType;

            switch (type)
            {
                case EventType.Breakpoint:
                    if (!HandleBreakPoint((BreakpointEvent)ev))
                        return;
                    break;
                case EventType.Step:
                    HandleStep((StepEvent)ev);
                    return;
                case EventType.TypeLoad:
                    var typeEvent = (TypeLoadEvent)ev;
                    RegisterType(typeEvent.Type);
                    TryBindBreakpoints();
                    break;

                case EventType.UserLog:
                    UserLogEvent e = (UserLogEvent)ev;
                    HostOutputWindowEx.WriteLaunchError(e.Message);
                    break;
                case EventType.VMDeath:
                case EventType.VMDisconnect:
                    Disconnect();
                    return;
                default:
                    logger.Trace(ev);
                    break;
            }

            try
            {
                _vm.Resume();
            }
            catch (VMNotSuspendedException)
            {
                if (type != EventType.VMStart && _vm.Version.AtLeast(2, 2))
                    throw;
            }
        }

        private void HandleStep(StepEvent stepEvent)
        {
            if (currentStepRequest != null)
            {
                currentStepRequest.Enabled = false;
                currentStepRequest = null;
            }

            _engine.Callback.StepCompleted(_mainThread);
            logger.Trace("Stepping: {0}:{1}", stepEvent.Method.Name, stepEvent.Location);

            isStepping = false;
        }

        private bool HandleBreakPoint(BreakpointEvent ev)
        {
            if (isStepping)
                return true;

            bool resume = false;
            
            AD7PendingBreakpoint bp;
            lock (_pendingBreakpoints)
                bp = _pendingBreakpoints.FirstOrDefault(x => x.LastRequest == ev.Request);

            if (bp == null)
                return true;
            
            Mono.Debugger.Soft.StackFrame[] frames = ev.Thread.GetFrames();
            _engine.Callback.BreakpointHit(bp, _mainThread);

            return resume;
        }

        private int TryBindBreakpoints()
        {
            int countBounded = 0;

            try
            {
                AD7PendingBreakpoint[] pendingList;
                lock (_pendingBreakpoints)
                    pendingList = _pendingBreakpoints.Where(x => !x.Bound).ToArray();

                foreach (AD7PendingBreakpoint bp in pendingList)
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
                            bp.Bound = true;
                            bp.LastRequest = request;
                            _engine.Callback.BoundBreakpoint(bp);
                            //_vm.Resume();
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

        public void Close()
        {
            //if (_launchOptions.DeviceAppLauncher != null)
            //{
            //    _launchOptions.DeviceAppLauncher.Terminate();
            //}
            //CloseQuietly();
        }

        internal void WaitForAttach()
        {
        }

        internal void Break()
        {
            //TODO: techcap
            _vm.Suspend();
        }

        /// <summary>
        /// On First run
        /// </summary>
        /// <param name="thread"></param>
        internal void Continue(DebuggedThread thread)
        {
            //_vm.Resume();
        }

        //internal void Resume()
        //{
        //    _vm.Resume();
        //}

        /// <summary>
        /// For Run
        /// </summary>
        /// <param name="debuggedMonoThread"></param>
        internal void Execute(AD7Thread debuggedMonoThread)
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

        public void Detach()
        {
            Terminate();
        }

        internal AD7PendingBreakpoint AddPendingBreakpoint(IDebugBreakpointRequest2 pBPRequest)
        {
            var bp = new AD7PendingBreakpoint(_engine, pBPRequest);
            lock (_pendingBreakpoints)
                _pendingBreakpoints.Add(bp);

            TryBindBreakpoints();
            return bp;
        }

        internal void DeletePendingBreakpoint(AD7PendingBreakpoint breakPoint)
        {
            lock (_pendingBreakpoints)
                _pendingBreakpoints.Remove(breakPoint);
        }

        internal void Step(AD7Thread thread, enum_STEPKIND sk, enum_STEPUNIT stepUnit)
        {
            if (!isStepping)
            {
                if (currentStepRequest == null)
                    currentStepRequest = _vm.CreateStepRequest(thread.ThreadMirror);
                else
                {
                    currentStepRequest.Disable();
                }

                isStepping = true;
                if (stepUnit == enum_STEPUNIT.STEP_LINE || stepUnit == enum_STEPUNIT.STEP_STATEMENT)
                {
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
                }
                else if (stepUnit == enum_STEPUNIT.STEP_INSTRUCTION)
                {
                    //TODO: by techcap
                }
                else
                    throw new NotImplementedException();

                currentStepRequest.Size = StepSize.Line;
                currentStepRequest.Enable();
            }

            _vm.Resume();
        }

        public void AssociateDebugSession(IDebugSession session)
        {
            this.session = session;
        }

        //{bhlee
        internal static string UnixPathToWindowsPath(string unixPath)
        {
            return unixPath.Replace('/', '\\');
        }
        //}

        internal void OnPostedOperationError(object sender, Exception e)
        {
            if (this.ProcessState == MICore.ProcessState.Exited)
            {
                return; // ignore exceptions after the process has exited
            }

            string exceptionMessage = e.Message.TrimEnd(' ', '\t', '.', '\r', '\n');
            string userMessage = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_ExceptionInOperation, exceptionMessage);
            _callback.OnError(userMessage);
        }
    }
}