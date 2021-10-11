// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> FileMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;

namespace NFTGenerator
{
    internal class FileMetadata
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("cdn")]
        public bool Cdn { get; set; }
    }
}