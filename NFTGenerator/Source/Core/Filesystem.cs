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

    public bool Verify(bool verbose = true)
    {
        List<Action> warnings = new List<Action>();
        var amountToMint = Configurator.Options.Generation.SerieCount;

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

        if (verbose) logger?.LogInfo("Loading layers");
        var dirs = Directory.GetDirectories($"{Paths.LAYERS}");
        for (var i = 0; i < dirs.Length; i++)
        {
            Layer layer = new(dirs[i]);
            //logger.LogInfo($"Layer {i}: {layer.Name}");
            Regex reg = new Regex(@"[0-9]+.json");
            var assets = Directory.GetFiles(dirs[i], "*.json").Where(s => reg.IsMatch(s));
            for (var j = 0; j < assets.Count(); j++)
            {
                var assetPath = assets.ElementAt(j);
                if (Asset.TryParse(out Asset asset, dirs[i], j, logger))
                {
                    layer.Assets.Add(asset);
                    //logger.LogInfo($"Asset {j} | id: {asset.Id}");
                }
            }

            if (layer.Assets.Count > 0)
            {
                Layers.Add(layer);
            }
        }

        if (verbose) logger?.LogInfo("Verifying whether layers are fucked up or not...");

        var fileExtension = string.Empty;
        foreach (Layer layer in Layers)
        {
            var amount = 0;
            foreach (Asset a in layer.Assets)
            {
                amount += a.Metadata.Amount;
                FileInfo info = new(a.AssetAbsolutePath);

                if (info.Extension != fileExtension && fileExtension != string.Empty)
                {
                    logger?.LogError("Assets are not of the same type at: " + a.AssetAbsolutePath);
                    return false;
                }
                fileExtension = info.Extension;
            }
            if (amount < amountToMint)
            {
                logger?.LogError("Wrong assets sum in layer: " + layer.Path);
                return false;
            }
            else if (amount > amountToMint)
            {
                warnings.Add(() => logger?.LogWarning("Assets sum in layer: " + layer.Path + " is greater than the SERIE_AMOUNT, adjust it if you want amounts in metadata to actually represent probabilities"));
            }
        }

        if (verbose) logger?.LogInfo("Verifying some weird math...");

        var dispositions = 1;
        Layers.ForEach(l => dispositions *= l.Assets.Count);
        if (dispositions < amountToMint)
        {
            logger?.LogError("There are less mathematical available disposition than the amount to mint (" + amountToMint + ")");
            return false;
        }
        if (amountToMint == 0)
        {
            warnings.Add(() => logger?.LogWarning("The amount to mint is set to 0 in the configuration file"));
        }

        if (Directory.Exists($"{Paths.FALLBACKS}"))
        {
            if (Serializer.DeserializeJson($"{Paths.FALLBACKS}", "fallbacks.json", out FallbackMetadata meta))
            {
                FallbackMetadata = meta;
                if (!FallbackMetadata.Verify(Layers.Count))
                {
                    logger?.LogError("Errors in fallback metadata");
                    return false;
                }
                logger?.LogInfo($"Found {FallbackMetadata.GetFallbacksByPriority().Count} fallbacks definitions");
            }
            else
            {
                logger?.LogWarning("Unable to deserialize fallbacks metadata");
            }
        }
        if (verbose)
        {
            logger?.LogInfo("Verification process passed with " + warnings.Count + " warnings", ConsoleColor.Green);
            foreach (Action w in warnings)
            {
                w?.Invoke();
            }
        }
        return true;
    }
}