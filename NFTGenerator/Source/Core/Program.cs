// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Program.cs
//
// All Rights Reserved

using System;
using System.Threading;

namespace NFTGenerator
{
    internal class Program
    {
        private static Context context;
        private static Logger logger;

        private static void CliRun()
        {
            string command = string.Empty;
            logger.LogInfo("\n");
            logger.LogInfo("CLI (Command line interface) - ", ConsoleColor.DarkCyan, false);
            logger.LogInfo("help", ConsoleColor.Green, false);
            logger.LogInfo(" for the available commands", ConsoleColor.DarkCyan);
            while (!command.Equals("exit"))
            {
                logger.LogInfo("\n");
                logger.LogInfo("> ", ConsoleColor.DarkCyan, false);
                command = Console.ReadLine();
                if (!command.Equals("exit"))
                {
                    context.CommandsDispatcher.Process(command);
                }
            }
        }

        private static void RegisterCommands()
        {
            context.CommandsDispatcher.Register(Commands.OPEN_PATH);
            context.CommandsDispatcher.Register(Commands.HELP);
            context.CommandsDispatcher.Register(Commands.GENERATE);
            context.CommandsDispatcher.Register(Commands.VERIFY);
            context.CommandsDispatcher.Register(Commands.PURGE);
            context.CommandsDispatcher.Register(Commands.CALC);
            context.CommandsDispatcher.Register(Commands.CREATE_FS);
        }

        private static void Initialize()
        {
            Dependencies.Resolve(logger);
            Configurator.Load(logger);
            Filesystem filesystem = new(logger);
            Generator generator = new(filesystem, logger);
            CommandsDispatcher commandsDispatcher = new(logger);

            context = new Context()
            {
                Filesystem = filesystem,
                CommandsDispatcher = commandsDispatcher,
                Generator = generator
            };
            context.Filesystem.Verify(false);
            RegisterCommands();
        }

        private static void Main(string[] args)
        {
            Console.Title = "NFTGenerator";
            logger = new Logger();
            logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);
            Initialize();

            Thread cliThread = new Thread(new ThreadStart(CliRun));
            cliThread.Start();
            cliThread.Join();
            return;
        }

        internal static class Commands
        {
            public static Command OPEN_PATH = Command.Literal(
                "open",
                "Opens a path in the explorer",
                (ctx) => CommandsDelegates.OpenPathCMD(context, ctx, logger),
                (ctx) =>
                {
                    logger.LogInfo("\tfs: opens the file system folder");
                    logger.LogInfo("\tlayers: opens the layers folder");
                    logger.LogInfo("\tres: opens the results folder");
                    logger.LogInfo("\tconfig: opens the config file");
                    logger.LogInfo("\troot: opens the root application folder");
                })
                .ArgsKeys("path");

            public static Command HELP = Command.Literal(
                "help",
                "Display available commands",
                (ctx) =>
                {
                    context.CommandsDispatcher.ForEachCommand(c =>
                    {
                        logger.LogInfo("\n" + c.Key + " [" + c.Description + "]");
                        c.Helper?.Invoke(ctx);
                    });
                });

            public static Command GENERATE = Command.Literal(
                "generate",
                "Start the generation process",
                (ctx) =>
                {
                    if (context.Filesystem.Verify(false))
                    {
                        PURGE.Execute(new string[] { "res", "-f" }, logger);
                        context.Generator.ResetGenerationParameters();
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
                            for (int j = 0; j < amountToMint; j++)
                            {
                                context.Generator.GenerateSingle(j);
                            }
                        }
                    }
                });

            public static Command VERIFY = Command.Literal(
                "verify",
                "Verify a path",
                (ctx) => CommandsDelegates.VerifyCMD(context, ctx, logger),
                (ctx) =>
                {
                    logger.LogInfo("\tfs: verify file system");
                    logger.LogInfo("\tres: verify the results");
                    logger.LogInfo("\t\tclean: also delete all invalid assets");
                })
                .ArgsKeys("path", "clean");

            public static Command PURGE = Command.Literal(
                "purge",
                "Clears all items inside a path",
                (ctx) => CommandsDelegates.PurgePathCMD(context, ctx, logger),
                (ctx) =>
                {
                    logger.LogInfo("\tres: clear the results folder");
                    logger.LogInfo("\tlayers: clear the layers folder");
                })
                .ArgsKeys("path", "force");

            public static Command CREATE_FS = Command.Literal(
                "create-fs",
                "Create a schema for a filesystem",
                (ctx) => CommandsDelegates.CreateFilesystemSchemaCMD(context, ctx, logger),
                (ctx) =>
                {
                    logger.LogInfo("\tlayer_n: number of layers");
                    logger.LogInfo("\t\tassets_n: number of assets each layer");
                })
                .ArgsKeys("layers_n", "assets_n");

            public static Command CALC = Command.Literal(
                "calc",
                "Calculate some math regarded the filesystem",
                (ctx) =>
                {
                    int perms = 1;
                    if (context.Filesystem.Layers.Count == 0) perms = 0;
                    foreach (Layer layer in context.Filesystem.Layers)
                    {
                        if (layer.Assets.Count > 0)
                        {
                            perms *= layer.Assets.Count;
                        }
                    }
                    logger.LogInfo("Permutations: " + perms);
                },
                (ctx) =>
                {
                    logger.LogInfo("\tperms: calculate the mathematical permutations number with the current correct assets in the filesystem");
                })
                .ArgsKeys("param");
        }

        internal class Context
        {
            public Generator Generator { get; init; }

            public Filesystem Filesystem { get; init; }

            public CommandsDispatcher CommandsDispatcher { get; init; }
        }
    }
}