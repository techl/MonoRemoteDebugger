using Ionic.Zip;

namespace MonoRemoteDebugger.SharedLib
{
    public static class ZipFile
    {
        public static void CreateFromDirectory(string directory, string targetZip)
        {
            using (var zip = new Ionic.Zip.ZipFile())
            {
                zip.AddDirectory(directory);
                zip.Save(targetZip);
            }
        }

        public static void ExtractToDirectory(string ZipFileName, string targetDirectory)
        {
            using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(ZipFileName))
            {
                foreach (ZipEntry e in zip)
                {
                    e.Extract(targetDirectory);
                }
            }
        }
    }
}