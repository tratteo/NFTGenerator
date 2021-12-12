// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System.Linq;

namespace NFTGenerator.Source.Metadata;

internal class AssetFallbackMetadata
{
    public const string BLUEPRINT = "assetfallback_metadata.json";

    private int incompatiblesCount = -1;

    [JsonProperty("incompatibles")]
    public int[] Incompatibles { get; set; }

    [JsonIgnore]
    public int IncompatiblesCount
    {
        get
        {
            if (incompatiblesCount == -1)
            {
                incompatiblesCount = Incompatibles.Select(a => a != -1).Count();
            }
            return incompatiblesCount;
        }
    }

    public static AssetFallbackMetadata Blueprint() => Serializer.DeserializeJson<AssetFallbackMetadata>(Paths.BLUEPRINT_PATH, BLUEPRINT, out var metadata) ? metadata : null;
}