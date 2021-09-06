namespace NFTGenerator
{
    internal static class GifHandler
    {
        public static void MergeGifs(string first, string second, string res = "out.gif")
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = GetMergeCommnd(first, second, res);
            process.StartInfo = startInfo;
            process.Start();
        }

        private static string GetMergeCommnd(string first, string second, string res) => "/C magick convert " + first + " -coalesce null: " + second + " -gravity center -layers composite " + res;
    }
}