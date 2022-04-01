// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using NFTGenerator.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NFTGenerator.Services;

internal class Filesystem : IFilesystem
{
    private readonly ILogger logger;
    private readonly IOptionsMonitor<GenerationSettings> generationSettings;

    public List<Layer> Layers { get; private set; }

    public FallbackMetadata FallbackMetadata { get; private set; }

    public double MinRarity { get; private set; }

    public double MaxRarity { get; private set; }

    public Filesystem(ILoggerFactory loggerFactory, IOptionsMonitor<GenerationSettings> generationSettings)
    {
        Layers = new List<Layer>();
        logger = loggerFactory.CreateLogger("Filesystem");
        this.generationSettings = generationSettings;
    }

    public float CalculateDispositions()
    {
        var dispositions = float.MaxValue;
        var bound = 1;
        foreach (var layer in Layers)
        {
            bound *= layer.Assets.Count;
            float avgAssetsDistrib = 1;
            foreach (var asset in layer.Assets)
            {
                avgAssetsDistrib *= asset.Metadata.Amount;
            }
            if (avgAssetsDistrib < dispositions)
            {
                dispositions = avgAssetsDistrib;
            }
        }
        return Math.Min(dispositions, bound);
    }

    public bool Verify()
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var serieAmount = generationSettings.CurrentValue.SerieCount;
        // TODO change
        var verbose = true;

        #region Create folders

        Layers.Clear();
        if (!Directory.Exists($"{Paths.FILESYSTEM}"))
        {
            Directory.CreateDirectory($"{Paths.FILESYSTEM}");
            logger?.LogInformation("Created FS root directory: " + $"{Paths.FILESYSTEM}");
        }
        if (!Directory.Exists($"{Paths.LAYERS}"))
        {
            Directory.CreateDirectory($"{Paths.LAYERS}");
            logger?.LogInformation($"Created FS root directory: {Paths.LAYERS}");
        }
        if (!Directory.Exists($"{Paths.RESULTS}"))
        {
            Directory.CreateDirectory($"{Paths.RESULTS}");
            logger?.LogInformation("Created FS root directory: " + $"{Paths.RESULTS}");
        }

        #endregion Create folders

        var assetsCount = 0;

        var layerNames = Directory.GetDirectories($"{Paths.LAYERS}");
        for (var i = 0; i < layerNames.Length; i++)
        {
            var amount = 0;
            Layer layer = new(layerNames[i], i);
            //logger.LogInfo($"Layer {i}: {layer.Name}");
            var reg = new Regex(@"[0-9]+.json");
            var assets = Directory.GetFiles(layerNames[i], "*.json").Where(s => reg.IsMatch(s));
            for (var j = 0; j < assets.Count(); j++)
            {
                var name = PathExtensions.Split(assets.ElementAt(j)).Item2.Replace(".json", string.Empty);

                if (!int.TryParse(name, out var id) || !Asset.TryParse(out var asset, layerNames[i], id, logger)) continue;

                layer.Assets.Add(asset);
                asset.Metadata.Attribute.Rarity = (float)Math.Round(100F * asset.Metadata.Amount / serieAmount, 2);
                //logger.LogInfo($"Asset {j} | id: {asset.Id}");
                amount += asset.Metadata.Amount;
            }
            //layer.SetAssetsPickProbabilities();
            if (layer.Assets.Count > 0)
            {
                Layers.Add(layer);
            }
            if (amount < serieAmount)
            {
                errors.Add($"Wrong assets sum in layer: {layer.Name}: {amount}");
            }
            else if (amount > serieAmount)
            {
                warnings.Add($"Assets sum in layer: {layer.Name} is greater than the SERIE_AMOUNT");
            }
            assetsCount += layer.Assets.Count;
        }

        if (verbose) logger?.LogInformation($"Parsed {Layers.Count} layers for a total of {assetsCount} assets");
        var dispositions = 1;
        Layers.ForEach(l => dispositions *= l.Assets.Count);
        if (dispositions < serieAmount)
        {
            errors.Add($"There are less mathematical available disposition than the amount to mint ({serieAmount})");
        }
        if (serieAmount == 0)
        {
            warnings.Add("The amount to mint is set to 0 in the configuration file");
        }

        if (Directory.Exists($"{Paths.FALLBACKS}"))
        {
            if (Serializer.DeserializeJson($"{Paths.FALLBACKS}", "fallbacks.json", out FallbackMetadata meta))
            {
                FallbackMetadata = meta;
                if (!FallbackMetadata.Verify(this))
                {
                    errors.Add("Errors in fallback metadata");
                }
                logger?.LogInformation($"Parsed {FallbackMetadata.GetFallbacksByPriority().Count} incompatibles definitions");
                var incompatibles = FallbackMetadata.GetFallbacksByPriority();
                var enable = incompatibles.FindAll(l => l.Enabled).Count;
                logger?.LogInformation("{en} enabled, {dis} disabled", enable, incompatibles.Count - enable);
            }
            else
            {
                warnings.Add("Unable to deserialize fallbacks metadata");
            }
        }

        MinRarity = 1F;
        MaxRarity = 1F;

        foreach (var layer in Layers)
        {
            var maxRarityAsset = layer.Assets.MinBy(a => a.Metadata.Attribute.Rarity);
            var minRarityAsset = layer.Assets.MaxBy(a => a.Metadata.Attribute.Rarity);

            if (verbose)
            {
                logger.LogInformation("Layer {layer}, max[{max},{v1}], min[{min},{v2}]", layer.Name, maxRarityAsset.Id, maxRarityAsset.Metadata.Attribute.Rarity, minRarityAsset.Id, minRarityAsset.Metadata.Attribute.Rarity);
            }
            MinRarity /= minRarityAsset.Metadata.Attribute.Rarity;
            MaxRarity /= maxRarityAsset.Metadata.Attribute.Rarity;
        }
        if (verbose)
        {
            logger.LogInformation("min rarity score {min}, max rarity score {max}", MinRarity, MaxRarity);
        }

        errors.ForEach(e => logger?.LogError(e));
        warnings.ForEach(w => logger?.LogWarning(w));
        if (errors.Count > 0)
        {
            logger?.LogError($"Verification process passed with {errors.Count} errors and {warnings.Count} warnings\n");
        }
        else if (warnings.Count > 0)
        {
            logger?.LogWarning($"Verification process passed with {errors.Count} errors and {warnings.Count} warnings\n");
        }
        else
        {
            logger?.LogInformation($"Verification process passed with {errors.Count} errors and {warnings.Count} warnings\n");
        }
        return errors.Count <= 0;
    }
}