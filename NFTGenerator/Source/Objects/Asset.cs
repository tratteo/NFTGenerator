﻿// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Asset.cs
//
// All Rights Reserved

using System.IO;
using System.Linq;

namespace NFTGenerator
{
    internal class Asset
    {
        public int Id { get; private set; }

        public AssetMetadata Metadata { get; private set; }

        public string AssetAbsolutePath { get; private set; }

        public int MintedAmount { get; set; } = 0;

        private Asset()
        {
        }

        public static bool TryCreate(out Asset asset, string resourceAbsolutePath, int id, Logger logger)
        {
            asset = new Asset();
            asset.Id = id;
            string[] metadata = Directory.GetFiles(resourceAbsolutePath, "*.json");
            if (metadata.Length > 1)
            {
                logger.LogWarning("found multiple metadata in path: " + resourceAbsolutePath);
            }
            else if (metadata.Length <= 0)
            {
                if (!Configurator.Options.Generation.AssetsOnly)
                {
                    //logger.LogError("Metadata missing in folder: " + resourceAbsolutePath);
                    asset = null;
                    return false;
                }
            }
            string[] assets = Directory.GetFiles(resourceAbsolutePath, "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
            if (assets.Length > 1)
            {
                logger.LogWarning("found multiple assets in path: " + resourceAbsolutePath);
            }
            else if (assets.Length <= 0)
            {
                //logger.LogError("Asset missing in folder: " + resourceAbsolutePath);
                asset = null;
                return false;
            }
            string assetPath = assets[0];
            string metadataPath = metadata[0];
            if (!File.Exists(metadataPath))
            {
                logger.LogError("Unable to find the metadata inside path: " + resourceAbsolutePath);
                asset = null;
                return false;
            }
            if (!File.Exists(assetPath))
            {
                logger.LogError("Unable to find the asset inside path: " + resourceAbsolutePath);
                asset = null;
                return false;
            }

            asset.Metadata = Json.Deserialize<AssetMetadata>(metadataPath);
            asset.AssetAbsolutePath = assetPath;
            return true;
        }

        public override string ToString()
        {
            return "Id: " + Id + ", Asset: " + AssetAbsolutePath + ", Metadata: " + Metadata.ToString();
        }
    }
}