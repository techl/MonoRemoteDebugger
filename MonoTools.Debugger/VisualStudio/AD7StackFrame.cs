using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using Microsoft.MIDebugEngine;
using System.Diagnostics;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    internal class AD7StackFrame : IDebugStackFrame2, IDebugExpressionContext2
    {
        public AD7Engine Engine { get; private set; }
        public AD7Thread Thread { get; private set; }
        public Mono.Debugger.Soft.StackFrame ThreadContext { get; private set; }

        private string _functionName;
        private MITextPosition _textPosition;

        private readonly AD7DocumentContext docContext;
        private readonly List<MonoProperty> LocalVariables;

        public AD7StackFrame(AD7Engine engine, AD7Thread thread, Mono.Debugger.Soft.StackFrame threadContext)
        {
            Debug.Assert(threadContext != null, "ThreadContext is null");

            Engine = engine;
            this.Thread = thread;
            this.ThreadContext = threadContext;

            _textPosition = RoslynHelper.GetStatementRange(ThreadContext.FileName, ThreadContext.LineNumber, ThreadContext.ColumnNumber);
            _functionName = threadContext.Method.Name;

            //if(threadContext.IsNativeTransition)
            //{

            //}

            if (_textPosition != null)
            {
                docContext = new AD7DocumentContext(_textPosition);
            }

            this.LocalVariables = threadContext.GetVisibleVariables().Select(x => new MonoProperty(threadContext, x)).ToList();
        }

        public int ParseText(string pszCode, enum_PARSEFLAGS dwFlags, uint nRadix, out IDebugExpression2 ppExpr,
            out string pbstrError, out uint pichError)
        {
            pbstrError = "";
            pichError = 0;
            ppExpr = null;
            string lookup = pszCode;


            LocalVariable result = ThreadContext.GetVisibleVariableByName(lookup);
            if (result != null)
            {
                ppExpr = new AD7Expression(new MonoProperty(ThreadContext, result));
                return VSConstants.S_OK;
            }

            pbstrError = "Unsupported Expression";
            pichError = (uint)pbstrError.Length;
            return VSConstants.S_FALSE;
        }

        public int EnumProperties(enum_DEBUGPROP_INFO_FLAGS dwFields, uint nRadix, ref Guid guidFilter, uint dwTimeout,
            out uint pcelt, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = new AD7PropertyInfoEnum(LocalVariables.Select(x => x.GetDebugPropertyInfo(dwFields)).ToArray());
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
            if (docContext != null)
                return docContext.GetLanguageInfo(ref pbstrLanguage, ref pguidLanguage);
            else
                return Constants.S_OK;
            //pbstrLanguage = AD7Guids.LanguageName;
            //pguidLanguage = AD7Guids.guidLanguageCpp;
            //return VSConstants.S_OK;
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = ThreadContext.FileName;
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
            ppThread = Thread;
            return VSConstants.S_OK;
        }

        internal FRAMEINFO GetFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec)
        {
            var frameInfo = new FRAMEINFO();
            frameInfo.m_bstrFuncName = ThreadContext.Location.Method.Name;
            frameInfo.m_bstrModule = ThreadContext.FileName;
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