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
        Glitch,
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
                return ApplyRgbShiftFilter(bitmap);

            case Filter.SliceShift:
                return ApplyShiftFilter(bitmap);

            case Filter.VideoD_RgbShift:
                return ApplyGlitchFilter(ApplyVideoDegradationFilter(bitmap));

            case Filter.Glitch:
                return ApplyGlitchFilter(bitmap);
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
    private static Bitmap ApplyShiftFilter(Bitmap image, int lower = 30, int upper = 60, int shiftAmountLower = 20, int shiftAmountUpper = 100, int nShifts = 4, int rowPosPercentage = 15)
    {
        Bitmap test = new Bitmap(image);

        Random r = new Random(image.GetHashCode() * (int)DateTime.Now.Ticks);

        for (int i = 0; i < nShifts; i++)
        {
            var shift = GetRandomShift(r, shiftAmountLower, shiftAmountUpper);
            ShiftSingleLine(image, test, r, shift, lower, upper, rowPosPercentage);
        }
        return test;
    }

    private static Bitmap ApplyRgbShiftFilter(Bitmap image)
    {
        Random r = new Random(image.GetHashCode() * (int)DateTime.Now.Ticks);
        int shiftAmount = r.Next(4, 10);
        Bitmap res = new Bitmap(image.Width, image.Height);
        //base blue layer
        BluShift(image, res, shiftAmount);
        //green shifted layer
        GreenShift(image, res, shiftAmount);
        //red shifted layer
        RedShift(image, res, shiftAmount);
        //horizontal shift lines
        return res;
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
        Random r = new Random(image.GetHashCode() * (int)DateTime.Now.Ticks);
        var res = ApplyRgbShiftFilter(image);
        //horizontal shift lines
        int lines = r.Next(2, 8);
        return ApplyShiftFilter(res, 30, 150, 10, 25, lines, 15);
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
                var x = j + shiftAmount;
                var y = i + shiftAmount;
                if (x < image.Width && y < image.Height)
                {
                    pixel1 = image.GetPixel(x, y);
                    res.SetPixel(x, y, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (x >= image.Width && y < image.Height)
                {
                    pixel1 = image.GetPixel(j, y);
                    res.SetPixel(x % image.Width, y, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (x < image.Width && y >= image.Height)
                {
                    pixel1 = image.GetPixel(x, i);
                    res.SetPixel(x, y % image.Height, Color.FromArgb(pixel.A, 0, pixel1.G, pixel.B));
                }
                else if (x >= image.Width && y >= image.Height)
                {
                    res.SetPixel(x % image.Width, y % image.Height, Color.FromArgb(pixel.A, 0, pixel.G, pixel.B));
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
                var x = j + shiftAmount;
                var y = i + shiftAmount;
                if (y < image.Height && x < image.Width)
                {
                    pixel1 = image.GetPixel(x, y); //red
                    res.SetPixel(j, i, Color.FromArgb(pixel.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (y < image.Height && x >= image.Width)
                {
                    pixel1 = image.GetPixel(x % image.Width, y);
                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (y >= image.Height && x < image.Width)
                {
                    pixel1 = image.GetPixel(x, y % image.Height);

                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
                else if (y >= image.Height && x >= image.Width)
                {
                    pixel1 = image.GetPixel(x % image.Width, y % image.Height);
                    res.SetPixel(j, i, Color.FromArgb(pixel1.A, pixel1.R, pixel.G, pixel.B));
                }
            }
        }
        return res;
    }

    #endregion Color Shift

    #region slice Shift

    /// <summary>
    ///   Method <c> GetRandomShift </c> Decides wheter to shift left or right than picks the amount randomly
    /// </summary>
    /// <param name="lowerBound"> lower bound for the row's shift amount </param>
    /// <param name="upperBound"> upper bound for the row's shift amount </param>
    private static int GetRandomShift(Random r, int lowerBound, int upperBound) => r.Next(0, 2) == 0 ? r.Next(lowerBound, upperBound) : r.Next(1000 - upperBound, 1000 - lowerBound);

    private static Bitmap ShiftHorizontal(Bitmap image, Bitmap res, Random r, int range, int shift, int position)
    {
        for (int i = 0; i < range; i++)
        {
            var y = i + position;
            if (y >= image.Height)
            {
                break;
            }
            for (int j = 0; j < image.Width; j++)
            {
                Color c = image.GetPixel(j, y);
                var x = j + shift;
                if (x < image.Width)
                {
                    res.SetPixel(x, y, c);
                }
                else
                {
                    res.SetPixel(x % image.Width, y, c);
                }
            }
        }
        return res;
    }

    private static Bitmap ShiftVertical(Bitmap image, Bitmap res, Random r, int range, int shift, int position)
    {
        for (int i = 0; i < image.Height; i++)
        {
            for (int j = 0; j < range; j++)
            {
                var x = j + position;
                var y = i + shift;
                if (x >= image.Width)
                {
                    break;
                }
                Color c = image.GetPixel(j + position, i);
                if (y < image.Height)
                {
                    res.SetPixel(x, y, c);
                }
                else
                {
                    res.SetPixel(x, y % image.Height, c);
                }
            }
        }
        return res;
    }

    private static Bitmap ShiftSingleLine(Bitmap image, Bitmap res, Random r, int shift, int lower, int upper, int position)
    {
        int range = r.Next(lower, upper);
        // position = height range in which shifts are performed in percentual

        //horizontal
        if (r.Next(0, 2) == 0)
        {
            var pos = (image.Height * position / 100);
            position = r.Next(pos, image.Height - pos);
            ShiftHorizontal(image, res, r, range, shift, position);
        }
        else
        {
            var pos = (image.Width * position / 100);
            position = r.Next(pos, image.Width - pos);
            ShiftVertical(image, res, r, range, shift, position);
        }
        //vertical shift

        return res;
    }

    #endregion slice Shift
}