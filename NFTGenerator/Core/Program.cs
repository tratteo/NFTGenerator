using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace NFTGenerator
{
    internal class Program
    {
        private static Generator generator;
        private static Filesystem filesystem;
        private static List<Command> registeredCommands;

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
                Command cmd = registeredCommands.Find(c => c.Key.Equals(command));
                if (cmd != null)
                {
                    cmd.Delegate?.Invoke();
                }
                else
                {
                    Logger.LogWarning("Command not found");
                }
            }
        }

        private static void RegisterCommands()
        {
            registeredCommands ??= new List<Command>();
            registeredCommands.AddRange(new Command[]
            {
                new Command()
                {
                    Key = "help",
                    Delegate = ()=>
                    {
                        foreach(Command cmd in registeredCommands)
                        {
                            Logger.LogInfo(" - ", ConsoleColor.White, false);
                            Logger.LogInfo(cmd.Key+": ", ConsoleColor.Green, false);
                            Logger.LogInfo(cmd.Description);
                        }
                    }
                },
                new Command()
                {
                    Key = "exit",
                    Description = "exit the application",
                    Delegate = () => { return; }
                },
                new Command()
                {
                    Key = "generate",
                    Description="start the generation process",
                    Delegate = ()=>
                    {
                        if(filesystem.Verify(false))
                        {
                            Generate();
                        }
                    }
                },
                new Command()
                {
                    Key = "config",
                    Description="opens the application configuration file",
                    Delegate = ()=>
                    {
                        using Process fileopener = new();

                        fileopener.StartInfo.FileName = "explorer";
                        fileopener.StartInfo.Arguments = "\"" + ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath + "\"";
                        fileopener.Start();
                    }
                },
                new Command()
                {
                    Key = "verifyfs",
                    Description="verify the layout of the filesystem. In case, it creates a new filesystem base layout",
                    Delegate = ()=>
                    {
                        filesystem.Verify();
                    }
                },
                new Command()
                {
                    Key = "openfs",
                    Description="opens the filesystem folder in the explorer",
                    Delegate = ()=>
                    {
                        Process.Start("explorer.exe" , AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH));
                    }
                },
                new Command()
                {
                    Key = "openres",
                    Description="opens the results folder in the explorer",
                    Delegate = ()=>
                    {
                        Process.Start("explorer.exe" , AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.RESULTS_PATH));
                    }
                },
                new Command()
                {
                    Key = "openlayers",
                    Description="opens the layers folder in the explorer",
                    Delegate = ()=>
                    {
                        Process.Start("explorer.exe" , AppDomain.CurrentDomain.BaseDirectory + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH)+"\\layers");
                    }
                }
            });
        }

        private static void Main(string[] args)
        {
            Console.Title = "NFTGenerator";
            Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);
            DependenciesHandler.Resolve();
            Configurator.LoadConf();
            filesystem = new Filesystem();
            generator = new Generator(filesystem);
            filesystem.Verify();
            RegisterCommands();
            Thread cliThread = new Thread(new ThreadStart(CliRun));
            cliThread.Start();
            cliThread.Join();
            return;
        }

        private static void Generate()
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

        private class Command
        {
            public string Key { get; init; }

            public string Description { get; init; }

            public Action Delegate { get; init; }

            public override string ToString()
            {
                return Key + ": " + Description;
            }
        }
    }
}