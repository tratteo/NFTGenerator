// Copyright Matteo Beltrame

namespace NFTGenerator.Settings;

public class GenerationSettings
{
    public const string Position = "Generation";

    public bool GenerateRaritiesData { get; set; }

    public bool AllowDuplicates { get; set; }

    public int SerieCount { get; set; }

    public int WorkersCount { get; set; }

    public string Filter { get; set; } = null!;
}