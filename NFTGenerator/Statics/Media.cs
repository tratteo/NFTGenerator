// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposePNG(string res, ILogger logger, params IMediaProvider[] mediaProvider)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Restart();
        Bitmap[] maps = new Bitmap[mediaProvider.Length];
        for (int i = 0; i < maps.Length; i++)
        {
            //logger.LogInfo(mediaProvider[i].ProvideMediaPath());
            maps[i] = new Bitmap(mediaProvider[i].ProvideMediaPath());
        }
        stopwatch.Stop();
        //logger.LogInfo($"Created bitmaps {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
        stopwatch.Restart();
        var width = maps[0].Width;
        var height = maps[0].Height;
        var target = new Bitmap(width, height);
        using var composed = Graphics.FromImage(target);
        Rectangle rect = new Rectangle(0, 0, width, height);
        stopwatch.Stop();
        //logger.LogInfo($"Created graphic {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
        stopwatch.Restart();
        for (int i = 0; i < maps.Length; i++)
        {
            composed.DrawImage(maps[i], rect);
            maps[i].Dispose();
        }
        stopwatch.Stop();
        // logger.LogInfo($"Merges bitmaps {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
        stopwatch.Restart();
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
        stopwatch.Stop();
        //logger.LogInfo($"Finishing {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
    }
}