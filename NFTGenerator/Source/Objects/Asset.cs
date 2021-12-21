// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator.Source;
using System.Drawing;
using System.IO;

namespace NFTGenerator;

internal class Asset : IMediaProvider, IIdOwner
{
    public int Id { get; private set; }

    public AssetMetadata Metadata { get; private set; }

    public string AssetAbsolutePath { get; private set; }

    public int MintedAmount { get; set; } = 0;

    private Asset()
    {
    }

    public static bool TryParse(out Asset asset, string resourceAbsolutePath, int id, Logger logger)
    {
        asset = new Asset
        {
            Id = id
        };
        var metadata = Directory.GetFiles(resourceAbsolutePath, $"{id}.json");
        if (metadata.Length > 1)
        {
            logger.LogWarning($"Found multiple metadata in path: {resourceAbsolutePath}");
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
        var assets = Directory.GetFiles(resourceAbsolutePath, $"{id}.png");
        if (assets.Length > 1)
        {
            logger.LogWarning($"Found multiple assets in path: {resourceAbsolutePath}");
        }
        else if (assets.Length <= 0)
        {
            asset = null;
            return false;
        }
        var assetPath = assets[0];
        var metadataPath = metadata[0];
        if (!File.Exists(metadataPath))
        {
            logger.LogError($"Unable to find the metadata inside path: {resourceAbsolutePath}");
            asset = null;
            return false;
        }
        if (!File.Exists(assetPath))
        {
            logger.LogError($"Unable to find the asset inside path: {resourceAbsolutePath}");
            asset = null;
            return false;
        }

        if (Serializer.DeserializeJson<AssetMetadata>(string.Empty, metadataPath, out var assetMetadata))
        {
            asset.Metadata = assetMetadata;
        }
        asset.AssetAbsolutePath = assetPath;
        return true;
    }

    public string ProvideMediaPath() => AssetAbsolutePath;
}