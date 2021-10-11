// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> CreatorMetadata.cs
//
// All Rights Reserved

using Newtonsoft.Json;

namespace NFTGenerator
{
    [System.Serializable]
    internal class CreatorMetadata
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("share")]
        public int Share { get; set; }

        public bool Valid(Logger logger)
        {
            if (Address.Equals(string.Empty))
            {
                logger.LogError("Creator Address is empty");
                return false;
            }
            if (Share <= 0)
            {
                logger.LogError("Creator Share is set to " + Share);
                return false;
            }
            return true;
        }
    }
}