// Copyright Matteo Beltrame

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace NFTGenerator;

internal static class Media
{
    public enum Filter
    {
        VideoDegradation,
        RgbShift,
        SliceShift,
        VideoD_RgbShift
    }

    public static void ComposePNG(string res, ILogger logger, Filter? filter = null, params string[] mediaProvider)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        stopwatch.Restart();
        Bitmap[] maps = new Bitmap[mediaProvider.Length];
        for (int i = 0; i < maps.Length; i++)
        {
            string path = mediaProvider[i];
            //logger.LogInfo(mediaProvider[i].ProvideMediaPath());
            maps[i] = new Bitmap(path);
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
        target = ApplyFilter(target, filter);
        //var quantizer = new WuQuantizer();
        //using var quantized = quantizer.QuantizeImage(target);
        //quantized.Save(res, ImageFormat.Png);
        target.Save(res, ImageFormat.Png);
        target.Dispose();
        composed.Dispose();
        stopwatch.Stop();
        //logger.LogInfo($"Finishing {stopwatch.ElapsedMilliseconds} ms", ConsoleColor.Magenta);
    }

    public static Bitmap ApplyFilter(Bitmap bitmap, Filter? filter)
    {
        if (filter == null) return null;
        switch (filter)
        {
            case Filter.VideoDegradation:
                return ApplyVideoDegradationFilter(bitmap);

            case Filter.RgbShift:
                return ApplyGlitchFilter(bitmap);

            case Filter.SliceShift:
                return ApplyShiftFilter(bitmap);

            case Filter.VideoD_RgbShift:
                return ApplyGlitchFilter(ApplyVideoDegradationFilter(bitmap));
        }
        return null;
    }

    /// <summary>
    ///   Applies the shift filter to a given Bitmap
    /// </summary>
    /// <param name="image"> The Bitmap to apply the filter to </param>
    /// <param name="lower"> lower bound for the row's height </param>
    /// <param name="upper"> upper bound for the row's height </param>
    /// <param name="shiftAmountLower"> lower bound for the row's shift amount </param>
    /// <param name="shiftAmountUpper"> upper bound for the row's shift amount </param>
    /// <param name="nShifts"> Number of shifts to perform on the Image </param>
    /// <returns> </returns>
    private static Bitmap ApplyShiftFilter(Bitmap image, int lower = 30, int upper = 60, int shiftAmountLower = 20, int shiftAmountUpper = 100, int nShifts = 4)
    {
        Bitmap test = new Bitmap(image);

        Random r = new Random(image.GetHashCode() * image.GetPixel(lower, upper).R);

        for (int i = 0; i < nShifts; i++)
        {
            var shift = GetRandomShift(r, shiftAmountLower, shiftAmountUpper);
            ShiftSingleLine(image, test, r, shift, lower, upper);
        }
        return test;
    }

    private static Bitmap ApplyVideoDegradationFilter(Bitmap bitmap, int strenght = 15, int width = 3)
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
        return bitmap;
    }

    private static Bitmap ApplyGlitchFilter(Bitmap image)
    {
        Random r = new Random(image.GetHashCode());
        int shiftAmount = r.Next(5, 15);
        Bitmap res = new Bitmap(1000, 1000);
        //base blue layer
        BluShift(image, res, shiftAmount);
        //green shifted layer
        GreenShift(image, res, shiftAmount);
        //red shifted layer
        RedShift(image, res, shiftAmount);
        //horizontal shift lines
        int lines = r.Next(2, 8);

        return ApplyShiftFilter(res, 30, 150, 8, 25, lines);
    }

    #region Color Shift

    private static void BluShift(Bitmap image, Bitmap res, int shiftAmount)
    {
        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < image.Width; j++)
            {
                Color pixel = image.GetPixel(j, i);

                res.SetPixel(j, i, Color.FromArgb(pixel.A, 0, 0, pixel.B));
            }
        }
    }

    private static Bitmap GreenShift(Bitmap image, Bitmap res, int shiftAmount)
    {
        Color pixel1;
        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < image.Width; j++)
            {
                Color pixel = image.GetPixel(j, i);

                if (i + shiftAmount < image.Height && j + shiftAmount < image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount, i + shiftAmount);
                    res.SetPixel(j + shiftAmount, i + shiftAmount, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (j + shiftAmount >= image.Width && i + shiftAmount < image.Width)
                {
                    pixel1 = image.GetPixel(j, i + shiftAmount);
                    res.SetPixel(j + shiftAmount - image.Width, i + shiftAmount, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (j + shiftAmount < image.Width && i + shiftAmount >= image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount, i);
                    res.SetPixel(j + shiftAmount, i + shiftAmount - image.Width, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (j + shiftAmount >= image.Width && i + shiftAmount >= image.Width)
                {
                    res.SetPixel(j + shiftAmount - image.Width, i + shiftAmount - image.Width, Color.FromArgb(pixel.A, 0, pixel.G, pixel.B));
                }
            }
        }
        return res;
    }

    private static Bitmap RedShift(Bitmap image, Bitmap res, int shiftAmount)
    {
        Color pixel1;
        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < image.Width; j++)
            {
                Color pixel = res.GetPixel(j, i);//blue + green
                if (i + shiftAmount < image.Width && j + shiftAmount < image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount, i + shiftAmount); //red
                    res.SetPixel(j, i, Color.FromArgb(pixel.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (i + shiftAmount < image.Width && j + shiftAmount >= image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount - image.Width, i + shiftAmount);
                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (i + shiftAmount >= image.Width && j + shiftAmount < image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount, i + shiftAmount - image.Width);

                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (i + shiftAmount >= image.Width && j + shiftAmount >= image.Width)
                {
                    pixel1 = image.GetPixel(j + shiftAmount - image.Width, i + shiftAmount - image.Width);
                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
            }
        }
        return res;
    }

    #endregion Color Shift

    #region slice Shift

    /// <summary>
    ///   Method <c> GetRandomShift </c> Decides wheter to shift left or right than picks the amount
    /// </summary>
    /// <param name="lowerBound"> lower bound for the row's shift amount </param>
    /// <param name="upperBound"> upper bound for the row's shift amount </param>
    private static int GetRandomShift(Random r, int lowerBound, int upperBound) => r.Next(0, 2) == 0 ? r.Next(lowerBound, upperBound) : r.Next(1000 - upperBound, 1000 - lowerBound);

    private static Bitmap ShiftSingleLine(Bitmap image, Bitmap res, Random r, int shift, int lower, int upper)
    {
        int h = r.Next(lower, upper);
        int w = image.Width;
        int position = r.Next(150, 850);// height range in which shifts are performed
        for (int i = 0; i < h; i++)
        {
            if (i + position >= image.Height)
            {
                break;
            }
            for (int j = 0; j < w; j++)
            {
                Color c = image.GetPixel(j, i + position);
                if (j + shift < image.Width)
                {
                    res.SetPixel(j + shift, i + position, c);
                }
                else
                {
                    res.SetPixel(j + shift - image.Width, i + position, c);
                }
            }
        }
        return res;
    }

    #endregion slice Shift
}