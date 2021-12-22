// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NFTGenerator;

internal class Filesystem
{
    private readonly Logger logger;

    public List<Layer> Layers { get; private set; }

    public FallbackMetadata FallbackMetadata { get; private set; }

    public Filesystem(Logger logger)
    {
        Layers = new List<Layer>();
        this.logger = logger;
    }

    public float CalculateDispositions()
    {
        float dispositions = float.MaxValue;
        int bound = 1;
        foreach (Layer layer in Layers)
        {
            bound *= layer.Assets.Count;
            float avgAssetsDistrib = 1;
            foreach (Asset asset in layer.Assets)
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

    public bool Verify(bool verbose = true)
    {
        List<string> warnings = new List<string>();
        List<string> errors = new List<string>();
        var serieAmount = Configurator.Options.Generation.SerieCount;

        #region Create folders

        Layers.Clear();
        if (!Directory.Exists($"{Paths.FILESYSTEM}"))
        {
            Directory.CreateDirectory($"{Paths.FILESYSTEM}");
            logger?.LogInfo("Created FS root directory: " + $"{Paths.FILESYSTEM}");
        }
        if (!Directory.Exists($"{Paths.LAYERS}"))
        {
            Directory.CreateDirectory($"{Paths.LAYERS}");
            logger?.LogInfo($"Created FS root directory: {Paths.LAYERS}");
        }
        if (!Directory.Exists($"{Paths.RESULTS}"))
        {
            Directory.CreateDirectory($"{Paths.RESULTS}");
            logger?.LogInfo("Created FS root directory: " + $"{Paths.RESULTS}");
        }

        #endregion Create folders

        int assetsCount = 0;
        var fileExtension = string.Empty;
        var layerNames = Directory.GetDirectories($"{Paths.LAYERS}");
        for (var i = 0; i < layerNames.Length; i++)
        {
            int amount = 0;
            Layer layer = new(layerNames[i]);
            //logger.LogInfo($"Layer {i}: {layer.Name}");
            Regex reg = new Regex(@"[0-9]+.json");
            var assets = Directory.GetFiles(layerNames[i], "*.json").Where(s => reg.IsMatch(s));
            for (var j = 0; j < assets.Count(); j++)
            {
                string name = Paths.Split(assets.ElementAt(j)).Item2.Replace(".json", string.Empty);

                if (!int.TryParse(name, out int id) || !Asset.TryParse(out Asset asset, layerNames[i], id, logger)) continue;

                layer.Assets.Add(asset);
                //logger.LogInfo($"Asset {j} | id: {asset.Id}");
                amount += asset.Metadata.Amount;
                FileInfo info = new(asset.AssetAbsolutePath);

                if (info.Extension != fileExtension && fileExtension != string.Empty)
                {
                    errors.Add($"Assets are not of the same type at: {asset.AssetAbsolutePath}");
                }
                fileExtension = info.Extension;
            }
            if (layer.Assets.Count > 0)
            {
                Layers.Add(layer);
            }
            if (amount < serieAmount)
            {
                errors.Add($"Wrong assets sum in layer: {layer.Name}");
            }
            else if (amount > serieAmount)
            {
                warnings.Add($"Assets sum in layer: {layer.Name} is greater than the SERIE_AMOUNT");
            }
            assetsCount += layer.Assets.Count;
        }

        if (verbose) logger?.LogInfo($"Parsed {Layers.Count} layers for a total of {assetsCount} assets");
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
                if (!FallbackMetadata.Verify(Layers.Count))
                {
                    errors.Add("Errors in fallback metadata");
                }
                if (verbose) logger?.LogInfo($"Parsed {FallbackMetadata.GetFallbacksByPriority().Count} fallback definitions");
            }
            else
            {
                warnings.Add("Unable to deserialize fallbacks metadata");
            }
        }
        ConsoleColor color = warnings.Count > 0 ? ConsoleColor.DarkYellow : ConsoleColor.Green;
        color = errors.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.Green;
        logger?.LogInfo($"Verification process passed with {errors.Count} errors and {warnings.Count} warnings\n", color);
        warnings.ForEach(w => logger?.LogWarning(w));
        errors.ForEach(e => logger?.LogError(e));
        return errors.Count <= 0;
    }
}