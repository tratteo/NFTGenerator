// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> CollectionMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;

namespace NFTGenerator
{
    [System.Serializable]
    internal class CollectionMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("family")]
        public string Family { get; set; }

        public bool Valid(Logger logger)
        {
            bool valid = true;
            if (Name.Equals(string.Empty))
            {
                logger.LogError("[Collection] Name is empty");
                valid = false;
            }
            if (Family.Equals(string.Empty))
            {
                logger.LogError("[Colleciton] Family is empty");
                valid = false;
            }
            return valid;
        }
    }
}