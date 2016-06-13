using Mono.Debugger.Soft;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    internal class MonoBreakpointLocation
    {
        public MethodMirror Method { get; set; }
        public long Offset { get; set; }
    }
}