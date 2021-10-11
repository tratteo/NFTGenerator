// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> NFTMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator
{
    [System.Serializable]
    internal class NFTMetadata
    {
        public const string SCHEMA = "Schema/nft_metadata.json";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("seller_fee_basis_points")]
        public int SellerFeeBasisPoints { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("animation_url")]
        public string AnimationUrl { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }

        [JsonProperty("attributes")]
        public List<AttributeMetadata> Attributes { get; set; }

        [JsonProperty("collection")]
        public CollectionMetadata Collection { get; set; }

        [JsonProperty("properties")]
        public PropertiesMetadata Properties { get; set; }

        public static NFTMetadata Schema() => Json.Deserialize<NFTMetadata>(SCHEMA);

        public bool Valid(Logger logger)
        {
            bool valid = true;
            if (Name.Equals(string.Empty))
            {
                logger.LogError("Field Name is empty");
                valid = false;
            }
            if (Description.Equals(string.Empty))
            {
                logger.LogWarning("Field Description is empty");
            }
            if (Symbol.Equals(string.Empty))
            {
                logger.LogError("Field Symbol is empty");
                valid = false;
            }
            if (SellerFeeBasisPoints.Equals(0))
            {
                logger.LogWarning("SellerFeeBasisPoints is set to 0, you don't want to earn anything from sales?");
            }
            if (!Image.Equals("image.png"))
            {
                logger.LogError("Since Metaplex developers are weirdos, image field MUST be populated with <image.png>");
                valid = false;
            }
            if (ExternalUrl.Equals(string.Empty))
            {
                logger.LogWarning("Field ExternalUrl is empty");
            }
            if (!Collection.Valid(logger))
            {
                valid = false;
            }
            if (!Properties.Valid(logger))
            {
                valid = false;
            }
            return valid;
        }

        public void AddAttributes(IEnumerable<AttributeMetadata> attributes)
        {
            foreach (AttributeMetadata data in attributes)
            {
                if (!Attributes.Contains(data))
                {
                    Attributes.Add(data);
                }
            }
        }
    }
}