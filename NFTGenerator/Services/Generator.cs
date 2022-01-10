// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFTGenerator.Services;

internal class Generator : IGenerator
{
    private readonly IFilesystem filesystem;
    private readonly IServiceProvider services;
    private readonly ILogger logger;
    private readonly NFTMetadata nftMetadataBlueprint;
    private readonly List<int[]> generatedHashes;
    private readonly IConfiguration configuration;

    public Generator(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        this.services = services;
        this.logger = loggerFactory.CreateLogger("Generator");
        filesystem = services.GetService<IFilesystem>();
        configuration = services.GetService<IConfiguration>();
        generatedHashes = new List<int[]>();
        nftMetadataBlueprint = NFTMetadata.Template();
    }

    public string GenerateSingle(int index, IProgress<int> progress = null)
    {
        int serieCount = configuration.GetValue<int>("Generation:SerieCount");
        NFTMetadata meta = nftMetadataBlueprint.Clone();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = $"{Paths.RESULTS}\\{index}.png";
        List<LayerPick> toMerge = new List<LayerPick>();
        int cycle = 0;
        do
        {
            if (cycle > 0)
            {
                //logger.LogWarning("Found duplicate, brute forcing again");
            }
            cycle++;
            toMerge.Clear();

            for (var i = 0; i < filesystem.Layers.Count; i++)
            {
                Asset pick = filesystem.Layers[i].GetRandom();
                mintedHash[i] = pick.Id;

                toMerge.Add(new LayerPick() { Asset = pick, Layer = filesystem.Layers[i] });
            }
            if (configuration.GetValue<bool>("Generation:AllowDuplicates")) break;
        }
        while (!IsHashValid(mintedHash));

        toMerge.ForEach(a =>
        {
            a.Asset.UsedAmount++;
            if (a.Asset.UsedAmount >= a.Asset.Metadata.Amount)
                a.Asset.PickProbability = 0F;
        });

        if (toMerge.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return string.Empty;
        }

        List<IMediaProvider> assets = CheckIncompatibles(toMerge, index);
        progress?.Report(1);
        Media.ComposePNG(resPath, logger, assets.ToArray());

        float generationProbability = 1F;
        generationProbability *= toMerge[0].Asset.Metadata.Amount / (float)serieCount;
        generationProbability *= toMerge[1].Asset.Metadata.Amount / (float)serieCount;
        meta.AddAttributes(toMerge[0].Asset.Metadata.Attributes);
        meta.AddAttributes(toMerge[1].Asset.Metadata.Attributes);

        for (var i = 2; i < toMerge.Count; i++)
        {
            meta.AddAttributes(toMerge[i].Asset.Metadata.Attributes);
            generationProbability *= toMerge[i].Asset.Metadata.Amount / (float)serieCount;
        }
        StringBuilder stringBuilder = new StringBuilder();
        foreach (int val in mintedHash)
        {
            stringBuilder.Append($"{val} ");
        }
        string report = stringBuilder.ToString();
        Serializer.WriteAll($"{Paths.RESULTS}\\rarities\\", $"{index}.rarity", $"Probability: {generationProbability}\nHash: {stringBuilder}");

        meta.Name += $"#{index}";

        Serializer.SerializeJson($"{Paths.RESULTS}\\", $"{index}.json", meta);
        lock (generatedHashes)
        {
            generatedHashes.Add(mintedHash);
        }
        return $"{index}.json";
    }

    private List<IMediaProvider> CheckIncompatibles(List<LayerPick> assets, int genIndex)
    {
        foreach (var fallbackDef in filesystem.FallbackMetadata.GetFallbacksByPriority())
        {
            if (fallbackDef.HasInstructionsHit(assets, out int firstHit, out List<LayerPick> toRemove))
            {
                //logger.LogWarning($"Found incompatible, hash:");
                //foreach (AssetPick a in assets)
                //{
                //    Logger.ConsoleInstance.LogInfo("L: " + a.Layer.Name + ", a: " + a.Asset.Id + " | ", false);
                //}
                //foreach (var asset in assets)
                //{
                //    logger.LogInfo($"{asset.Id} ", false);
                //}
                //logger.LogWarning($"\nShould remove indexes:");
                //foreach (var index in toRemove)
                //{
                //    logger.LogInfo($"{index} ", false);
                //}

                List<IMediaProvider> res = new List<IMediaProvider>();
                foreach (var asset in assets)
                {
                    if (!toRemove.Contains(asset))
                    {
                        res.Add(asset.Asset);
                    }
                }
                //for (int i = 0; i < assets.Count; i++)
                //{
                //    if (toRemove.Find(a => a.Id.Equals(i)) == null)
                //    {
                //        res.Add(assets[i].Asset);
                //    }
                //}
                if (fallbackDef.FallbackAction == Incompatible.Action.ReplaceAll)
                {
                    res.Insert(firstHit, fallbackDef);
                }
                return res;
            }
        }
        return assets.ConvertAll(a => (IMediaProvider)a.Asset);
    }

    private bool IsHashValid(IEnumerable<int> current)
    {
        return generatedHashes.FindAll((h) =>
        {
            for (var i = 0; i < current.Count(); i++)
            {
                if (current.ElementAt(i) != h[i])
                {
                    return false;
                }
            }
            return true;
        }).Count <= 0;
    }
}