using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    internal class MonoDocument : IDebugDocument2
    {
        private readonly AD7PendingBreakpoint _pendingBreakpoint;

        public MonoDocument(AD7PendingBreakpoint pendingBreakpoint)
        {
            _pendingBreakpoint = pendingBreakpoint;
        }

        public int GetDocumentClassId(out Guid pclsid)
        {
            throw new NotImplementedException();
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrFileName)
        {
            gnType = enum_GETNAME_TYPE.GN_FILENAME;
            pbstrFileName = Path.GetFileName(_pendingBreakpoint.DocumentName);
            return VSConstants.S_OK;
        }
    }
}