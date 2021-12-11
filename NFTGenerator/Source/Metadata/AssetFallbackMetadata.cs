// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator.Source.Metadata;

internal class AssetFallbackMetadata
{
    public const string BLUEPRINT = "assetfallback_metadata.json";

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }
   
    [JsonProperty ("incompatibles")]
    public int[] Incompatibles { get; set; }


    public int IncompatiblesCount => Incompatibles.Select(a => a != -1).Count();
    public static AssetFallbackMetadata Blueprint() => Serializer.DeserializeJson<AssetFallbackMetadata>(Paths.BLUEPRINT_PATH, BLUEPRINT, out var metadata) ? metadata : null;
}
