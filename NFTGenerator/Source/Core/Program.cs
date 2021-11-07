// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Program.cs
//
// All Rights Reserved

using GibNet;
using GibNet.Cli;
using GibNet.Logging;
using System;
using System.Threading.Tasks;

namespace NFTGenerator
{
    internal class Program
    {
        private static Context context;
        private static Logger logger;

        private static void Initialize()
        {
            Dependencies.Resolve(logger);
            Configurator.Load(logger);
            Filesystem filesystem = new(logger);
            Generator generator = new(filesystem, logger);

            context = new Context()
            {
                Filesystem = filesystem,
                Generator = generator
            };
            context.Filesystem.Verify(false);
        }

        private static async Task Main(string[] args)
        {
            Console.Title = "NFTGenerator";
            logger = Logger.ConsoleInstance;
            logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);
            Initialize();
            logger.LogInfo("\n");
            await BuildCli().Run();
        }

        private static CommandLine BuildCli()
        {
            CommandLine cli = CommandLine.Factory(Logger.ConsoleInstance).ExitOn("exit", "quit").Build()
                .Register(Command.Factory("open")
                    .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path"))
                    .Description("opens a path in the explorer")
                    .AddAsync(async (handler) =>
                    {
                        await Task.Run(() => CommandsDelegates.OpenPathCMD(context, handler.GetPositional(0), logger));
                    }))
                .Register(Command.Factory("generate")
                    .Description("start the generation process")
                    .ArgumentsHandler(ArgumentsHandler.Factory())
                    .AddAsync(async (handler) =>
                    {
                        await Task.Run(() =>
                        {
                            if (context.Filesystem.Verify(false))
                            {
                                Commands.PURGE.Execute(new string[] { "res", "/f" });
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
                    }))
                .Register(Command.Factory("verify")
                    .Description("verify a certain path")
                    .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify").Flag("/c", "clean invalid assets"))
                    .AddAsync(async (handler) =>
                    {
                        await Task.Run(() =>
                        {
                            CommandsDelegates.VerifyCMD(context, handler.GetPositional(0), handler.HasFlag("/c"), logger);
                        });
                    }))
                .Register(Commands.PURGE)
                .Register(Command.Factory("create-fs")
                    .Description("create the blueprint of a filesystem")
                    .ArgumentsHandler(ArgumentsHandler.Factory().Positional("layers number").Positional("per layer assets number"))
                    .AddAsync(async (handler) =>
                    {
                        await Task.Run(() =>
                        {
                            CommandsDelegates.CreateFilesystemSchemaCMD(context, handler.GetPositional(0), handler.GetPositional(1), logger);
                        });
                    }));

            cli.Register(Command.Factory("help")
                .Description("display the available commands")
                .ArgumentsHandler(ArgumentsHandler.Factory())
                .AddAsync(async (handler) =>
                {
                    logger.LogInfo(cli.ToString());
                    await Task.FromResult<object>(null);
                }));
            return cli;
        }

        internal class Context
        {
            public Generator Generator { get; init; }

            public Filesystem Filesystem { get; init; }
        }

        private static class Commands
        {
            public static readonly Command PURGE = Command.Factory("purge")
                    .Description("verify a certain path")
                    .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
                    .AddAsync(async (handler) =>
                    {
                        await Task.Run(() =>
                        {
                            CommandsDelegates.PurgePathCMD(context, handler.GetPositional(0), handler.HasFlag("/f"), logger);
                        });
                    });
        }
    }
}