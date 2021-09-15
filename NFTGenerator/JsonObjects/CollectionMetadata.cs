using Newtonsoft.Json;

namespace NFTGenerator.JsonObjects
{
    [System.Serializable]
    internal class CollectionMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("family")]
        public string Family { get; set; }

        public bool Valid()
        {
            bool valid = true;
            if (Name.Equals(string.Empty))
            {
                Logger.LogError("[Collection] Name is empty");
                valid = false;
            }
            if (Family.Equals(string.Empty))
            {
                Logger.LogError("[Colleciton] Family is empty");
                valid = false;
            }
            return valid;
        }
    }
}