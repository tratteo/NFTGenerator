using NFTGenerator.JsonObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NFTGenerator
{
    internal class Asset
    {
        public int Id { get; init; }

        public AssetMetadata Metadata { get; init; }

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
            string[] assets = Directory.GetFiles(resourceAbsolutePath, "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
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

            Metadata = Json.Deserialize<AssetMetadata>(metadataPath);
            AssetAbsolutePath = assetPath;
        }

        public override string ToString()
        {
            return "Id: " + Id + ", Asset: " + AssetAbsolutePath + ", Metadata: " + Metadata.ToString();
        }
    }
}