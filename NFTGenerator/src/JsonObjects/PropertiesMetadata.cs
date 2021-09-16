using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator.JsonObjects
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

        public bool Valid()
        {
            bool valid = true;
            if (Category.Equals(string.Empty))
            {
                Logger.LogError("[Properties] Category is empty");
                valid = false;
            }
            if (Creators == null || Creators.Count <= 0)
            {
                Logger.LogError("[Properties]: there are no creators");
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
}