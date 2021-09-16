using Newtonsoft.Json;
using System.Collections.Generic;

namespace NFTGenerator.JsonObjects
{
    [System.Serializable]
    internal class NFTMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("seller_fee_basis_points")]
        public int SellerFeeBasisPoints { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("animation_url")]
        public string AnimationUrl { get; set; }

        [JsonProperty("external_url")]
        public string ExternalUrl { get; set; }

        [JsonProperty("attributes")]
        public List<AttributeMetadata> Attributes { get; set; }

        [JsonProperty("collection")]
        public CollectionMetadata Collection { get; set; }

        [JsonProperty("properties")]
        public PropertiesMetadata Properties { get; set; }

        public static NFTMetadata Defaulted() => Json.Deserialize<NFTMetadata>("blueprint.json");

        public bool Valid()
        {
            bool valid = true;
            if (Name.Equals(string.Empty))
            {
                Logger.LogError("Field Name is empty");
                valid = false;
            }
            if (Description.Equals(string.Empty))
            {
                Logger.LogWarning("Field Description is empty");
            }
            if (Symbol.Equals(string.Empty))
            {
                Logger.LogError("Field Symbol is empty");
                valid = false;
            }
            if (SellerFeeBasisPoints.Equals(0))
            {
                Logger.LogWarning("SellerFeeBasisPoints is set to 0, you don't want to earn anything from sales?");
            }
            if (!Image.Equals("image.png"))
            {
                Logger.LogError("Since Metaplex developers are weirdos, image field MUST be populated with <image.png>");
                valid = false;
            }
            if (ExternalUrl.Equals(string.Empty))
            {
                Logger.LogWarning("Field ExternalUrl is empty");
            }
            if (!Collection.Valid())
            {
                valid = false;
            }
            if (!Properties.Valid())
            {
                valid = false;
            }
            return valid;
        }

        public void AddAttributes(IEnumerable<AttributeMetadata> attributes)
        {
            foreach (AttributeMetadata data in attributes)
            {
                if (!Attributes.Contains(data))
                {
                    Attributes.Add(data);
                }
            }
        }
    }
}