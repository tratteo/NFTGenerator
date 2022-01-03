// Copyright Matteo Beltrame

using BetterHaveIt;
using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Models;
using NFTGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFTGenerator.Services;

internal class Generator : IGenerator
{
    private readonly IFilesystem filesystem;
    private readonly IServiceProvider services;
    private readonly ILogger<Generator> logger;
    private readonly NFTMetadata nftMetadataBlueprint;
    private readonly List<int[]> generatedHashes;
    private readonly IConfiguration configuration;

    public Generator(IServiceProvider services, ILogger<Generator> logger)
    {
        this.services = services;
        this.logger = logger;
        filesystem = services.GetService<IFilesystem>();
        configuration = services.GetService<IConfiguration>();
        generatedHashes = new List<int[]>();
        nftMetadataBlueprint = NFTMetadata.Template();
    }

    public void GenerateSingle(int index, IProgress<int> progress = null)
    {
        int serieCount = configuration.GetValue<int>("Generation:SerieCount");
        NFTMetadata meta = nftMetadataBlueprint.Clone();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = $"{Paths.RESULTS}\\{index}.png";
        List<Asset> toMerge = new List<Asset>();
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

                toMerge.Add(pick);
            }
            if (configuration.GetValue<bool>("Generation:AllowDuplicates")) break;
        }
        while (!IsHashValid(mintedHash));

        toMerge.ForEach(a => a.UsedAmount++);

        if (toMerge.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return;
        }

        List<IMediaProvider> assets = CheckIncompatibles(toMerge, index);
        progress?.Report(1);
        Media.ComposePNG(resPath, logger, assets.ToArray());

        float generationProbability = 1F;
        generationProbability *= toMerge[0].Metadata.Amount / (float)serieCount;
        generationProbability *= toMerge[1].Metadata.Amount / (float)serieCount;
        meta.AddAttributes(toMerge[0].Metadata.Attributes);
        meta.AddAttributes(toMerge[1].Metadata.Attributes);

        for (var i = 2; i < toMerge.Count; i++)
        {
            meta.AddAttributes(toMerge[i].Metadata.Attributes);
            generationProbability *= toMerge[i].Metadata.Amount / (float)serieCount;
        }
        StringBuilder stringBuilder = new StringBuilder();
        foreach (int val in mintedHash)
        {
            stringBuilder.Append($"{val} ");
        }
        string report = stringBuilder.ToString();
        Serializer.WriteAll($"{Paths.RESULTS}\\rarities\\", $"{index}.rarity", $"Probability: {generationProbability}\nHash: {stringBuilder}");

        Serializer.SerializeJson($"{Paths.RESULTS}\\", $"{index}.json", meta);
        lock (generatedHashes)
        {
            generatedHashes.Add(mintedHash);
        }
    }

    private List<IMediaProvider> CheckIncompatibles(List<Asset> assets, int genIndex)
    {
        foreach (var fallbackDef in filesystem.FallbackMetadata.GetFallbacksByPriority())
        {
            if (fallbackDef.CheckIncompatibleHit(assets, out int firstHit, out List<int> toRemove))
            {
                //logger.LogWarning($"Found incompatible, hash:");
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
                for (int i = 0; i < assets.Count; i++)
                {
                    if (!toRemove.Contains(i))
                    {
                        res.Add(assets[i]);
                    }
                }
                res.Insert(firstHit, fallbackDef);
                return res;
            }
        }
        return new List<IMediaProvider>(assets);
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