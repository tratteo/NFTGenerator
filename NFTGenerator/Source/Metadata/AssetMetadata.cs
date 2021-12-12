// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator;

[System.Serializable]
internal class AssetMetadata
{
    public const string BLUEPRINT = "asset_metadata.json";

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }

    public static AssetMetadata Blueprint() => Serializer.DeserializeJson<AssetMetadata>(Paths.BLUEPRINT_PATH, BLUEPRINT, out var metadata) ? metadata : null;

    public override string ToString()
    {
        var attr = "";
        Attributes.ForEach(a => attr += a.ToString());
        return "Amount: " + Amount + "\nAttributes\n" + attr;
    }
}