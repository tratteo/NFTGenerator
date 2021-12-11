// Copyright Matteo Beltrame

using HandierCli;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NFTGenerator;

internal class Generator
{
    private readonly Filesystem filesystem;
    private readonly Logger logger;
    private readonly NFTMetadata nftMetadataBlueprint;
    private readonly List<int[]> generatedHashes;

    public Generator(Filesystem filesystem, Logger logger)
    {
        this.logger = logger;
        generatedHashes = new List<int[]>();
        this.filesystem = filesystem;
        nftMetadataBlueprint = NFTMetadata.Blueprint();
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
        generatedHashes.Clear();
    }

    public void GenerateSingle(int index, IProgress<int> progress = null)
    {
        NFTMetadata meta = nftMetadataBlueprint.Clone();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = $"{Configurator.Options.ResultsPath}\\{index}.png";
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

            lock (pick)
            {
                pick.MintedAmount++;
            }
            toMerge.Add(pick);
        }

        //At this point i have all the assets to be merged
        if (toMerge.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return;
        }
        List<IMediaProvider> assets = CheckIncompatibles(toMerge);
        
        Bitmap[] bitmaps = (from asset in assets select asset.ProvideMedia()).ToArray();
        Media.ComposePNG(resPath, logger, bitmaps);

        float generationProbability = 1F;
        generationProbability *= toMerge[0].Metadata.Amount / (float)Configurator.Options.Generation.SerieCount;
        generationProbability *= toMerge[1].Metadata.Amount / (float)Configurator.Options.Generation.SerieCount;
        if (!Configurator.Options.Generation.AssetsOnly)
        {
            meta.AddAttributes(toMerge[0].Metadata.Attributes);
            meta.AddAttributes(toMerge[1].Metadata.Attributes);
        }

        for (var i = 2; i < toMerge.Count; i++)
        {
            if (!Configurator.Options.Generation.AssetsOnly)
            {
                meta.AddAttributes(toMerge[i].Metadata.Attributes);
            }
            generationProbability *= toMerge[i].Metadata.Amount / (float)Configurator.Options.Generation.SerieCount;
        }
        StringBuilder stringBuilder = new StringBuilder();
        foreach (int val in mintedHash)
        {
            stringBuilder.Append($"{val} ");
        }
        string report = stringBuilder.ToString();
        Serializer.WriteAll($"{Configurator.Options.ResultsPath}\\rarities\\", $"{index}.rarity", $"Probability: {generationProbability}\nHash: {stringBuilder}");

        if (!Configurator.Options.Generation.AssetsOnly)
        {
            Serializer.SerializeJson($"{Configurator.Options.ResultsPath}\\", $"{index}.json", meta);
        }
        generatedHashes.Add(mintedHash);
        progress?.Report(1);
    }

    private Asset GetLastLayerValidAsset(int[] hash)
    {
        if (!filesystem.Layers[^1].HasMintableAssets())
        {
            return null;
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

    private List<IMediaProvider> CheckIncompatibles(List<Asset> assets)
    {
        var fallbacks = filesystem.AssetFallbacks;
        var iters = fallbacks[0].Metadata.Incompatibles.Length;
        List<int> incompatibles = new List<int>();
        for (int i = 0; i<fallbacks.Count; i++)
        {
            var fallback = fallbacks[i].Metadata.Incompatibles;// int array
            var numberIncompatible = fallbacks[i].Metadata.IncompatiblesCount;// incompatibles sum
            int incompatibleCount = 0;// incompatibles current check
            incompatibles.Clear();
            for (int j = 0; j < iters; j++)
            {
                if(fallback[j] != -1)
                {
                    if(fallback[j] == assets[j].Id)
                    {
                        incompatibleCount++;
                        incompatibles.Add(j);
                    }
                }
            }
            if(numberIncompatible == incompatibleCount)
            {
                List<IMediaProvider> res = new List<IMediaProvider>();  
                for(int k =0; k < iters; k++)
                {
                    if(!incompatibles.Contains(k))
                    {
                        res.Add(assets[k]);
                    }    
                }
                res.Insert(incompatibles[0], fallbacks[i]);
                return res;
            }
        }
        return assets.ConvertAll(a => a as IMediaProvider);
    }
    private bool IsHashValid(int[] current)
    {
        return generatedHashes.FindAll((h) =>
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