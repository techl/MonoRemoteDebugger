using System.Net.Sockets;

namespace MonoRemoteDebugger.SharedLib
{
    internal static class ExtensionMethods
    {
        internal static bool IsSocketConnected(this Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
            {
                return false;
            }
            return true;
        }
    }
}