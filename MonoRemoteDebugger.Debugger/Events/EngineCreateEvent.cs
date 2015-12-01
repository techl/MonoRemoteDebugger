using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.Events
{
    internal sealed class EngineCreateEvent : AsynchronousEvent, IDebugEngineCreateEvent2
    {
        public const string IID = "FE5B734C-759D-4E59-AB04-F103343BDD06";
        private readonly IDebugEngine2 m_engine;

        public EngineCreateEvent(IDebugEngine2 engine)
        {
            m_engine = engine;
        }

        #region IDebugEngineCreateEvent2 Members

        int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 pEngine)
        {
            pEngine = m_engine;

            return VSConstants.S_OK;
        }

        #endregion
    }
}