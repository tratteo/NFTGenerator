// Copyright Matteo Beltrame

using GibNet.Logging;
using Newtonsoft.Json;

namespace NFTGenerator;

[System.Serializable]
internal class CollectionMetadata
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("family")]
    public string Family { get; set; }

    public bool Valid()
    {
        var valid = true;
        if (Name.Equals(string.Empty))
        {
            Logger.ConsoleInstance.LogError("[Collection] Name is empty");
            valid = false;
        }
        if (Family.Equals(string.Empty))
        {
            Logger.ConsoleInstance.LogError("[Colleciton] Family is empty");
            valid = false;
        }
        return valid;
    }
}