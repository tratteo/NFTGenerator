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
}