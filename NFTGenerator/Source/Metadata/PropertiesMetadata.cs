// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> PropertiesMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator
{
    [System.Serializable]
    internal class PropertiesMetadata
    {
        [JsonProperty("files")]
        public List<FileMetadata> Files { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("creators")]
        public List<CreatorMetadata> Creators { get; set; }

        public bool Valid(Logger logger)
        {
            bool valid = true;
            if (Category.Equals(string.Empty))
            {
                logger.LogError("[Properties] Category is empty");
                valid = false;
            }
            if (Creators == null || Creators.Count <= 0)
            {
                logger.LogError("[Properties]: there are no creators");
                valid = false;
            }
            else
            {
                foreach (CreatorMetadata creator in Creators)
                {
                    if (!creator.Valid(logger))
                    {
                        valid = false;
                    }
                }
            }
            return valid;
        }
    }
}