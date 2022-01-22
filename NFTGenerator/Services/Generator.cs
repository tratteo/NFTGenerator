// Copyright Matteo Beltrame

using BetterHaveIt;
using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        logger = loggerFactory.CreateLogger("Generator");
        filesystem = services.GetService<IFilesystem>();
        configuration = services.GetService<IConfiguration>();
        generatedHashes = new List<int[]>();
        nftMetadataBlueprint = NFTMetadata.Template();
    }

    public void Generate()
    {
        int serieCount = configuration.GetValue<int>("Generation:SerieCount");
        bool generateRarities = configuration.GetValue<bool>("Generation:GenerateRarities");
        int currentCount = 0;
        Stopwatch reportWatch = Stopwatch.StartNew();
        long lastReport = 0;
        Progress<int> generationProgressReporter = new Progress<int>((p) =>
        {
            currentCount++;
            long currentElapsed = reportWatch.ElapsedMilliseconds;
            if (currentElapsed - lastReport > 250)
            {
                lastReport = currentElapsed;
                ConsoleExtensions.ClearConsoleLine();
                logger.LogInformation("{current:0} %", currentCount / (float)serieCount * 100F);
            }
        });
        Stopwatch watch = Stopwatch.StartNew();
        logger.LogInformation("Parallelizing work...");
        watch.Restart();
        Parallel.ForEach(Enumerable.Range(0, serieCount), new ParallelOptions() { MaxDegreeOfParallelism = 30 }, (i, token) =>
        {
            string res = GenerateSingle(serieCount, generateRarities, i, generationProgressReporter);
        });
        watch.Stop();
        logger.LogInformation("Completed in {elapsed:0.000} s", watch.ElapsedMilliseconds / 1000F);
    }

    private string GenerateSingle(int serieCount, bool generateRarities, int index, IProgress<int> progress = null)
    {
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

        if (toMerge.Count < 2)
        {
            logger.LogError("Unable to merge less than 2 assets!");
            return string.Empty;
        }
        for (int i = 0; i < toMerge.Count; i++)
        {
            toMerge[i].Asset.UsedAmount++;
        }

        List<IMediaProvider> assets = filesystem.FallbackMetadata.BuildMediaProviders(toMerge);
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
        if (generateRarities)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (int val in mintedHash)
            {
                stringBuilder.Append($"{val} ");
            }
            string report = stringBuilder.ToString();
            Serializer.WriteAll($"{Paths.RESULTS}\\rarities\\", $"{index}.rarity", $"Probability: {generationProbability}\nHash: {stringBuilder}");
        }

        meta.Name += $"#{index}";
        meta.Image = $"{index}.png";
        meta.Properties.Files = new List<FileMetadata>() { new FileMetadata() { Uri = meta.Image, Type = "image/png" } };

        Serializer.SerializeJson($"{Paths.RESULTS}\\", $"{index}.json", meta);
        generatedHashes.Add(mintedHash);
        return $"{index}.json";
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