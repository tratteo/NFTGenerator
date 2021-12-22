// Copyright Matteo Beltrame

using HandierCli;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NFTGenerator;

internal static class CommandsDelegates
{
    public static void Verify(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
    {
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

    public static void OpenPath(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
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

    public static void PurgePath(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
    {
        string path = handler.GetPositional(0);
        bool force = handler.HasFlag("/f");
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

    public static void ScaleSerie(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
    {
        logger.LogInfo("Are you sure you want to scale the serie number? (Y/N)", ConsoleColor.DarkYellow);
        string answer = Console.ReadLine();
        if (!answer.ToLower().Equals("y"))
        {
            return;
        }
        logger.LogInfo("It will not be possible to scale it back down, consider saving a copy of your filesystem, you want to proceed? (Y/N)", ConsoleColor.DarkYellow);
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
                Configurator.EditOptions(options => options.Generation.SerieCount *= factor);
            }
            catch (Exception)
            {
                logger.LogError("Unable to parse int factor");
                return;
            }
        }
    }

    public static async Task Generate(Filesystem filesystem, ArgumentsHandler handler, Logger logger)
    {
        if (filesystem.Verify(false))
        {
            int amountToMint = Configurator.Options.Generation.SerieCount;
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
                Generator generator = new Generator(filesystem, logger);
                int currentCount = 0;
                Stopwatch reportWatch = Stopwatch.StartNew();
                long lastReport = 0;
                Progress<int> generationProgressReporter = new Progress<int>((p) =>
                {
                    currentCount++;
                    long currentElapsed = reportWatch.ElapsedMilliseconds;
                    if (currentElapsed - lastReport > 500)
                    {
                        lastReport = currentElapsed;
                        ConsoleExtensions.ClearConsoleLine();
                        logger.LogInfo($"{currentCount / (float)amountToMint * 100F:0} %", false);
                    }
                });
                Stopwatch watch = Stopwatch.StartNew();
                logger.LogInfo("Parallelizing work...");
                watch.Restart();
                Parallel.ForEach(Enumerable.Range(0, amountToMint), new ParallelOptions() { MaxDegreeOfParallelism = 16 }, (i, token) => generator.GenerateSingle(i, generationProgressReporter));
                watch.Stop();
                await Task.Delay(250);
                ConsoleExtensions.ClearConsoleLine();
                logger.LogInfo($"Completed in {watch.ElapsedMilliseconds / 1000F:0.000} s", ConsoleColor.Green);
            }
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