// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator.Source.Metadata;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator.Source.Objects;

internal class AssetFallback : IMediaProvider
{
    public Bitmap Img { get; private set; }
    public AssetFallbackMetadata Metadata { get; private set; }
    public string AssetFallbackAbsolutePath { get; private set; }

    public Bitmap ProvideMedia() => Img;
    public static bool TryParse(out AssetFallback assetFallback, string resourceAbsolutePath, int id, Logger logger)
    {
        assetFallback = new AssetFallback();
        
        var metadata = Directory.GetFiles(resourceAbsolutePath, "*.json");
        if (metadata.Length > 1)
        {
            logger.LogWarning($"Found multiple metadata in path: {resourceAbsolutePath}");
        }
        else if (metadata.Length <= 0)
        {
            if (!Configurator.Options.Generation.AssetsOnly)
            {
                //logger.LogError("Metadata missing in folder: " + resourceAbsolutePath);
                assetFallback = null;
                return false;
            }
        }
        var assets = Directory.GetFiles(resourceAbsolutePath, "*.png");
        if (assets.Length > 1)
        {
            logger.LogWarning($"Found multiple assets in path: {resourceAbsolutePath}");
        }
        else if (assets.Length <= 0)
        {
            assetFallback = null;
            return false;
        }
        var assetPath = assets[0];
        var metadataPath = metadata[0];
        if (!File.Exists(metadataPath))
        {
            logger.LogError($"Unable to find the metadata inside path: {resourceAbsolutePath}");
            assetFallback = null;
            return false;
        }
        if (!File.Exists(assetPath))
        {
            logger.LogError($"Unable to find the asset inside path: {resourceAbsolutePath}");
            assetFallback = null;
            return false;
        }

        if (Serializer.DeserializeJson<AssetFallbackMetadata>(string.Empty, metadataPath, out var assetMetadata))
        {
            assetFallback.Metadata = assetMetadata;
        }
        assetFallback.AssetFallbackAbsolutePath = assetPath;
        assetFallback.Img = new Bitmap(assetPath);
        return true;
    }

}
