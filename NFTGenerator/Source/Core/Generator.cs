// Copyright Matteo Beltrame

using GibNet.Logging;
using GibNet.Serialization;
using System;
using System.Collections.Generic;

namespace NFTGenerator;

internal class Generator
{
    private readonly Filesystem filesystem;
    private readonly Logger logger;

    public List<int[]> GeneratedHashes { get; private set; }

    public Generator(Filesystem filesystem, Logger logger)
    {
        this.logger = logger;
        GeneratedHashes = new List<int[]>();
        this.filesystem = filesystem;
    }

    public void ResetGenerationParameters()
    {
        foreach (Layer layer in filesystem.Layers)
        {
            foreach (Asset asset in layer.Assets)
            {
                asset.MintedAmount = 0;
            }
        }
        GeneratedHashes.Clear();
    }

    public void GenerateSingle(int index)
    {
        logger.LogInfo("Generating NFT #" + index);

        NFTMetadata meta = NFTMetadata.Blueprint();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = Configurator.Options.ResultsPath + "\\" + index + filesystem.MediaExtension;
        List<Asset> toMerge = new List<Asset>();
        for (var i = 0; i < filesystem.Layers.Count; i++)
        {
            Asset pick = filesystem.Layers[i].GetRandom();
            mintedHash[i] = pick.Id;
            //We are in last layer and there is a duplicate
            if (!Configurator.Options.Generation.AllowDuplicates && i == filesystem.Layers.Count - 1 && !IsHashValid(mintedHash))
            {
                pick = GetLastLayerValidAsset(mintedHash);
                mintedHash[i] = pick.Id;
            }
            pick.MintedAmount++;
            toMerge.Add(pick);
        }
        //At this point i have all the assets to be merged
        if (toMerge.Count < 2)
        {
            throw new Exception("Unable to merge less than 2 assets!");
        }
        // Create the first gif
        if (!Configurator.Options.Generation.AssetsOnly)
        {
            meta.AddAttributes(toMerge[0].Metadata.Attributes);
            meta.AddAttributes(toMerge[1].Metadata.Attributes);
        }
        Media.ComposeMedia(toMerge[0].AssetAbsolutePath, toMerge[1].AssetAbsolutePath, resPath, logger);
        //Logger.LogInfo(toMerge.Count.ToString());
        for (var i = 2; i < toMerge.Count; i++)
        {
            if (!Configurator.Options.Generation.AssetsOnly)
            {
                meta.AddAttributes(toMerge[i].Metadata.Attributes);
            }
            Media.ComposeMedia(resPath, toMerge[i].AssetAbsolutePath, resPath, logger);
        }
        if (!Configurator.Options.Generation.AssetsOnly)
        {
            Serializer.SerializeJson(meta, string.Empty, Configurator.Options.ResultsPath + "\\" + index + ".json");
        }
        GeneratedHashes.Add(mintedHash);
    }

    private Asset GetLastLayerValidAsset(int[] hash)
    {
        if (!filesystem.Layers[^1].HasMintableAssets())
        {
            throw new Exception("Ok this should never happen, the last layer has no mintable assets");
        }
        foreach (Asset asset in filesystem.Layers[^1].Assets)
        {
            if (asset.MintedAmount < asset.Metadata.Amount && asset.Id != hash[^1])
            {
                //Valid asset
                return asset;
            }
        }
        logger.LogError("Wait, WTF, unable to find a last layer available asset, this means that assets in the last layer are less than expected!");
        return null;
    }

    private bool IsHashValid(int[] current)
    {
        return GeneratedHashes.FindAll((h) =>
        {
            for (var i = 0; i < current.Length; i++)
            {
                if (current[i] != h[i])
                {
                    return false;
                }
            }
            return true;
        }).Count <= 0;
    }
}