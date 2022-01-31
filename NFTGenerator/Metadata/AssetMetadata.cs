// Copyright Matteo Beltrame

using BetterHaveIt;
using Newtonsoft.Json;
using static NFTGenerator.Metadata.TokenMetadata;

namespace NFTGenerator.Metadata;

[System.Serializable]
internal class AssetMetadata
{
    public const string TEMPLATE_NAME = "asset_metadata.json";

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("attribute")]
    public AttributeMetadata Attribute { get; set; }

    public static AssetMetadata Template() => Serializer.DeserializeJson<AssetMetadata>(Paths.TEMPLATES, TEMPLATE_NAME, out var metadata) ? metadata : null;

    public override string ToString()
    {
        var attr = "";
        if (Attribute is not null)
        {
            attr += Attribute.ToString();
        }
        return "Amount: " + Amount + "\nAttributes\n" + attr;
    }
}