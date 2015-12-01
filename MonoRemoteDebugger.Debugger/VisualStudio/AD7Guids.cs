using System;

namespace MonoRemoteDebugger.Debugger.VisualStudio
{
    public static class AD7Guids
    {
        public const string EngineString = "8BF3AB9F-3864-449A-93AB-E7B0935FC8F5";
        public const string ProgramProviderString = "CA171DED-5920-4ACD-93C2-BD9E4FA10CA0";

        public const string CSharpLanguageString = "3f5162f8-07c6-11d3-9053-00c04fa302a1"; //CorSym_LanguageType_CSharp 
        public const string EngineName = "MonoRemoteDebugger";
        public const string LanguageName = "Mono";

        public static readonly Guid ProgramProviderGuid = new Guid(ProgramProviderString);
        public static readonly Guid EngineGuid = new Guid(EngineString);
        public static readonly Guid LanguageGuid = new Guid(CSharpLanguageString);
    }
}