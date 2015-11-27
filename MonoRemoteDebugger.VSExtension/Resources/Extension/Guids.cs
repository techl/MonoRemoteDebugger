// Guids.cs
// MUST match guids.h

using System;

namespace MonoDebugger.VS2013.Resources.Extension
{
    internal static class GuidList
    {
        public const string GuidMonoDebuggerVs2013PkgString = "15538f63-0557-4a8c-8ed4-a842d6f6f4db";
        public const string GuidMonoDebuggerVs2013CmdSetString = "7951fdf3-b04d-41d6-9917-3fa9d554cdcd";
        public static readonly Guid GuidMonoDebuggerVs2013CmdSet = new Guid(GuidMonoDebuggerVs2013CmdSetString);
    };
}