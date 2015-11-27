using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class TrivialMonoExpression : IDebugExpression2
    {
        private readonly MonoProperty _monoProperty;

        public TrivialMonoExpression(MonoProperty monoProperty)
        {
            _monoProperty = monoProperty;
        }

        public int Abort()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int EvaluateAsync(enum_EVALFLAGS dwFlags, IDebugEventCallback2 pExprCallback)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int EvaluateSync(enum_EVALFLAGS dwFlags, uint dwTimeout, IDebugEventCallback2 pExprCallback,
            out IDebugProperty2 ppResult)
        {
            ppResult = _monoProperty;
            return VSConstants.S_OK;
        }
    }
}