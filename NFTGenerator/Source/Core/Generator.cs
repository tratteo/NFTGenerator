// Copyright Matteo Beltrame

using HandierCli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFTGenerator;

internal class Generator
{
    private readonly Filesystem filesystem;
    private readonly Logger logger;
    private readonly NFTMetadata nftMetadataBlueprint;
    private readonly List<List<int>> generatedHashes;

    public Generator(Filesystem filesystem, Logger logger)
    {
        this.logger = logger;
        generatedHashes = new List<List<int>>();
        this.filesystem = filesystem;
        nftMetadataBlueprint = NFTMetadata.Template();
    }

    public void ResetGenerationParameters()
    {
        foreach (Layer layer in filesystem.Layers)
        {
            foreach (Asset asset in layer.Assets)
            {
                asset.UsedAmount = 0;
            }
        }
        generatedHashes.Clear();
    }

    public void GenerateSingle(int index, IProgress<int> progress = null)
    {
        NFTMetadata meta = nftMetadataBlueprint.Clone();
        var mintedHash = new List<int>();
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
            mintedHash.Clear();
            for (var i = 0; i < filesystem.Layers.Count; i++)
            {
                Asset pick = filesystem.Layers[i].GetRandom();
                mintedHash.Add(pick.Id);

                toMerge.Add(pick);
            }
            if (Configurator.Options.Generation.AllowDuplicates) break;
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
        Serializer.WriteAll($"{Paths.RESULTS}\\rarities\\", $"{index}.rarity", $"Probability: {generationProbability}\nHash: {stringBuilder}");

        if (!Configurator.Options.Generation.AssetsOnly)
        {
            Serializer.SerializeJson($"{Paths.RESULTS}\\", $"{index}.json", meta);
        }
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