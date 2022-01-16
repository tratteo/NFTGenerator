// Copyright Matteo Beltrame

using Microsoft.Extensions.Logging;
using nQuant;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

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
        ApplyVideoDegradationFilter(target);
        //var quantizer = new WuQuantizer();
        //using var quantized = quantizer.QuantizeImage(target);
        //quantized.Save(res, ImageFormat.Png);
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
        stopwatch.Stop();
        //logger.LogInfo($"Finishing {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
    }

    public static void ApplyVideoDegradationFilter(Bitmap bitmap, int strenght = 15, int width = 3)
    {
        Color[] filters = new Color[] { Color.FromArgb(strenght, 0, 0), Color.FromArgb(0, strenght, 0), Color.FromArgb(0, 0, strenght) };
        int filtersIndex = 0;
        int currentIndex = 0;
        for (int i = 0; i < bitmap.Height; i++)
        {
            Color currentFilter = filters[filtersIndex];
            for (int j = 0; j < bitmap.Width; j++)
            {
                Color pixel = bitmap.GetPixel(j, i);
                bitmap.SetPixel(j, i, Color.FromArgb(pixel.A,
                    Math.Clamp(pixel.R + currentFilter.R, 0, 255),
                    Math.Clamp(pixel.G + currentFilter.G, 0, 255),
                    Math.Clamp(pixel.B + currentFilter.B, 0, 255)));
            }

            if (currentIndex == width)
            {
                currentIndex = 0;
                filtersIndex = filtersIndex + 1 >= filters.Length ? 0 : filtersIndex + 1;
            }
            currentIndex++;
        }
    }
}