// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System;
using System.Text;

namespace NFTGenerator.Metadata;

[Serializable]
public class RarityMetadata
{
    [JsonProperty("id")]
    public int Id { get; init; }

    [JsonProperty("rarity")]
    public double Rarity { get; set; }

    [JsonProperty("hash")]
    public int[] Hash { get; init; }

    public override string? ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append($"ID: {Id}");
        stringBuilder.Append($" | rarity: {Rarity} | ");
        stringBuilder.Append('[');
        for (var i = 0; i < Hash.Length; i++)
        {
            var id = Hash[i];
            stringBuilder.Append($"{id}");
            if (i < Hash.Length - 1)
            {
                stringBuilder.Append(", ");
            }
        }
        stringBuilder.Append(']');
        return stringBuilder.ToString();
    }
}