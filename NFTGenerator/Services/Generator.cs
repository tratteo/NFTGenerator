// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
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
    private readonly IServiceProvider services;
    private readonly ILogger logger;
    private readonly TokenMetadata nftMetadataBlueprint;
    private readonly IConfiguration configuration;
    private readonly bool verbose;
    private readonly bool allowDuplicates;
    private readonly Media.Filter? filter;
    private List<int[]> generatedHashes;
    private List<RarityMetadata> rarities;

    public Generator(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        this.services = services;
        logger = loggerFactory.CreateLogger("Generator");
        filesystem = services.GetService<IFilesystem>();
        configuration = services.GetService<IConfiguration>();
        nftMetadataBlueprint = TokenMetadata.Template();
        verbose = configuration.GetValue<bool>("Debug:Verbose");
        allowDuplicates = configuration.GetValue<bool>("Generation:AllowDuplicates");
        string filterParse = configuration.GetValue<string>("Generation:Filter");
        if (filterParse == null)
        {
            filter = null;
        }
        else
        {
            filter = (Media.Filter)Enum.Parse(typeof(Media.Filter), filterParse, true);
        }
        logger.LogInformation("{f}", filter);
    }

    public void Generate()
    {
        rarities = new List<RarityMetadata>();
        generatedHashes = new List<int[]>();
        int serieCount = configuration.GetValue<int>("Generation:SerieCount");
        bool generateRarities = configuration.GetValue<bool>("Generation:GenerateRarities");
        int currentCount = 0;
        Stopwatch reportWatch = Stopwatch.StartNew();
        long lastReport = 0;
        ConsoleProgressBar progressBar = new ConsoleProgressBar(50, "|/-\\", 8);
        Progress<int> generationProgressReporter = new Progress<int>((p) =>
        {
            Interlocked.Increment(ref currentCount);
            long currentElapsed = reportWatch.ElapsedMilliseconds;
            if (currentElapsed - lastReport > 250)
            {
                lastReport = currentElapsed;
                progressBar.Report(currentCount / (float)serieCount);
                //logger.LogInformation("{current:0} %", currentCount / (float)serieCount * 100F);
            }
        });
        Stopwatch watch = Stopwatch.StartNew();
        logger.LogInformation("Parallelizing work...");
        watch.Restart();
        Parallel.ForEach(Enumerable.Range(0, serieCount), new ParallelOptions() { MaxDegreeOfParallelism = configuration.GetValue<int>("Generation:WorkersCount") }, (i, token) =>
        {
            string res = GenerateSingle(serieCount, generateRarities, i, generationProgressReporter);
        });
        progressBar.Dispose();

        double maxRarity = rarities.MaxBy(r => r.Rarity).Rarity;
        double minRarity = rarities.MinBy(r => r.Rarity).Rarity;
        double diff = maxRarity - minRarity;
        foreach (var rarity in rarities)
        {
            rarity.Rarity = (float)(100F * (rarity.Rarity - minRarity) / diff);
            Serializer.SerializeJson($"{Paths.RESULTS}rarities\\", $"{rarity.Id}-rarity.json", rarity);
        }
        watch.Stop();

        logger.LogInformation("Completed in {elapsed:0.000} s", watch.ElapsedMilliseconds / 1000F);
    }

    private string GenerateSingle(int serieCount, bool generateRarities, int index, IProgress<int> progress = null)
    {
        TokenMetadata meta = nftMetadataBlueprint.Clone();
        var mintedHash = new int[filesystem.Layers.Count];
        var resPath = $"{Paths.RESULTS}\\{index}.png";

        var iterationPicks = new List<LayerPick>();
        int cycle = 0;
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
                Asset pick = filesystem.Layers[i].GetRandom();
                mintedHash[i] = pick.Id;

                iterationPicks.Add(new LayerPick() { Asset = pick, Layer = filesystem.Layers[i] });
            }
            if (allowDuplicates) break;
        }
        while (!IsHashValid(mintedHash));

        if (iterationPicks.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return string.Empty;
        }
        double rarityScore = 1F;
        var attributes = new List<AttributeMetadata>();
        for (int i = 0; i < iterationPicks.Count; i++)
        {
            Interlocked.Increment(ref iterationPicks[i].Asset.usedAmount);
            rarityScore /= iterationPicks[i].Asset.Metadata.Attribute.Rarity;
            if (iterationPicks[i].Asset.Metadata.Attribute.Trait != string.Empty)
            {
                attributes.Add(iterationPicks[i].Asset.Metadata.Attribute);
            }
        }

        List<string> assets = filesystem.FallbackMetadata.BuildMediaProviders(iterationPicks, ref rarityScore, attributes, ref mintedHash);
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

        Serializer.SerializeJson($"{Paths.RESULTS}\\", $"{index}.json", meta);
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
            foreach (int[] hash in generatedHashes)
            {
                bool valid = false;
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