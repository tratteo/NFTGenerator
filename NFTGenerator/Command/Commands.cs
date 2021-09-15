using NFTGenerator.JsonObjects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFTGenerator
{
    internal static class Delegates
    {
        public static void VerifyCMD(Program.Context pctx, Command.Context ctx)
        {
            string arg = ctx.GetArg("path");
            switch (arg)
            {
                case "res":
                    string[] assets = Directory.GetFiles(Configurator.GetSetting<string>(Configurator.RESULTS_PATH), "*.*").Where(s => s.EndsWith(".gif") || s.EndsWith(".jpeg") || s.EndsWith(".png")).ToArray();
                    string extension = string.Empty;
                    foreach (string asset in assets)
                    {
                        FileInfo file = new(asset);
                        if (!extension.Equals(string.Empty) && !extension.Equals(file.Extension))
                        {
                            Logger.LogError("Found results with different extensions! This should never happen WTF");
                            return;
                        }
                    }

                    string[] metadata = Directory.GetFiles(Configurator.GetSetting<string>(Configurator.RESULTS_PATH), "*.json");

                    if (assets.Length == metadata.Length && metadata.Length == 0)
                    {
                        Logger.LogWarning("There is nothing in here");
                        return;
                    }
                    if (assets.Length != metadata.Length)
                    {
                        Logger.LogError("There are different numbers of assets and metadata. How the fuck did you manage to do such a thing");
                        return;
                    }
                    foreach (string data in metadata)
                    {
                        NFTMetadata nftData = Json.Deserialize<NFTMetadata>(data);

                        if (!nftData.Valid())
                        {
                            Logger.LogError("Errors on metadata: " + data);
                            Logger.LogInfo();
                        }
                    }
                    break;

                case "fs":
                    pctx.Filesystem.Verify();
                    break;
            }
        }

        public static void OpenPathCMD(Program.Context pctx, Command.Context ctx)
        {
            string path = ctx.GetArg("path");
            switch (path)
            {
                case "fs":
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH));
                    break;

                case "res":
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.RESULTS_PATH));
                    break;

                case "layers":
                    Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH) + "\\layers");
                    break;

                case "config":
                    using (Process fileopener = new())
                    {
                        fileopener.StartInfo.FileName = "explorer";
                        fileopener.StartInfo.Arguments = "\"" + ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath + "\"";
                        fileopener.Start();
                    }
                    break;
            }
        }

        public static void PurgePathCMD(Program.Context pctx, Command.Context ctx)
        {
            string path = ctx.GetArg("path");
            switch (path)
            {
                case "res":
                    Logger.LogInfo("Are you sure you want to purge results folder? (Y/N)", ConsoleColor.DarkGreen);
                    string answer = Console.ReadLine();
                    if (answer.ToLower().Equals("y"))
                    {
                        int amount = 0;
                        DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.RESULTS_PATH));
                        foreach (FileInfo file in di.GetFiles())
                        {
                            amount++;
                            file.Delete();
                        }
                        Logger.LogInfo("Deleted " + amount + " files");
                        amount = 0;
                        foreach (DirectoryInfo dir in di.GetDirectories())
                        {
                            amount++;
                            dir.Delete(true);
                        }
                        Logger.LogInfo("Deleted " + amount + " directories");
                    }
                    break;
            }
        }
    }
}