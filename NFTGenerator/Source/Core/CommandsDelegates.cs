// Copyright Matteo Beltrame

using HandierCli;
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
                var assets = Directory.GetFiles($"{Paths.RESULTS}", "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
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
                    var metadata = Directory.GetFiles($"{Paths.RESULTS}", "*.json");

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
                logger.LogInfo($"{Paths.FILESYSTEM}");
                Process.Start("explorer.exe", $"{Paths.FILESYSTEM}");
                break;

            case "res":
                Process.Start("explorer.exe", $"{Paths.RESULTS}");
                break;

            case "layer":
                if (handler.GetKeyed("-n", out string layerNumber))
                {
                    if (int.TryParse(layerNumber, out int layerId) && layerId >= 0 && layerId < filesystem.Layers.Count)
                    {
                        Layer layer = filesystem.Layers[layerId];
                        Process.Start("explorer.exe", $"{Paths.LAYERS}{layer.Name}");
                    }
                }
                else
                {
                    Process.Start("explorer.exe", $"{Paths.LAYERS}");
                }
                break;

            case "config":
                using (Process fileopener = new())
                {
                    fileopener.StartInfo.FileName = "explorer"; fileopener.StartInfo.Arguments = $"{Paths.CONFIG}{Configurator.OPTIONS_NAME}";
                    fileopener.Start();
                }
                break;

            case "fallbacks":
                Process.Start("explorer.exe", $"{Paths.FALLBACKS}");
                break;

            case "root":
                Process.Start("explorer.exe", $"{Paths.ROOT}");
                break;

            default:
                logger.LogWarning("Unable to find path");
                break;
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
                    PurgeRecursive($"{Paths.RESULTS}", logger);
                    logger.LogInfo("Purged results");
                }
                else
                {
                    logger.LogInfo("Are you sure you want to purge results folder? (Y/N)", ConsoleColor.DarkYellow);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        PurgeRecursive($"{Paths.RESULTS}", logger);
                        logger.LogInfo("Purged results");
                    }
                }
                break;

            case "layers":
                logger.LogInfo("Are you sure you want to purge layers? (Y/N)", ConsoleColor.DarkYellow);
                answer = Console.ReadLine();
                if (answer.ToLower().Equals("y"))
                {
                    PurgeRecursive($"{Paths.LAYERS}", logger);
                    logger.LogInfo("Purged layers");
                }
                break;

            case "fallbacks":
                logger.LogInfo("Are you sure you want to purge fallbacks? (Y/N)", ConsoleColor.DarkYellow);
                answer = Console.ReadLine();
                if (answer.ToLower().Equals("y"))
                {
                    PurgeRecursive($"{Paths.FALLBACKS}", logger);
                    logger.LogInfo("Purged fallbacks");
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