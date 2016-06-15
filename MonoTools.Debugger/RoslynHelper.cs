using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Mono.Debugger.Soft;
using MonoTools.Debugger.VisualStudio;
using NLog;
using Location = Microsoft.CodeAnalysis.Location;
using System.IO;
using Microsoft.MIDebugEngine;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MonoTools.Debugger
{
    internal class RoslynHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal static MITextPosition GetStatementRange(string fileName, int startLine, int startColumn)
        {
            try
            {
                logger.Trace("Line: {0} Column: {1} Source: {2}", startLine, startColumn, fileName);

                using (var stream = File.OpenRead(fileName))
                {
                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path: fileName);
                    SourceText text = syntaxTree.GetText();
                    var root = (CompilationUnitSyntax)syntaxTree.GetRoot();

                    var span = new TextSpan(text.Lines[startLine - 1].Start + startColumn, 1);
                    SyntaxNode node = root.FindNode(span, false, false);

                    if (node is BlockSyntax)
                        return MapBlockSyntax(span, node, fileName);
                    while (node is TypeSyntax || node is MemberAccessExpressionSyntax)
                        node = node.Parent;

                    Location location = node.GetLocation();
                    FileLinePositionSpan mapped = location.GetMappedLineSpan();

                    return new MITextPosition(fileName,
                        new TEXT_POSITION() { dwLine = (uint)mapped.StartLinePosition.Line, dwColumn = (uint)mapped.StartLinePosition.Character },
                        new TEXT_POSITION() { dwLine = (uint)mapped.EndLinePosition.Line, dwColumn = (uint)mapped.EndLinePosition.Character });
                }
            }
            catch (Exception ex)
            {
                logger.Trace($"Exception : {ex}");
                return null;
            }
        }

        private static MITextPosition MapBlockSyntax(TextSpan span, SyntaxNode node, string fileName)
        {
            var block = (BlockSyntax)node;
            bool start = Math.Abs(block.SpanStart - span.Start) < Math.Abs(block.Span.End - span.Start);

            Location location = block.GetLocation();
            FileLinePositionSpan mapped = location.GetMappedLineSpan();

            if (start)
            {
                return new MITextPosition(fileName,
                    new TEXT_POSITION() { dwLine = (uint)mapped.StartLinePosition.Line, dwColumn = (uint)mapped.StartLinePosition.Character },
                    new TEXT_POSITION() { dwLine = (uint)mapped.StartLinePosition.Line, dwColumn = (uint)mapped.StartLinePosition.Character + 1 });
            }

            return new MITextPosition(fileName,
                new TEXT_POSITION() { dwLine = (uint)mapped.EndLinePosition.Line, dwColumn = (uint)mapped.EndLinePosition.Character - 1 },
                new TEXT_POSITION() { dwLine = (uint)mapped.EndLinePosition.Line, dwColumn = (uint)mapped.EndLinePosition.Character });
        }

        internal static void GetILOffset(AD7PendingBreakpoint bp, MethodMirror methodMirror, out int ilOffset)
        {
            List<Mono.Debugger.Soft.Location> locations = methodMirror.Locations.ToList();

            foreach (Mono.Debugger.Soft.Location location in locations)
            {
                int line = location.LineNumber;
                int column = location.ColumnNumber;

                if (line != bp.StartLine + 1)
                    continue;
                //if (column != bp.StartColumn)
                //    continue;

                ilOffset = location.ILOffset;

                Console.WriteLine(location.ColumnNumber);
                return;
            }

            throw new Exception("Cant bind breakpoint");
        }
    }
}