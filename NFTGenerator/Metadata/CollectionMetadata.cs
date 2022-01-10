// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NFTGenerator.Metadata;

[System.Serializable]
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