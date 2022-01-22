// Copyright Matteo Beltrame

using Newtonsoft.Json;

namespace NFTGenerator.Metadata;

internal class FileMetadata
{
    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

}