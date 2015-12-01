using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    internal class AD7DocumentContext : IDebugDocumentContext2, IDebugCodeContext2
    {
        private readonly StatementRange _currentStatementRange;
        private readonly string _fileName;

        public AD7DocumentContext(string fileName, int startLine, int startColumn)
        {
            _fileName = fileName;
            _currentStatementRange = RoslynHelper.GetStatementRange(fileName, startLine, startColumn);
        }

        public int Add(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        {
            throw new NotImplementedException();
        }

        public int Compare(enum_CONTEXT_COMPARE Compare, IDebugMemoryContext2[] rgpMemoryContextSet,
            uint dwMemoryContextSetLen, out uint pdwMemoryContext)
        {
            throw new NotImplementedException();
        }

        public int GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
        {
            ppSrcCxt = this;
            return VSConstants.S_OK;
        }

        public int GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
        {
            pinfo[0].dwFields = enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL | enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS;

            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL) != 0)
            {
                pinfo[0].bstrModuleUrl = _fileName;
                pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL;
            }

            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS) != 0)
            {
                pinfo[0].bstrAddress = _currentStatementRange.StartLine.ToString();
                pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS;
            }

            return VSConstants.S_OK;
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = AD7Guids.LanguageName;
            return VSConstants.S_OK;
        }

        public int Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        {
            throw new NotImplementedException();
        }

        public int Compare(enum_DOCCONTEXT_COMPARE Compare, IDebugDocumentContext2[] rgpDocContextSet,
            uint dwDocContextSetLen, out uint pdwDocContext)
        {
            pdwDocContext = 0;
            return VSConstants.E_NOTIMPL;
        }

        public int EnumCodeContexts(out IEnumDebugCodeContexts2 ppEnumCodeCxts)
        {
            ppEnumCodeCxts = null;
            return VSConstants.S_FALSE;
        }

        public int GetDocument(out IDebugDocument2 ppDocument)
        {
            ppDocument = null; // new MonoDocument(_pendingBreakpoint);
            return VSConstants.S_OK;
        }

        public int GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            pbstrLanguage = AD7Guids.LanguageName;
            pguidLanguage = AD7Guids.LanguageGuid;
            return VSConstants.S_OK;
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            pbstrFileName = _fileName;
            return VSConstants.S_OK;
        }

        public int GetSourceRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            throw new NotImplementedException();
        }

        public int GetStatementRange(TEXT_POSITION[] pBegPosition, TEXT_POSITION[] pEndPosition)
        {
            pBegPosition[0].dwLine = (uint) _currentStatementRange.StartLine;
            pBegPosition[0].dwColumn = (uint) _currentStatementRange.StartColumn;

            pEndPosition[0].dwLine = (uint) _currentStatementRange.EndLine;
            pEndPosition[0].dwColumn = (uint) _currentStatementRange.EndColumn;
            return VSConstants.S_OK;
        }

        public int Seek(int nCount, out IDebugDocumentContext2 ppDocContext)
        {
            ppDocContext = null;
            return VSConstants.E_NOTIMPL;
        }
    }
}