// Copyright Matteo Beltrame

using BetterHaveIt;
using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using NFTGenerator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator;

internal static class CommandsDelegates
{
    public static void Verify(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        var configuration = services.GetService<IConfiguration>();
        var path = handler.GetPositional(0);
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
                    if (Serializer.DeserializeJson<TokenMetadata>(data, out var nftData))
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
                var filesystem = services.GetService<IFilesystem>();
                filesystem.Verify();
                break;
        }
    }

    public static void PrepareBatch(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        List<(int index, Bitmap oldImage, TokenMetadata meta)> metadata = new List<(int, Bitmap, TokenMetadata)>();
        var root = handler.GetPositional(0);
        var metas = Directory.GetFiles(root, "*.json");
        logger.LogInformation("Found {amount} metadata", metas.Length);
        var count = 0;
        for (var i = 300; i < 1569; i++, count++)
        {
            logger.LogInformation("Editing {d}", i);
            var met = $"{root}\\{i}.json";
            if (Serializer.DeserializeJson<TokenMetadata>(met, out var nftData))
            {
                if (!File.Exists($"{root}\\{nftData.Image}"))
                {
                    logger.LogError("Unable to find image asset");
                    return;
                }
                foreach (var file in nftData.Properties.Files)
                {
                    file.Uri = $"{count}.png";
                }
                var imageInfo = new FileInfo($"{root}\\{nftData.Image}");
                imageInfo.MoveTo($"{root}\\{count}.png", false);
                nftData.Image = $"{count}.png";
                Serializer.SerializeJson<TokenMetadata>($"{root}\\{count}.json", nftData);
                File.Delete(met);
                //metadata.Add((i, new Bitmap($"{root}\\{nftData.Image}"), nftData));
            }
        }
        //PurgeRecursive(root);
        //foreach (var (index, oldImage, meta) in metadata)
        //{
        //    foreach (var file in meta.Properties.Files)
        //    {
        //        file.Uri = $"{index}.png";
        //    }
        //    meta.Image = $"{index}.png";
        //    oldImage.Save($"{root}\\{index}.png");
        //}
        logger.LogInformation("Serialized {amount} metadata", metadata.Count);
    }

    public static void ApplyFilter(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        Media.Filter filter;
        try
        {
            filter = (Media.Filter)Enum.Parse(typeof(Media.Filter), handler.GetPositional(2), true);
        }
        catch (Exception)
        {
            logger.LogError("Unable to parse filter");
            return;
        }
        using var bitmap = new Bitmap(handler.GetPositional(0));
        Media.ApplyFilter(bitmap, filter).Save(handler.GetPositional(1));
    }

    public static void RenameProgressively(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        var start = 0;
        if (handler.GetKeyed("-si", out var startIndex))
        {
            try
            {
                start = int.Parse(startIndex);
            }
            catch (Exception)
            {
            }
        }
        var counter = start;
        var usingPattern = handler.GetKeyed("-pm", out var pattern);
        var files = usingPattern ? Directory.GetFiles(handler.GetPositional(0), pattern) : Directory.GetFiles(handler.GetPositional(0));
        var dirs = usingPattern ? Directory.GetDirectories(handler.GetPositional(0), pattern) : Directory.GetDirectories(handler.GetPositional(0));
        for (var i = 0; i < dirs.Length; i++, counter++)
        {
            var directoryInfo = new DirectoryInfo(dirs[i]);
            var path = Path.GetDirectoryName(directoryInfo.FullName);
            var name = Path.GetFileName(directoryInfo.FullName);
            if (directoryInfo.Name.Equals($"{counter}")) continue;
            directoryInfo.MoveTo($"{path}\\{counter}");
        }
        for (var i = 0; i < files.Length; i++, counter++)
        {
            var fileInfo = new FileInfo(files[i]);
            fileInfo.MoveTo($"{fileInfo.Directory.FullName}\\{counter}{fileInfo.Extension}", true);
        }
    }

    public static void OpenPath(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        var configuration = services.GetService<IConfiguration>();
        var filesystem = services.GetService<IFilesystem>();
        switch (handler.GetPositional(0))
        {
            case "fs":
                Process.Start("explorer.exe", $"{Paths.FILESYSTEM}");
                break;

            case "res":
                Process.Start("explorer.exe", $"{Paths.RESULTS}");
                break;

            case "layer":
                if (handler.GetKeyed("-n", out var layerNumber))
                {
                    if (int.TryParse(layerNumber, out var layerId) && layerId >= 0 && layerId < filesystem.Layers.Count)
                    {
                        var layer = filesystem.Layers[layerId];
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
        var configuration = services.GetService<IConfiguration>();
        var path = handler.GetPositional(0);
        var force = handler.HasFlag("/f");
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
        var configuration = services.GetService<IConfiguration>();
        var filesystem = services.GetService<IFilesystem>();
        logger.LogInformation("Are you sure you want to scale the serie number? (Y/N)", ConsoleColor.DarkYellow);
        var answer = Console.ReadLine();
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
            var factor = 1;
            try
            {
                factor = int.Parse(handler.GetPositional(0));
                foreach (var layer in filesystem.Layers)
                {
                    foreach (var asset in layer.Assets)
                    {
                        asset.Metadata.Amount *= factor;
                        Serializer.SerializeJson($"{Paths.LAYERS}{layer.Name}\\{asset.Id}.json", asset.Metadata);
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
        var configuration = services.GetService<IConfiguration>();
        var filesystem = services.GetService<IFilesystem>();
        var generator = services.GetService<IGenerator>();
        if (filesystem.Verify())
        {
            var amountToMint = configuration.GetValue<int>("Generation:SerieCount");
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

    public static async Task QueryResults(ArgumentsHandler handler, IServiceProvider services, ILogger logger)
    {
        var filesystem = services.GetService<IFilesystem>();
        var configuration = services.GetService<IConfiguration>();
        if (!Directory.Exists(Paths.RESULTS))
        {
            logger.LogWarning("There are no generated results");
            return;
        }
        if (!Directory.Exists($"{Paths.RESULTS}rarities"))
        {
            logger.LogWarning("There are no generated rarities");
            return;
        }
        var files = Directory.GetFiles($"{Paths.RESULTS}rarities", "*.json", SearchOption.AllDirectories);
        logger.LogInformation($"{Paths.RESULTS}rarities");
        if (files.Length != configuration.GetValue<int>("Generation:SerieCount"))
        {
            logger.LogWarning("The number of generated rarities differs from the serie amount");
            return;
        }
        if (!filesystem.Verify())
        {
            return;
        }
        var matches = new List<RarityMetadata>();
        var pattern = handler.GetPositional(0);
        if (pattern.ToLower().Equals("max"))
        {
            RarityMetadata maxRarityMetadata = null;
            foreach (var file in files)
            {
                if (Serializer.DeserializeJson<RarityMetadata>(file, out var rarityMetadata))
                {
                    if (maxRarityMetadata is null || maxRarityMetadata.Rarity < rarityMetadata.Rarity)
                    {
                        maxRarityMetadata = rarityMetadata;
                    }
                }
            }
            logger.LogInformation("Max\n{max}", maxRarityMetadata.ToString());
            return;
        }
        else if (pattern.ToLower().Equals("min"))
        {
            RarityMetadata minRarityMetadata = null;
            foreach (var file in files)
            {
                if (Serializer.DeserializeJson<RarityMetadata>(file, out var rarityMetadata))
                {
                    if (minRarityMetadata is null || minRarityMetadata.Rarity > rarityMetadata.Rarity)
                    {
                        minRarityMetadata = rarityMetadata;
                    }
                }
            }
            logger.LogInformation("Min\n{min}", minRarityMetadata.ToString());
            return;
        }
        var splits = pattern.Split('|');
        if (splits.Length <= 0)
        {
            logger.LogWarning("No pattern provided");
            return;
        }
        var layers = new (int, int)[splits.Length];

        for (var i = 0; i < splits.Length; i++)
        {
            var split = splits[i];
            var elem = split.Split(",");
            if (!int.TryParse(elem[0], out var first) || !int.TryParse(elem[1], out var second))
            {
                logger.LogWarning("Unable to parse integers in pattern");
                return;
            }
            if (first <= 0 || first >= filesystem.Layers.Count)
            {
                logger.LogError("Wrong query parameters");
                return;
            }
            layers[i] = (first, second);
        }

        foreach (var file in files)
        {
            if (Serializer.DeserializeJson<RarityMetadata>(file, out var rarityMetadata))
            {
                var isMatch = true;
                foreach (var pair in layers)
                {
                    //logger.LogInformation("{1}, {2}", pair.Item1, pair.Item2);
                    if (rarityMetadata.Hash[pair.Item1] != pair.Item2)
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    matches.Add(rarityMetadata);
                }
            }
        }
        logger.LogInformation("Found {amount} matches", matches.Count);

        if (handler.HasFlag("/p"))
        {
            matches.Sort((m1, m2) => m1.Id.CompareTo(m2.Id));
            var builder = new StringBuilder();
            foreach (var pair in matches)
            {
                builder.Append($"Id: {pair.Id}{Environment.NewLine}");
            }
            logger.LogInformation(builder.ToString());
        }
    }

    public static void PurgeRecursive(string path, ILogger logger = null)
    {
        var amount = 0;
        var dInfo = new DirectoryInfo(path);
        foreach (var file in dInfo.EnumerateFiles())
        {
            amount++;
            file.Delete();
        }
        logger?.LogInformation($"Deleted {amount} files");
        amount = 0;
        foreach (var dir in dInfo.EnumerateDirectories())
        {
            amount++;
            dir.Delete(true);
        }
        logger?.LogInformation($"Deleted {amount} directories");
    }
}