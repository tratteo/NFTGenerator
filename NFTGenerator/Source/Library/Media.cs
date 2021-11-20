// Copyright Matteo Beltrame

using GibNet.Logging;
using System.IO;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposeMedia(string first, string second, string res, Logger logger)
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            Arguments = GetMergeCommand(first, second, res, logger)
        };
        //Logger.LogInfo(startInfo.Arguments);
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
    }

    private static string GetMergeCommand(string first, string second, string res, Logger logger)
    {
        var firstExtension = new FileInfo(first).Extension;
        var secondExtension = new FileInfo(second).Extension;
        if (firstExtension != secondExtension)
        {
            logger.LogError("Unable to combine two different file types: " + firstExtension + " - " + secondExtension);
            return string.Empty;
        }
        else
        {
            //Logger.LogInfo(firstExtension);
            return firstExtension switch
            {
                ".gif" => "/C magick convert " + first + " -coalesce null: " + second + " -gravity center -layers composite " + res,
                ".png" => "/C magick convert " + first + " " + second + " -compose atop -gravity center -composite " + res,
                ".jpeg" => "/C magick convert " + first + " " + second + " -compose atop -gravity center -composite " + res,
                _ => string.Empty
            };
        }
    }
}