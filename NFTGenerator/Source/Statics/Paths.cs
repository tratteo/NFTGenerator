// Copyright Matteo Beltrame

using System;

namespace NFTGenerator;

internal static class Paths
{
    public static readonly string ROOT = $"{AppDomain.CurrentDomain.BaseDirectory}";
    public static readonly string FILESYSTEM = $"{ROOT}filesystem\\";

    public static readonly string RESULTS = $"{ROOT}results\\";

    public static readonly string TEMPLATES = $"{ROOT}templates\\";
    public static readonly string CONFIG = $"{ROOT}config\\";
    public static readonly string FALLBACKS = $"{FILESYSTEM}layers_fallback\\";
    public static readonly string LAYERS = $"{FILESYSTEM}layers\\";

    public static (string, string) Split(string path)
    {
        var index = path.LastIndexOf("\\");
        if (index == -1)
        {
            index = path.LastIndexOf("/");
        }
        var folder = index < 0 ? string.Empty : path[..(index + 1)];
        var name = path.Substring(index + 1, path.Length - index - 1);
        return (folder, name);
    }
}