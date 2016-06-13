using System;

namespace MonoTools.Debugger.Debugger.VisualStudio
{
    public static class AD7Guids
    {
        public const string EngineString = "8BF3AB9F-3864-449A-93AB-E7B0935FC8F5";
        public const string ProgramProviderString = "CA171DED-5920-4ACD-93C2-BD9E4FA10CA0";

        public const string EngineName = "MonoTools.Debugger";

        public static readonly Guid ProgramProviderGuid = new Guid(ProgramProviderString);
        public static readonly Guid EngineGuid = new Guid(EngineString);


        // Language guid for C++. Used when the language for a document context or a stack frame is requested.
        static private Guid s_guidLanguageCpp = new Guid("3a12d0b7-c26c-11d0-b442-00a0244a1dd2");
        static public Guid guidLanguageCpp
        {
            get { return s_guidLanguageCpp; }
        }

        static private Guid s_guidLanguageCs = new Guid("{3F5162F8-07C6-11D3-9053-00C04FA302A1}");
        static public Guid guidLanguageCs
        {
            get { return s_guidLanguageCs; }
        }

        static private Guid s_guidLanguageC = new Guid("63A08714-FC37-11D2-904C-00C04FA302A1");
        static public Guid guidLanguageC
        {
            get { return s_guidLanguageC; }
        }
    }
}