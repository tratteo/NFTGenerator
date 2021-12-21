// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System;

namespace NFTGenerator;

[Serializable]
internal class Options
{
    [JsonProperty("generation")]
    public GenerationOptions Generation { get; init; }

    public Options()
    {
        Generation = new GenerationOptions()
        {
            AllowDuplicates = false,
            SerieCount = 0,
            AssetsOnly = false
        };
    }

    [Serializable]
    internal class GenerationOptions
    {
        [JsonProperty("allow_duplicates")]
        public bool AllowDuplicates { get; init; }

        [JsonProperty("serie_count")]
        public int SerieCount { get; set; }

        [JsonProperty("assets_only")]
        public bool AssetsOnly { get; init; }
    }
}