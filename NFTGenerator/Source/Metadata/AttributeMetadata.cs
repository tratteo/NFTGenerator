// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> AttributeMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;
using System;

namespace NFTGenerator
{
    [Serializable]
    internal class AttributeMetadata : IEquatable<AttributeMetadata>
    {
        [JsonProperty("trait")]
        public string Trait { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

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