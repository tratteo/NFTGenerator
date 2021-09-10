using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NFTGenerator
{
    internal class Program
    {
        private static Generator generator;
        private static Filesystem filesystem;
        private static CommandsDispatcher commandsDispatcher;

        private static void CliRun()
        {
            string command = string.Empty;
            Logger.LogInfo("CLI (Command line interface) - ", ConsoleColor.DarkCyan, false);
            Logger.LogInfo("help", ConsoleColor.Green, false);
            Logger.LogInfo(" for the available commands", ConsoleColor.DarkCyan);
            while (!command.Equals("exit"))
            {
                Logger.LogInfo();
                Logger.LogInfo("> ", ConsoleColor.DarkCyan, false);
                command = Console.ReadLine();
                commandsDispatcher.Process(command);
            }
        }

        private static void RegisterCommands()
        {
            commandsDispatcher.Register(Command.Literal("open", "opens a path [fs, res, config]", (ctx) =>
            {
                Logger.LogWarning("Few arguments, usage: open [fs, res, layers, config]");
            }).Then("path", (ctx) =>
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
            }))
            .Register(Command.Literal("help", "display available commands", (ctx) =>
            {
                commandsDispatcher.ForEachCommand(c =>
                {
                    Logger.LogInfo(" - ", ConsoleColor.White, false);
                    Logger.LogInfo(c.Key + ": ", ConsoleColor.Green, false);
                    Logger.LogInfo(c.Description);
                });
            }))
            .Register(Command.Literal("generate", "start the generation process", (ctx) =>
            {
                if (filesystem.Verify(false))
                {
                    int amountToMint = Configurator.GetSetting<int>(Configurator.AMOUNT_TO_MINT);
                    if (amountToMint == 0)
                    {
                        Logger.LogWarning("Nothing to generate, amount to mint is set to 0");
                    }
                    else if (amountToMint < 0)
                    {
                        Logger.LogError("Negative amount to mint (" + amountToMint + ") in config file");
                    }
                    else
                    {
                        for (int j = 0; j < amountToMint; j++)
                        {
                            generator.GenerateSingle(j);
                        }
                    }
                }
            }))
            .Register(Command.Literal("verify", "verify the filesystem", (ctx) =>
            {
                filesystem.Verify();
            }))
            .Register(Command.Literal("purge", "clears all items inside a path [res]", (ctx) =>
            {
                Logger.LogWarning("Few arguments, usage: purge [res]");
            }).Then("path", (ctx) =>
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
            }));
        }

        private static void Initialize()
        {
            Dependencies.Resolve();
            Configurator.Load();
            filesystem = new Filesystem();
            filesystem.Verify();
            generator = new Generator(filesystem);
            commandsDispatcher = new CommandsDispatcher();
            RegisterCommands();
        }

        private static void Main(string[] args)
        {
            Console.Title = "NFTGenerator";
            Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

            Initialize();

            Thread cliThread = new Thread(new ThreadStart(CliRun));
            cliThread.Start();
            cliThread.Join();
            return;
        }
    }
}