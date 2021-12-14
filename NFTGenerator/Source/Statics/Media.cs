// Copyright Matteo Beltrame

using HandierCli;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposePNG(string res, Logger logger, params Bitmap[] bitmaps)
    {
        Bitmap[] maps = new Bitmap[bitmaps.Length];
        for (int i = 0; i < bitmaps.Length; i++)
        {
            lock (bitmaps[i])
            {
                maps[i] = new Bitmap(bitmaps[i]);
            }
        }
        var width = maps[0].Width;
        var height = maps[0].Height;
        var target = new Bitmap(width, height);
        using var composed = Graphics.FromImage(target);
        Rectangle rect = new Rectangle(0, 0, width, height);
        for (int i = 0; i < maps.Length; i++)
        {
            composed.DrawImage(maps[i], rect);
            maps[i].Dispose();
        }
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
    }
}