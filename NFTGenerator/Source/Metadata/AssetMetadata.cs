// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> AssetMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator
{
    [System.Serializable]
    internal class AssetMetadata
    {
        public const string SCHEMA = "Schema/asset_metadata.json";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("attributes")]
        public List<AttributeMetadata> Attributes { get; set; }

        public override string ToString()
        {
            string attr = "";
            Attributes.ForEach(a => attr += a.ToString());
            return "Id: " + Id + ", Description: " + Description + ", Amount: " + Amount + "\nAttributes\n" + attr;
        }
    }
}