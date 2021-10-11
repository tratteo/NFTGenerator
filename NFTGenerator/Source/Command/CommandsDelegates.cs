// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> CommandsDelegates.cs
//
// All Rights Reserved

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NFTGenerator
{
    internal static class CommandsDelegates
    {
        public static void VerifyCMD(Program.Context pctx, Command.Context ctx, Logger logger)
        {
            string cleanArg = ctx.GetArg("clean");
            bool clean = cleanArg.Equals("clean");
            string arg = ctx.GetArg("path");
            switch (arg)
            {
                case "res":
                    bool valid = true;
                    string[] assets = Directory.GetFiles(Configurator.Options.ResultsPath, "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
                    if (assets.Length <= 0)
                    {
                        logger.LogWarning("There is nothing in here");
                        return;
                    }
                    string extension = string.Empty;
                    foreach (string asset in assets)
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
                        string[] metadata = Directory.GetFiles(Configurator.Options.ResultsPath, "*.json");

                        if (assets.Length == metadata.Length && metadata.Length == 0)
                        {
                            logger.LogWarning("There is nothing in here");
                            return;
                        }
                        if (assets.Length != metadata.Length)
                        {
                            logger.LogError("There are different numbers of assets and metadata. How the fuck did you manage to do such a thing"); return;
                            valid = false;
                        }
                        foreach (string data in metadata)
                        {
                            NFTMetadata nftData = Json.Deserialize<NFTMetadata>(data);
                            if (!nftData.Valid(logger))
                            {
                                logger.LogError("Errors on metadata: " + data);
                                logger.LogInfo("\n");
                                valid = false;
                            }
                        }
                    }
                    if (valid)
                    {
                        logger.LogInfo("All good in the results folder");
                    }
                    break;

                case "fs":
                    pctx.Filesystem.Verify(true, clean);
                    break;
            }
        }

        public static void OpenPathCMD(Program.Context pctx, Command.Context ctx, Logger logger)
        {
            string path = ctx.GetArg("path");
            switch (path)
            {
                case "fs":
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath);
                    break;

                case "res": Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath); break;

                case "layers":
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.FilesystemPath + "\\layers"); break;

                case "config":
                    using (Process fileopener = new())
                    {
                        fileopener.StartInfo.FileName = "explorer"; fileopener.StartInfo.Arguments = Configurator.OPTIONS_PATH + Configurator.OPTIONS_NAME; fileopener.Start();
                    }
                    break;

                case "root": Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory); break;

                default:
                    logger.LogWarning("Unable to find path");
                    break;
            }
        }

        public static void CreateFilesystemSchemaCMD(Program.Context pctx, Command.Context ctx, Logger logger)
        {
            int layersNumber = 0, assetsNumber = 0;
            try
            {
                layersNumber = int.Parse(ctx.GetArg("layers_n"));
                assetsNumber = int.Parse(ctx.GetArg("assets_n"));
            }
            catch (Exception e)
            {
                logger.LogError("Arguments must be integers");
                return;
            }
            for (int i = 0; i < layersNumber; i++)
            {
                string layerName = "layer_" + i;
                for (int j = 0; j < assetsNumber; j++)
                {
                    string assetName = Configurator.Options.FilesystemPath + "\\layers\\" + layerName + "\\asset_" + j;
                    Directory.CreateDirectory(assetName);
                    Json.Serialize(Json.Deserialize<AssetMetadata>(AssetMetadata.SCHEMA), assetName + "\\" + j.ToString() + ".json");
                }
            }
        }

        public static void PurgePathCMD(Program.Context pctx, Command.Context ctx, Logger logger)
        {
            string path = ctx.GetArg("path");
            string force = ctx.GetArg("force");
            string answer;
            DirectoryInfo dInfo;
            switch (path)
            {
                case "res":
                    if (force != string.Empty || force == "-f")
                    {
                        int amount = 0;
                        DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath);
                        foreach (FileInfo file in di.GetFiles())
                        {
                            amount++;
                            file.Delete();
                        }
                        logger.LogInfo("Deleted " + amount + " files");
                        amount = 0;
                        foreach (DirectoryInfo dir in di.GetDirectories())
                        {
                            amount++;
                            dir.Delete(true);
                        }
                        logger.LogInfo("Deleted " + amount + " directories");
                    }
                    else
                    {
                        logger.LogInfo("Are you sure you want to purge results folder? (Y/N)", ConsoleColor.DarkGreen);
                        answer = Console.ReadLine();
                        if (answer.ToLower().Equals("y"))
                        {
                            int amount = 0;
                            dInfo = new(AppDomain.CurrentDomain.BaseDirectory + Configurator.Options.ResultsPath);
                            foreach (FileInfo file in dInfo.EnumerateFiles())
                            {
                                amount++;
                                file.Delete();
                            }
                            logger.LogInfo("Deleted " + amount + " files");
                            amount = 0;
                            foreach (DirectoryInfo dir in dInfo.EnumerateDirectories())
                            {
                                amount++;
                                dir.Delete(true);
                            }
                            logger.LogInfo("Deleted " + amount + " directories");
                        }
                    }
                    break;

                case "layers":

                    logger.LogInfo("Are you sure you want to purge layers? (Y/N)", ConsoleColor.DarkGreen);
                    answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        dInfo = new(Configurator.Options.FilesystemPath + "\\layers");
                        foreach (FileInfo file in dInfo.EnumerateFiles())
                        {
                            file.Delete();
                        }
                        foreach (DirectoryInfo dir in dInfo.EnumerateDirectories())
                        {
                            dir.Delete(true);
                        }

                        logger.LogInfo("Purged layers");
                    }
                    break;
            }
        }
    }
}