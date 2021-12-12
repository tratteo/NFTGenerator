// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator.Source.Metadata;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NFTGenerator;

internal static class CommandsDelegates
{
    public static void VerifyCMD(Filesystem filesystem, string path, Logger logger)
    {
        switch (path)
        {
            case "res":
                var valid = true;
                var assets = Directory.GetFiles(Configurator.Options.ResultsPath, "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
                if (assets.Length <= 0)
                {
                    logger.LogWarning("There is nothing in here");
                    return;
                }
                var extension = string.Empty;
                foreach (var asset in assets)
                {
                    FileInfo file = new(asset);
                    if (!extension.Equals(string.Empty) && !extension.Equals(file.Extension))
                    {
                        logger.LogError("Found results with different extensions! This should never happen WTF");
                        valid = false;
                    }
                }

                if (!Configurator.Options.Generation.AssetsOnly)
                {
                    var metadata = Directory.GetFiles(Configurator.Options.ResultsPath, "*.json");

                    if (assets.Length == metadata.Length && metadata.Length == 0)
                    {
                        logger.LogWarning("There is nothing in here");
                        return;
                    }
                    if (assets.Length != metadata.Length)
                    {
                        logger.LogError("There are different numbers of assets and metadata. How the fuck did you manage to do such a thing");
                        valid = false;
                    }
                    foreach (var data in metadata)
                    {
                        if (Serializer.DeserializeJson<NFTMetadata>(string.Empty, data, out var nftData))
                        {
                            if (!nftData.Valid(logger))
                            {
                                logger.LogError($"Errors on metadata: {data}");
                                logger.LogInfo("\n");
                                valid = false;
                            }
                        }
                    }
                }
                if (valid)
                {
                    logger.LogInfo("All good in the results folder");
                }
                break;

            case "fs":
                filesystem.Verify(true);
                break;
        }
    }

    public static void OpenPathCMD(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
    {
        switch (handler.GetPositional(0))
        {
            case "fs":
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath);
                break;

            case "res":
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath);
                break;

            case "layers":
                if (handler.GetKeyed("-n", out string layerNumber))
                {
                    if (int.TryParse(layerNumber, out int layerId) && layerId >= 0 && layerId < filesystem.Layers.Count)
                    {
                        Layer layer = filesystem.Layers[layerId];
                        logger?.LogInfo(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\" + layer.Name);
                        Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\layers\\" + layer.Name);
                    }
                }
                else
                {
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\layers");
                }
                break;

            case "config":
                using (Process fileopener = new())
                {
                    fileopener.StartInfo.FileName = "explorer"; fileopener.StartInfo.Arguments = Paths.CONFIG_PATH + Configurator.OPTIONS_NAME; fileopener.Start();
                }
                break;

            case "fallbacks":
                if (handler.GetKeyed("-n", out string fallbackNumber))
                {
                    if (int.TryParse(fallbackNumber, out int fallbackId) && fallbackId >= 0 && fallbackId < filesystem.AssetFallbacks.Count)
                    {
                        logger?.LogInfo(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + $"\\fallback_{fallbackId}");
                        Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\layers_fallback\\" + $"fallback_{fallbackId}");
                    }
                }
                else
                {
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\layers_fallback");
                }
                break;

            case "root":
                Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory);
                break;

            default:
                logger.LogWarning("Unable to find path");
                break;
        }
    }

    //overloaded method needed to create the fallback folder given the number of the eventual fallbacks
    public static void CreateFilesystemSchemaCMD(Filesystem filesystem, string fallbacks, Logger logger)
    {
        int fallbackNumber;
        try
        {
            fallbackNumber = int.Parse(fallbacks);
        }
        catch (Exception)
        {
            logger.LogError("Arguments must be integers");
            return;
        }
        for (var i = 0; i < fallbackNumber; i++)
        {
            var fallbackName = $"fallback_{i}";
            Directory.CreateDirectory(fallbackName);
            Serializer.SerializeJson($"{Configurator.Options.FilesystemPath}\\layers_fallback\\{fallbackName}\\",
                "fallback_metadata.json", AssetFallbackMetadata.Blueprint());
        }
    }

    public static void CreateFilesystemSchemaCMD(Filesystem filesystem, string layers, string assets, Logger logger)
    {
        int layersNumber;
        int assetsNumber;
        try
        {
            layersNumber = int.Parse(layers);
            assetsNumber = int.Parse(assets);
        }
        catch (Exception)
        {
            logger.LogError("Arguments must be integers");
            return;
        }
        for (var i = 0; i < layersNumber; i++)
        {
            var layerName = $"layer_{i}";
            for (var j = 0; j < assetsNumber; j++)
            {
                var assetFolder = $"asset_{j}";
                //Directory.CreateDirectory(assetFolder);
                Serializer.SerializeJson($"{Configurator.Options.FilesystemPath}\\layers\\{layerName}\\{assetFolder}\\",
                    "metadata.json", AssetMetadata.Blueprint());
            }
        }
    }

    public static void PurgePathCMD(Filesystem filesystem, string path, bool force, Logger logger)
    {
        string answer;
        switch (path)
        {
            case "res":
                if (force)
                {
                    PurgeRecursive(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath, logger);
                    logger.LogInfo("Purged results");
                }
                else
                {
                    logger.LogInfo("Are you sure you want to purge results folder? (Y/N)", ConsoleColor.DarkGreen);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        PurgeRecursive(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath, logger);
                        logger.LogInfo("Purged results");
                    }
                }
                break;

            case "layers":
                if (force)
                {
                    PurgeRecursive(Configurator.Options.FilesystemPath + "\\layers", logger);
                    logger.LogInfo("Purged layers");
                }
                else
                {
                    logger.LogInfo("Are you sure you want to purge layers? (Y/N)", ConsoleColor.DarkGreen);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        PurgeRecursive(Configurator.Options.FilesystemPath + "\\layers", logger);
                        logger.LogInfo("Purged layers");
                    }
                }
                break;

            case "fallbacks":
                if (force)
                {
                    PurgeRecursive(Configurator.Options.FilesystemPath + "\\layers_fallback", logger);
                    logger.LogInfo("Purged fallbacks");
                }
                else
                {
                    logger.LogInfo("Are you sure you want to purge fallbacks? (Y/N)", ConsoleColor.DarkGreen);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        PurgeRecursive(Configurator.Options.FilesystemPath + "\\layers_fallback", logger);
                        logger.LogInfo("Purged fallbacks");
                    }
                }
                break;
        }
    }

    private static void PurgeRecursive(string path, Logger logger = null)
    {
        var amount = 0;
        DirectoryInfo dInfo = new DirectoryInfo(path);
        foreach (FileInfo file in dInfo.EnumerateFiles())
        {
            amount++;
            file.Delete();
        }
        logger?.LogInfo($"Deleted {amount} files");
        amount = 0;
        foreach (DirectoryInfo dir in dInfo.EnumerateDirectories())
        {
            amount++;
            dir.Delete(true);
        }
        logger?.LogInfo($"Deleted {amount} directories");
    }
}