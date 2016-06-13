namespace MonoTools.Debugger.VSExtension
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidMonoDebugger_VS2013PkgString = "27d183e9-5d2b-44d6-9ec8-2db329096df7";
        public const string guidMonoDebugger_VS2013CmdSetString = "9ef3ef5e-965c-4443-a78a-947849fba55a";
        public const string guidImagesString = "5f91711c-12a6-4a4e-875a-352c3cf839c8";
        public static Guid guidMonoDebugger_VS2013Pkg = new Guid(guidMonoDebugger_VS2013PkgString);
        public static Guid guidMonoDebugger_VS2013CmdSet = new Guid(guidMonoDebugger_VS2013CmdSetString);
        public static Guid guidImages = new Guid(guidImagesString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int TopLevelMenuGroup = 0x1020;
        public const int cmdRemodeDebugCode = 0x0100;
        public const int cmdLocalDebugCode = 0x0101;
        public const int menuIDMainMenu = 0x0102;
        public const int cmdOpenLogFile = 0x0104;
        public const int publish = 0x0001;
    }
}
