// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NFTGenerator.Metadata;

[Serializable]
internal class TokenMetadata
{
    public const string TEMPLATE_NAME = "nft_metadata.json";

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

    [JsonProperty("attributes")]
    public List<AttributeMetadata> Attributes { get; set; }

    [JsonProperty("properties")]
    public PropertiesMetadata Properties { get; set; }

    [JsonProperty("collection")]
    public CollectionMetadata Collection { get; set; }

    public static TokenMetadata Template() => Serializer.DeserializeJson<TokenMetadata>(Paths.TEMPLATES + TEMPLATE_NAME, out var metadata) ? metadata : null;

    public bool Valid(ILogger logger)
    {
        var valid = true;
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
        if (!Properties.Valid(logger))
        {
            valid = false;
        }
        if (!Collection.Valid(logger))
        {
            valid = false;
        }
        return valid;
    }

    public void AddAttributes(IEnumerable<AttributeMetadata> attributes)
    {
        foreach (var data in attributes)
        {
            if (!Attributes.Contains(data))
            {
                Attributes.Add(data);
            }
        }
    }

    public TokenMetadata Clone()
    {
        return new TokenMetadata
        {
            Name = Name,
            Symbol = Symbol,
            Description = Description,
            Collection = Collection,
            SellerFeeBasisPoints = SellerFeeBasisPoints,
            Image = Image,
            Attributes = new List<AttributeMetadata>(Attributes),
            Properties = Properties
        };
    }

    [Serializable]
    internal class PropertiesMetadata
    {
        [JsonProperty("files")]
        public List<FileMetadata> Files { get; set; }

        [JsonProperty("creators")]
        public List<CreatorMetadata> Creators { get; set; }

        public bool Valid(ILogger logger)
        {
            var valid = true;
            if (Creators == null || Creators.Count <= 0)
            {
                logger.LogError("[Properties]: there are no creators");
                valid = false;
            }
            else
            {
                foreach (var creator in Creators)
                {
                    if (!creator.Valid(logger))
                    {
                        valid = false;
                    }
                }
            }
            return valid;
        }

        [Serializable]
        internal class FileMetadata
        {
            [JsonProperty("uri")]
            public string Uri { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }

        [Serializable]
        internal class CreatorMetadata
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("share")]
            public int Share { get; set; }

            public bool Valid(ILogger logger)
            {
                if (Address.Equals(string.Empty))
                {
                    logger.LogError("Creator Address is empty");
                    return false;
                }
                if (Share is <= 0 or > 100)
                {
                    logger.LogError("Creator Share is set to {val}", Share);
                    return false;
                }
                return true;
            }
        }
    }

    [Serializable]
    internal class CollectionMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("family")]
        public string Family { get; set; }

        public bool Valid(ILogger logger)
        {
            var valid = true;
            if (Name.Equals(string.Empty))
            {
                logger.LogError("[Collection] Name is empty");
                valid = false;
            }
            if (Family.Equals(string.Empty))
            {
                logger.LogError("[Collection] Family is empty");
                valid = false;
            }
            return valid;
        }
    }

    [Serializable]
    internal class AttributeMetadata : IEquatable<AttributeMetadata>
    {
        [JsonProperty("trait_type")]
        public string Trait { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("rarity")]
        public float Rarity { get; set; }

        public AttributeMetadata()
        {
            Trait = string.Empty;
            Value = string.Empty;
        }

        public bool Equals(AttributeMetadata other) => Trait.Equals(other.Trait);

        public override string ToString() => "{" + Trait + ": " + Value + "}";
    }
}