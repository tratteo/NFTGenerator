// Copyright Matteo Beltrame

using BetterHaveIt;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator.Metadata;

[System.Serializable]
internal class AssetMetadata
{
    public const string TEMPLATE_NAME = "asset_metadata.json";

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }

    public static AssetMetadata Template() => Serializer.DeserializeJson<AssetMetadata>(Paths.TEMPLATES, TEMPLATE_NAME, out var metadata) ? metadata : null;

    public override string ToString()
    {
        var attr = "";
        Attributes.ForEach(a => attr += a.ToString());
        return "Amount: " + Amount + "\nAttributes\n" + attr;
    }
}