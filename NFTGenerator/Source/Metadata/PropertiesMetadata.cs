// Copyright Matteo Beltrame

using GibNet.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator;

[System.Serializable]
internal class PropertiesMetadata
{
    [JsonProperty("files")]
    public List<FileMetadata> Files { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("creators")]
    public List<CreatorMetadata> Creators { get; set; }

    public bool Valid()
    {
        var valid = true;
        if (Category.Equals(string.Empty))
        {
            Logger.ConsoleInstance.LogError("[Properties] Category is empty");
            valid = false;
        }
        if (Creators == null || Creators.Count <= 0)
        {
            Logger.ConsoleInstance.LogError("[Properties]: there are no creators");
            valid = false;
        }
        else
        {
            foreach (CreatorMetadata creator in Creators)
            {
                if (!creator.Valid())
                {
                    valid = false;
                }
            }
        }
        return valid;
    }
}