// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using NFTGenerator.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static NFTGenerator.Metadata.TokenMetadata;
using static NFTGenerator.Metadata.TokenMetadata.PropertiesMetadata;

namespace NFTGenerator.Services;

internal class Generator : IGenerator
{
    private readonly IFilesystem filesystem;
    private readonly IOptionsMonitor<GenerationSettings> generationSettings;
    private readonly ILogger logger;
    private readonly TokenMetadata nftMetadataBlueprint;
    private readonly Media.Filter? filter;
    private List<int[]> generatedHashes;
    private List<RarityMetadata> rarities;

    public Generator(IFilesystem filesystem, ILoggerFactory loggerFactory, IOptionsMonitor<GenerationSettings> generationSettings)
    {
        logger = loggerFactory.CreateLogger("Generator");
        this.filesystem = filesystem;
        this.generationSettings = generationSettings;
        nftMetadataBlueprint = Template();
        var filterParse = generationSettings.CurrentValue.Filter;
        filter = filterParse == null ? null : (Media.Filter)Enum.Parse(typeof(Media.Filter), filterParse, true);
        logger.LogInformation("{f}", filter);
    }

    public void Generate()
    {
        rarities = new List<RarityMetadata>();
        generatedHashes = new List<int[]>();
        var serieCount = generationSettings.CurrentValue.SerieCount;
        var generateRarities = generationSettings.CurrentValue.GenerateRaritiesData;
        var currentCount = 0;
        var reportWatch = Stopwatch.StartNew();
        long lastReport = 0;
        var progressBar = new ConsoleProgressBar(50, "|/-\\", 8);
        var generationProgressReporter = new Progress<int>((p) =>
        {
            Interlocked.Increment(ref currentCount);
            var currentElapsed = reportWatch.ElapsedMilliseconds;
            if (currentElapsed - lastReport > 250)
            {
                lastReport = currentElapsed;
                progressBar.Report(currentCount / (float)serieCount);
                //logger.LogInformation("{current:0} %", currentCount / (float)serieCount * 100F);
            }
        });
        var watch = Stopwatch.StartNew();
        logger.LogInformation("Parallelizing work...");
        watch.Restart();
        Parallel.ForEach(Enumerable.Range(0, serieCount), new ParallelOptions() { MaxDegreeOfParallelism = generationSettings.CurrentValue.WorkersCount }, (i, token) =>
        {
            var res = GenerateSingle(serieCount, generateRarities, i, generationProgressReporter);
        });
        progressBar.Dispose();

        var maxRarity = rarities.MaxBy(r => r.Rarity).Rarity;
        var minRarity = rarities.MinBy(r => r.Rarity).Rarity;
        var diff = maxRarity - minRarity;
        foreach (var rarity in rarities)
        {
            rarity.Rarity = (float)(100F * (rarity.Rarity - minRarity) / diff);
            Serializer.SerializeJson($"{Paths.RESULTS}rarities\\{rarity.Id}-rarity.json", rarity);
        }
        watch.Stop();

        logger.LogInformation("Completed in {elapsed:0.000} s", watch.ElapsedMilliseconds / 1000F);
    }

    private string GenerateSingle(int serieCount, bool generateRarities, int index, IProgress<int> progress = null)
    {
        var meta = nftMetadataBlueprint.Clone();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = $"{Paths.RESULTS}\\{index}.png";

        var iterationPicks = new List<LayerPick>();
        var cycle = 0;
        do
        {
            if (cycle > 0)
            {
                //logger.LogWarning("Found duplicate, brute forcing again");
            }
            cycle++;
            iterationPicks.Clear();

            for (var i = 0; i < filesystem.Layers.Count; i++)
            {
                var pick = filesystem.Layers[i].GetRandom();
                mintedHash[i] = pick.Id;

                iterationPicks.Add(new LayerPick() { Asset = pick, Layer = filesystem.Layers[i] });
            }
            if (generationSettings.CurrentValue.AllowDuplicates) break;
        }
        while (!IsHashValid(mintedHash));

        if (iterationPicks.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return string.Empty;
        }
        double rarityScore = 1F;
        var attributes = new List<AttributeMetadata>();
        for (var i = 0; i < iterationPicks.Count; i++)
        {
            Interlocked.Increment(ref iterationPicks[i].Asset.usedAmount);
            rarityScore /= iterationPicks[i].Asset.Metadata.Attribute.Rarity;
            if (iterationPicks[i].Asset.Metadata.Attribute.Trait != string.Empty)
            {
                attributes.Add(iterationPicks[i].Asset.Metadata.Attribute);
            }
        }

        var assets = filesystem.FallbackMetadata.BuildMediaProviders(iterationPicks, ref rarityScore, attributes, ref mintedHash);
        progress?.Report(1);
        Media.ComposePNG(resPath, logger, filter, assets.ToArray());

        meta.AddAttributes(attributes);

        rarityScore /= MathF.Pow(10F, filesystem.Layers.Count - 2);
        //rarityScore = rarityScore.Remap(filesystem.MinRarity, filesystem.MaxRarity, 0F, 1_000_000_000);
        if (generateRarities)
        {
            lock (rarities)
            {
                rarities.Add(new RarityMetadata() { Id = index, Rarity = rarityScore, Hash = mintedHash });
            }
        }

        meta.Name += $"#{index}";
        meta.Image = $"{index}.png";
        meta.Properties.Files = new List<FileMetadata>() { new FileMetadata() { Uri = meta.Image, Type = "image/png" } };

        Serializer.SerializeJson($"{Paths.RESULTS}{index}.json", meta);
        lock (generatedHashes)
        {
            generatedHashes.Add(mintedHash);
        }
        return $"{index}.json";
    }

    private bool IsHashValid(IEnumerable<int> current)
    {
        lock (generatedHashes)
        {
            foreach (var hash in generatedHashes)
            {
                var valid = false;
                for (var i = 0; i < current.Count(); i++)
                {
                    if (current.ElementAt(i) != hash[i])
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    return false;
                }
            }
            return true;
        }
    }
}