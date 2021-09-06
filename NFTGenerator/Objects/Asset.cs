using System;
using System.IO;

namespace NFTGenerator
{
    internal class Asset
    {
        public Metadata Metadata { get; init; }

        public string AssetAbsolutePath { get; init; }

        public int MintedAmount { get; set; } = 0;

        public Asset(string resourceAbsolutePath)
        {
            string[] metadata = Directory.GetFiles(resourceAbsolutePath, "*.json");
            if (metadata.Length > 1)
            {
                Logger.Log("found multiple metadata in path: " + resourceAbsolutePath, Logger.LogType.WARNING);
            }
            string[] assets = Directory.GetFiles(resourceAbsolutePath, "*.gif");
            if (assets.Length > 1)
            {
                Logger.Log("found multiple assets in path: " + resourceAbsolutePath, Logger.LogType.WARNING);
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

            Metadata = JsonHandler.Deserialize<Metadata>(metadataPath);
            AssetAbsolutePath = assetPath;
        }

        public override string ToString()
        {
            return "Asset: " + AssetAbsolutePath + ", Metadata: " + Metadata.ToString();
        }
    }
}