// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator;

[System.Serializable]
internal class AssetMetadata
{
    public const string BLUEPRINT = "asset_metadata.json";

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }

    public static AssetMetadata Blueprint() => Serializer.DeserializeJson<AssetMetadata>(Paths.BLUEPRINT_PATH, BLUEPRINT, out var metadata) ? metadata : null;

    public override string ToString()
    {
        var attr = "";
        Attributes.ForEach(a => attr += a.ToString());
        return "Id: " + Id + ", Description: " + Description + ", Amount: " + Amount + "\nAttributes\n" + attr;
    }
}