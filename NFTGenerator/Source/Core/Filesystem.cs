// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Filesystem.cs
//
// All Rights Reserved

using GibNet.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Filesystem
    {
        private readonly Logger logger;

        public List<Layer> Layers { get; private set; }

        public string MediaExtension { get; private set; } = string.Empty;

        public Filesystem(Logger logger)
        {
            Layers = new List<Layer>();
            this.logger = logger;
        }

        public bool Verify(bool verbose = true, bool cleanInvalid = false)
        {
            List<Action> warnings = new List<Action>();
            int amountToMint = Configurator.Options.Generation.SerieCount;
            Load(verbose, cleanInvalid);
            if (verbose)
            {
                logger.LogInfo("Verifying whether layers are fucked up or not...");
            }
            string fileExtension = string.Empty;
            foreach (Layer layer in Layers)
            {
                int amount = 0;
                foreach (Asset a in layer.Assets)
                {
                    amount += a.Metadata.Amount;
                    FileInfo info = new(a.AssetAbsolutePath);

                    if (info.Extension != fileExtension && fileExtension != string.Empty)
                    {
                        logger.LogError("Assets are not of the same type at: " + a.AssetAbsolutePath);
                        return false;
                    }
                    fileExtension = info.Extension;
                }
                MediaExtension = fileExtension;
                if (amount < amountToMint)
                {
                    logger.LogError("Wrong assets sum in layer: " + layer.Path);
                    return false;
                }
                else if (amount > amountToMint)
                {
                    warnings.Add(() => logger.LogWarning("Assets sum in layer: " + layer.Path + " is greater than the AMOUNT_TO_MINT, adjust it if you want amounts in metadata to actually represents probabilities"));
                }
            }
            if (verbose)
            {
                logger.LogInfo("Verifying some weird math...");
            }
            int dispositions = 1;
            Layers.ForEach(l =>
            {
                dispositions *= l.Assets.Count;
            });
            if (dispositions < amountToMint)
            {
                logger.LogError("There are less mathematical available disposition than the amount to mint (" + amountToMint + ")");
                return false;
            }
            if (amountToMint == 0)
            {
                warnings.Add(() => logger.LogWarning("The amount to mint is set to 0 in the configuration file"));
            }
            if (verbose)
            {
                logger.LogInfo("Verification process passed with " + warnings.Count + " warnings", ConsoleColor.Green);
                foreach (Action w in warnings)
                {
                    w?.Invoke();
                }
            }
            return true;
        }

        private bool Layout()
        {
            bool created = false;
            if (!Directory.Exists(Configurator.Options.FilesystemPath))
            {
                Directory.CreateDirectory(Configurator.Options.FilesystemPath);
                logger.LogInfo("Created FS root directory: " + Configurator.Options.FilesystemPath);
                created = true;
            }
            if (!Directory.Exists(Configurator.Options.FilesystemPath + "\\layers"))
            {
                Directory.CreateDirectory(Configurator.Options.FilesystemPath + "\\layers");
                logger.LogInfo("Created FS root directory: " + Configurator.Options.FilesystemPath + "\\layers");
                created = true;
            }
            if (!Directory.Exists(Configurator.Options.ResultsPath))
            {
                Directory.CreateDirectory(Configurator.Options.ResultsPath);
                logger.LogInfo("Created FS root directory: " + Configurator.Options.ResultsPath);
                created = true;
            }
            return created;
        }

        private void Load(bool verbose = true, bool cleanInvalid = false)
        {
            Layers.Clear();
            Layout();
            if (verbose)
            {
                logger.LogInfo("Loading layers");
            }
            string[] dirs = Directory.GetDirectories(Configurator.Options.FilesystemPath + "\\layers");
            for (int i = 0; i < dirs.Length; i++)
            {
                Layer layer = new(dirs[i]);
                string[] assets = Directory.GetDirectories(dirs[i]);
                int currentAssets = assets.Length;
                for (int j = 0; j < assets.Length; j++)
                {
                    string assetPath = assets[j];
                    if (Asset.TryCreate(out Asset asset, assetPath, j, logger))
                    {
                        layer.Assets.Add(asset);
                    }
                    else if (cleanInvalid)
                    {
                        Directory.Delete(assetPath, true);
                        currentAssets--;
                    }
                }
                if (currentAssets > 0)
                {
                    Layers.Add(layer);
                }
                else if (cleanInvalid)
                {
                    Directory.Delete(dirs[i]);
                }
            }
        }
    }
}