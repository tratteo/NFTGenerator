using Newtonsoft.Json;
using System;

namespace NFTGenerator.JsonObjects
{
    [System.Serializable]
    internal class AttributeMetadata : IEquatable<AttributeMetadata>
    {
        [JsonProperty("trait")]
        public string Trait { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        public static AttributeMetadata Define(string name, string value)
        {
            return new AttributeMetadata()
            {
                Trait = name,
                Value = value
            };
        }

        public bool Equals(AttributeMetadata other)
        {
            return Trait.Equals(other.Trait);
        }

        public override string ToString()
        {
            return "{" + Trait + ": " + Value + "}";
        }
    }
}