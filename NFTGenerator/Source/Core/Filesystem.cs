// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator.Source.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator;

internal class Filesystem
{
    private readonly Logger logger;

    public List<Layer> Layers { get; private set; }

    public List<AssetFallback> AssetFallbacks { get; private set; }

    public Filesystem(Logger logger)
    {
        Layers = new List<Layer>();
        this.logger = logger;
        AssetFallbacks = new List<AssetFallback>();
    }

    public bool Verify(bool verbose = true)
    {
        List<Action> warnings = new List<Action>();
        var amountToMint = Configurator.Options.Generation.SerieCount;
        Load(verbose);
        if (verbose)
        {
            logger?.LogInfo("Verifying whether layers are fucked up or not...");
        }
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
        if (verbose)
        {
            logger?.LogInfo("Verifying some weird math...");
        }
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
        bool incompatiblesError = false;
        foreach (AssetFallback fallback in AssetFallbacks)//TODO finish verifying fallbacks
        {
            if (fallback.Metadata.Incompatibles.Length != Layers.Count)
            {
                logger?.LogError($"Incorrect incompatibles count in fallback: {fallback.Id} ");
                incompatiblesError = true;
            }
        }
        if (incompatiblesError) return false;
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

    private void Load(bool verbose = true)
    {
        Layers.Clear();
        AssetFallbacks.Clear();
        if (!Directory.Exists(Configurator.Options.FilesystemPath))
        {
            Directory.CreateDirectory(Configurator.Options.FilesystemPath);
            logger?.LogInfo("Created FS root directory: " + Configurator.Options.FilesystemPath);
        }
        if (!Directory.Exists(Configurator.Options.FilesystemPath + "\\layers"))
        {
            Directory.CreateDirectory(Configurator.Options.FilesystemPath + "\\layers");
            logger?.LogInfo("Created FS root directory: " + Configurator.Options.FilesystemPath + "\\layers");
        }
        if (!Directory.Exists(Configurator.Options.ResultsPath))
        {
            Directory.CreateDirectory(Configurator.Options.ResultsPath);
            logger?.LogInfo("Created FS root directory: " + Configurator.Options.ResultsPath);
        }

        if (verbose) logger?.LogInfo("Loading layers");
        var dirs = Directory.GetDirectories(Configurator.Options.FilesystemPath + "\\layers");
        for (var i = 0; i < dirs.Length; i++)
        {
            Layer layer = new(dirs[i]);
            var assets = Directory.GetDirectories(dirs[i]);
            var currentAssets = assets.Length;
            for (var j = 0; j < assets.Length; j++)
            {
                var assetPath = assets[j];
                if (Asset.TryParse(out Asset asset, assetPath, j, logger))
                {
                    layer.Assets.Add(asset);
                    //logger.LogInfo($"Asset {j}: {asset.Id}, {assetPath}");
                }
            }
            if (currentAssets > 0)
            {
                Layers.Add(layer);
                //logger.LogInfo($"Layer {i}: {layer.Name}");
            }
        }
        if (Directory.Exists(Configurator.Options.FilesystemPath + "\\layers_fallback"))
        {
            var fallbackDirs = Directory.GetDirectories(Configurator.Options.FilesystemPath + "\\layers_fallback");
            if (verbose)
            { logger?.LogInfo("Loading asset fallbacks"); }
            for (var i = 0; i < fallbackDirs.Length; i++)
            {
                if (AssetFallback.TryParse(out AssetFallback fallback, fallbackDirs[i], i, logger))
                {
                    AssetFallbacks.Add(fallback);
                }
            }
            logger?.LogInfo($"Found {AssetFallbacks.Count} fallbacks");
        }
    }
}