// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NFTGenerator.Services;

internal class Filesystem : IFilesystem
{
    private readonly IServiceProvider services;
    private readonly ILogger logger;
    private readonly IConfiguration configuration;

    public List<Layer> Layers { get; private set; }

    public FallbackMetadata FallbackMetadata { get; private set; }

    public Filesystem(IServiceProvider services, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        Layers = new List<Layer>();
        this.services = services;
        this.logger = loggerFactory.CreateLogger("Filesystem");
        this.configuration = configuration;
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

    public bool Verify()
    {
        List<string> warnings = new List<string>();
        List<string> errors = new List<string>();
        var serieAmount = configuration.GetValue<int>("Generation:SerieCount");
        bool verbose = configuration.GetSection("Debug:Verbose").Get<bool>();

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

        int assetsCount = 0;
        var fileExtension = string.Empty;
        var layerNames = Directory.GetDirectories($"{Paths.LAYERS}");
        for (var i = 0; i < layerNames.Length; i++)
        {
            int amount = 0;
            Layer layer = new(layerNames[i], i);
            //logger.LogInfo($"Layer {i}: {layer.Name}");
            Regex reg = new Regex(@"[0-9]+.json");
            var assets = Directory.GetFiles(layerNames[i], "*.json").Where(s => reg.IsMatch(s));
            for (var j = 0; j < assets.Count(); j++)
            {
                string name = PathExtensions.Split(assets.ElementAt(j)).Item2.Replace(".json", string.Empty);

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
                if (!FallbackMetadata.Verify(Layers.Count))
                {
                    errors.Add("Errors in fallback metadata");
                }
                logger?.LogInformation($"Parsed {FallbackMetadata.GetFallbacksByPriority().Count} incompatibles definitions");
            }
            else
            {
                warnings.Add("Unable to deserialize fallbacks metadata");
            }
        }
        ConsoleColor color = warnings.Count > 0 ? ConsoleColor.DarkYellow : ConsoleColor.Green;
        color = errors.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.Green;
        errors.ForEach(e => logger?.LogError(e));
        warnings.ForEach(w => logger?.LogWarning(w));
        logger?.LogInformation($"Verification process passed with {errors.Count} errors and {warnings.Count} warnings\n", color);
        CommandLineService cmdService = services.GetService<CommandLineService>();
        if (cmdService != null) cmdService.Cli.Logger.LogInfo(string.Empty);
        return errors.Count <= 0;
    }
}