// Guids.cs
// MUST match guids.h

using System;

namespace MonoRemoteDebugger.VSExtension
{
    internal static class GuidList
    {
        public const string guidMonoDebugger_VS2013PkgString = "27D183E9-5D2B-44D6-9EC8-2DB329096DF7";
        public const string guidMonoDebugger_VS2013CmdSetString = "9EF3EF5E-965C-4443-A78A-947849FBA55A";

        public static readonly Guid guidMonoDebugger_VS2013CmdSet = new Guid(guidMonoDebugger_VS2013CmdSetString);
    };
}