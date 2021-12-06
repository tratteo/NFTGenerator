// Copyright Matteo Beltrame

using GibNet.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposePNG(string res, Logger logger, params string[] pngs)
    {
        Bitmap[] bitmaps = new Bitmap[pngs.Length];
        for (int i = 0; i < pngs.Length; i++)
        {
            bitmaps[i] = new Bitmap(pngs[i]);
        }

        var target = new Bitmap(bitmaps[0].Width, bitmaps[0].Height, PixelFormat.Format32bppArgb);
        var composed = Graphics.FromImage(target);
        composed.CompositingMode = CompositingMode.SourceOver;

        for (int i = 0; i < pngs.Length; i++)
        {
            composed.DrawImage(bitmaps[i], 0, 0);
            bitmaps[i].Dispose();
        }
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
    }
}