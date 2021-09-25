using System.Diagnostics;

namespace NFTGenerator
{
    internal static class Processer
    {
        public static Process Compose(string command, bool enableEvents = false)
        {
            Process process = new Process()
            {
                EnableRaisingEvents = enableEvents
            };
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = "/C " + command
            };
            process.StartInfo = startInfo;
            return process;
        }
    }
}