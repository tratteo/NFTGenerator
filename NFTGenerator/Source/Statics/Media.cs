// Copyright Matteo Beltrame

using HandierCli;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public static void ComposePNG(string res, Logger logger, params string[] pngs)
    {
        var target = new Bitmap(1000, 1000);
        using var composed = Graphics.FromImage(target);
        //composed.CompositingMode = CompositingMode.SourceOver;
        Rectangle rect = new Rectangle(0, 0, 1000, 1000);
        for (int i = 0; i < pngs.Length; i++)
        {
            composed.DrawImage(new Bitmap(pngs[i]), rect);
            //bitmaps[i].Dispose();
        }
        target.Save(res, ImageFormat.Png);
        //target.Dispose();
        //composed.Dispose();
    }
}