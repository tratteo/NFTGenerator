using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Asset
    {
        public int Id { get; init; }

        public Metadata Data { get; init; }

        public string AssetAbsolutePath { get; init; }

        public int MintedAmount { get; set; } = 0;

        public Asset(string resourceAbsolutePath, int id)
        {
            Id = id;
            string[] metadata = Directory.GetFiles(resourceAbsolutePath, "*.json");
            if (metadata.Length > 1)
            {
                Logger.LogWarning("found multiple metadata in path: " + resourceAbsolutePath);
            }
            else if (metadata.Length <= 0)
            {
                throw new Exception("Metadata missing in folder: " + resourceAbsolutePath);
            }
            string[] assets = Directory.GetFiles(resourceAbsolutePath, "*.gif");
            if (assets.Length > 1)
            {
                Logger.LogWarning("found multiple assets in path: " + resourceAbsolutePath);
            }
            else if (assets.Length <= 0)
            {
                throw new Exception("Asset missing in folder: " + resourceAbsolutePath);
            }
            string assetPath = assets[0];
            string metadataPath = metadata[0];
            if (!File.Exists(metadataPath))
            {
                throw new Exception("Unable to find the metadata inside path: " + resourceAbsolutePath);
            }
            if (!File.Exists(assetPath))
            {
                throw new Exception("Unable to find the assed inside path: " + resourceAbsolutePath);
            }

            Data = JsonHandler.Deserialize<Metadata>(metadataPath);
            AssetAbsolutePath = assetPath;
        }

        public override string ToString()
        {
            return "Id: " + Id + ", Asset: " + AssetAbsolutePath + ", Metadata: " + Data.ToString();
        }

        internal class Metadata
        {
            public string Id { get; init; }

            public string Description { get; init; } = string.Empty;

            public int Amount { get; init; } = 0;

            public List<Attribute> Attributes { get; init; } = new List<Attribute>();

            public override string ToString()
            {
                string attr = "";
                Attributes.ForEach(a => attr += a.ToString());
                return "Id: " + Id + ", Description: " + Description + ", Amount: " + Amount + "\nAttributes\n" + attr;
            }
        }
    }
}