using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using Microsoft.MIDebugEngine;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        private readonly AD7DocumentContext docContext;
        private readonly StackFrame frame;
        private readonly List<MonoProperty> locals;
        private readonly AD7Thread thread;
        private readonly DebuggedProcess debuggedMonoProcess;

        public AD7StackFrame(AD7Thread thread, DebuggedProcess debuggedMonoProcess, StackFrame frame)
        {
            this.thread = thread;
            this.debuggedMonoProcess = debuggedMonoProcess;
            this.frame = frame;
            
            docContext = new AD7DocumentContext(this.frame.FileName,
                this.frame.LineNumber,
                this.frame.ColumnNumber);
            var locals = frame.GetVisibleVariables().ToList();
            
            this.locals = locals.Select(x => new MonoProperty(frame, x)).ToList();
        }

        public int ParseText(string pszCode, enum_PARSEFLAGS dwFlags, uint nRadix, out IDebugExpression2 ppExpr,
            out string pbstrError, out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = null;
            string lookup = pszCode;


            LocalVariable result = frame.GetVisibleVariableByName(lookup);
            if (result != null)
            {
                ppExpr = new AD7Expression(new MonoProperty(frame, result));
                return VSConstants.S_OK;
            }

            pbstrError = "Unsupported Expression";
            pichError = (uint) pbstrError.Length;
            return VSConstants.S_FALSE;
        }

        public int EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout,
            out uint pcelt, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = new AD7PropertyInfoEnum(locals.Select(x => x.GetDebugPropertyInfo(dwFields)).ToArray());
            ppEnum.GetCount(out pcelt);
            return VSConstants.S_OK;
        }

        public int GetCodeContext(out IDebugCodeContext2 ppCodeCxt)
        {
            ppCodeCxt = docContext;
            return VSConstants.S_OK;
        }

        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            ppProperty = null; // _locals;
            return VSConstants.S_OK;
        }

        public int GetDocumentContext(out IDebugDocumentContext2 ppCxt)
        {
            ppCxt = docContext;
            return VSConstants.S_OK;
        }

        public int GetExpressionContext(out IDebugExpressionContext2 ppExprCxt)
        {
            ppExprCxt = this;
            return VSConstants.S_OK;
        }

        public int GetInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, FRAMEINFO[] pFrameInfo)
        {
            pFrameInfo[0] = GetFrameInfo(dwFieldSpec);
            return VSConstants.S_OK;
        }

        public int GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = AD7Guids.LanguageName;
            pguidLanguage = AD7Guids.LanguageGuid;
            return VSConstants.S_OK;
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = frame.FileName;
            return VSConstants.S_OK;
        }

        public int GetPhysicalStackRange(out ulong paddrMin, out ulong paddrMax)
        {
            paddrMin = 0;
            paddrMax = 0;
            return VSConstants.S_OK;
        }

        public int GetThread(out IDebugThread2 ppThread)
        {
            ppThread = thread;
            return VSConstants.S_OK;
        }

        internal FRAMEINFO GetFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec)
        {
            var frameInfo = new FRAMEINFO();
            frameInfo.m_bstrFuncName = frame.Location.Method.Name;
            frameInfo.m_bstrModule = frame.FileName;
            frameInfo.m_pFrame = this;
            frameInfo.m_fHasDebugInfo = 1;
            frameInfo.m_fStaleCode = 0;

            frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_STALECODE;
            frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_DEBUGINFO;
            frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_MODULE;
            frameInfo.m_dwValidFields |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME;
            return frameInfo;
        }
    }
}