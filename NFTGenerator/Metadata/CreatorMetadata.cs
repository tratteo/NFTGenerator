// Copyright Matteo Beltrame

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NFTGenerator.Metadata;

[System.Serializable]
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