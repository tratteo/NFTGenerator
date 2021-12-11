// Copyright Matteo Beltrame

using HandierCli;
using System.Drawing;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposePNG(string res, Logger logger, params Bitmap[] bitmaps)
    {

        var width = bitmaps[0].Width;
        var height = bitmaps[0].Height;
        var target = new Bitmap(width, height);
        using var composed = Graphics.FromImage(target);
        Rectangle rect = new Rectangle(0, 0, width, height);
        for (int i = 0; i < bitmaps.Length; i++)
        {
            composed.DrawImage(bitmaps[i], rect);
            bitmaps[i].Dispose();
        }
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
    }
}