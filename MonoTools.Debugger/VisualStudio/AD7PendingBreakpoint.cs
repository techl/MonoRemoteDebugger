using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Mono.Debugger.Soft;
using Location = Microsoft.CodeAnalysis.Location;
using System.IO;
using NLog;
using Microsoft.MIDebugEngine;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    internal class AD7PendingBreakpoint : IDebugPendingBreakpoint2
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly AD7Engine _engine;
        private readonly IDebugBreakpointRequest2 _pBPRequest;
        private AD7BoundBreakpoint _boundBreakpoint;
        private BP_REQUEST_INFO _bpRequestInfo;

        public AD7PendingBreakpoint(AD7Engine engine, IDebugBreakpointRequest2 pBPRequest)
        {
            var requestInfo = new BP_REQUEST_INFO[1];
            pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION, requestInfo);
            _bpRequestInfo = requestInfo[0];
            _pBPRequest = pBPRequest;
            _engine = engine;

            Enabled = true;

            var docPosition =
                (IDebugDocumentPosition2) Marshal.GetObjectForIUnknown(_bpRequestInfo.bpLocation.unionmember2);

            string documentName;
            docPosition.GetFileName(out documentName);
            var startPosition = new TEXT_POSITION[1];
            var endPosition = new TEXT_POSITION[1];
            docPosition.GetRange(startPosition, endPosition);

            DocumentName = documentName;
            StartLine = (int) startPosition[0].dwLine;
            StartColumn = (int) startPosition[0].dwColumn;

            EndLine = (int) endPosition[0].dwLine;
            EndColumn = (int) endPosition[0].dwColumn;
        }

        public bool Bound { get; set; }
        public bool Enabled { get; private set; }
        public bool Deleted { get; private set; }
        public int StartLine { get; private set; }
        public int StartColumn { get; private set; }
        public int EndLine { get; private set; }
        public int EndColumn { get; private set; }
        public string DocumentName { get; set; }
        public AD7Thread CurrentThread { get; set; }
        public EventRequest LastRequest { get; set; }

        public int Bind()
        {
            try
            {
                _boundBreakpoint = new AD7BoundBreakpoint(_engine, this);
                return VSConstants.S_OK;
            }
            catch
            {
                return VSConstants.S_FALSE;
            }
        }

        public int CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
        {
            ppErrorEnum = null;
            if (_bpRequestInfo.bpLocation.bpLocationType == (uint) enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
                return VSConstants.S_OK;

            return VSConstants.S_FALSE;
        }

        public int Delete()
        {
            Deleted = true;

            try
            {
                LastRequest.Disable();
            }
            catch (VMDisconnectedException) { }

            _engine.DebuggedProcess.DeletePendingBreakpoint(this);

            return VSConstants.S_OK;
        }

        public int Enable(int fEnable)
        {
            Enabled = fEnable != 0;
            return VSConstants.S_OK;
        }

        public int EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = new AD7BoundBreakpointsEnum(new[] {_boundBreakpoint});
            return VSConstants.S_OK;
        }

        public int EnumErrorBreakpoints(enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
        {
            ppBPRequest = _pBPRequest;
            return VSConstants.S_OK;
        }

        public int GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (Deleted)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DELETED;
            }
            else if (Enabled)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_ENABLED;
            }
            else if (!Enabled)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DISABLED;
            }
            return VSConstants.S_OK;
        }

        public int SetCondition(BP_CONDITION bpCondition)
        {
            throw new NotImplementedException();
        }

        public int SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            throw new NotImplementedException();
        }

        public int Virtualize(int fVirtualize)
        {
            return VSConstants.S_OK;
        }

        internal bool TryBind(Dictionary<string, TypeSummary> types, out MonoBreakpointLocation breakpointLocation)
        {
            try
            {
                using (var stream = File.OpenRead(DocumentName))
                {
                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path:DocumentName);
                    TextLine textLine = syntaxTree.GetText().Lines[StartLine];
                    Location location = syntaxTree.GetLocation(textLine.Span);
                    SyntaxTree sourceTree = location.SourceTree;
                    SyntaxNode node = location.SourceTree.GetRoot().FindNode(location.SourceSpan, true, true);

                    var method = GetParentMethod<MethodDeclarationSyntax>(node.Parent);
                    string methodName = method.Identifier.Text;

                    var cl = GetParentMethod<ClassDeclarationSyntax>(method);
                    string className = cl.Identifier.Text;

                    var ns = GetParentMethod<NamespaceDeclarationSyntax>(method);
                    string nsname = ns.Name.ToString();

                    string name = string.Format("{0}.{1}", nsname, className);
                    TypeSummary summary;
                    if (types.TryGetValue(name, out summary))
                    {
                        MethodMirror methodMirror = summary.Methods.FirstOrDefault(x => x.Name == methodName);

                        if (methodMirror != null)
                        {
                            breakpointLocation = new MonoBreakpointLocation
                            {
                                Method = methodMirror,
                                Offset = 0,
                            };
                            return true;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Trace($"Exception : {ex}");
            }

            breakpointLocation = null;
            return false;
        }

        private T GetParentMethod<T>(SyntaxNode node) where T : SyntaxNode
        {
            if (node == null)
                return null;

            if (node is T)
                return node as T;
            return GetParentMethod<T>(node.Parent);
        }
    }
}