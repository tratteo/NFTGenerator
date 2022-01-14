// Copyright Matteo Beltrame

using BetterHaveIt;
using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using NFTGenerator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NFTGenerator;

internal static class CommandsDelegates
{
    public static void Verify(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        IConfiguration configuration = services.GetService<IConfiguration>();
        string path = handler.GetPositional(0);
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
                            logger.LogInformation("\n");
                            valid = false;
                        }
                    }
                }
                if (valid)
                {
                    logger.LogInformation("All good in the results folder");
                }
                break;

            case "fs":
                IFilesystem filesystem = services.GetService<IFilesystem>();
                filesystem.Verify();
                break;
        }
    }

    public static void OpenPath(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        IConfiguration configuration = services.GetService<IConfiguration>();
        IFilesystem filesystem = services.GetService<IFilesystem>();
        switch (handler.GetPositional(0))
        {
            case "fs":
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

    public static void PurgePath(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        IConfiguration configuration = services.GetService<IConfiguration>();
        string path = handler.GetPositional(0);
        bool force = handler.HasFlag("/f");
        string answer;
        switch (path)
        {
            case "res":
                if (force)
                {
                    PurgeRecursive($"{Paths.RESULTS}", logger);
                    logger.LogInformation("Purged results");
                }
                else
                {
                    logger.LogInformation("Are you sure you want to purge results folder? (Y/N)", ConsoleColor.DarkYellow);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        PurgeRecursive($"{Paths.RESULTS}", logger);
                        logger.LogInformation("Purged results");
                    }
                }
                break;

            case "layers":
                logger.LogInformation("Are you sure you want to purge layers? (Y/N)", ConsoleColor.DarkYellow);
                answer = Console.ReadLine();
                if (answer.ToLower().Equals("y"))
                {
                    PurgeRecursive($"{Paths.LAYERS}", logger);
                    logger.LogInformation("Purged layers");
                }
                break;

            case "fallbacks":
                logger.LogInformation("Are you sure you want to purge fallbacks? (Y/N)", ConsoleColor.DarkYellow);
                answer = Console.ReadLine();
                if (answer.ToLower().Equals("y"))
                {
                    PurgeRecursive($"{Paths.FALLBACKS}", logger);
                    logger.LogInformation("Purged fallbacks");
                }
                break;
        }
    }

    public static void ScaleSerie(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        IConfiguration configuration = services.GetService<IConfiguration>();
        IFilesystem filesystem = services.GetService<IFilesystem>();
        logger.LogInformation("Are you sure you want to scale the serie number? (Y/N)", ConsoleColor.DarkYellow);
        string answer = Console.ReadLine();
        if (!answer.ToLower().Equals("y"))
        {
            return;
        }
        logger.LogInformation("It will not be possible to scale it back down, consider saving a copy of your filesystem, you want to proceed? (Y/N)", ConsoleColor.DarkYellow);
        answer = Console.ReadLine();
        if (!answer.ToLower().Equals("y"))
        {
            return;
        }
        if (!filesystem.Verify())
        {
            logger.LogError("Unable to scale, filesystem contains errors");
            return;
        }
        else
        {
            int factor = 1;
            try
            {
                factor = int.Parse(handler.GetPositional(0));
                foreach (Layer layer in filesystem.Layers)
                {
                    foreach (Asset asset in layer.Assets)
                    {
                        asset.Metadata.Amount *= factor;
                        Serializer.SerializeJson($"{Paths.LAYERS}{layer.Name}\\", $"{asset.Id}.json", asset.Metadata);
                    }
                }
                configuration["Generation:SerieCount"] = (configuration.GetValue<int>("Generation:SerieCount") * factor).ToString();
            }
            catch (Exception)
            {
                logger.LogError("Unable to parse int factor");
                return;
            }
        }
    }

    public static async Task Generate(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        IConfiguration configuration = services.GetService<IConfiguration>();
        IFilesystem filesystem = services.GetService<IFilesystem>();
        IGenerator generator = services.GetService<IGenerator>();
        if (filesystem.Verify())
        {
            int amountToMint = configuration.GetValue<int>("Generation:SerieCount");
            if (amountToMint == 0)
            {
                logger.LogWarning("Nothing to generate, amount to mint is set to 0");
            }
            else if (amountToMint < 0)
            {
                logger.LogError("Negative amount to mint (" + amountToMint + ") in config file");
            }
            else
            {
                generator.Generate();
                await Task.Delay(250);
                ConsoleExtensions.ClearConsoleLine();
            }
        }
    }

    private static void PurgeRecursive(string path, ILogger logger = null)
    {
        var amount = 0;
        DirectoryInfo dInfo = new DirectoryInfo(path);
        foreach (FileInfo file in dInfo.EnumerateFiles())
        {
            amount++;
            file.Delete();
        }
        logger?.LogInformation($"Deleted {amount} files");
        amount = 0;
        foreach (DirectoryInfo dir in dInfo.EnumerateDirectories())
        {
            amount++;
            dir.Delete(true);
        }
        logger?.LogInformation($"Deleted {amount} directories");
    }
}