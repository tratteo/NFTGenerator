// Copyright Matteo Beltrame

using GibNet;
using GibNet.Cli;
using GibNet.Logging;
using NFTGenerator;
using System;
using System.Threading.Tasks;

void RegisterCommands(CommandLine cli, Filesystem filesystem, Generator generator)
{
    Command PURGE = Command.Factory("purge")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.PurgePathCMD(filesystem, generator, handler.GetPositional(0), handler.HasFlag("/f"), cli.Logger)));

    cli.Register(Command.Factory("open")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path"))
        .Description("opens a path in the explorer")
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.OpenPathCMD(filesystem, generator, handler.GetPositional(0), cli.Logger))))
    .Register(Command.Factory("generate")
        .Description("start the generation process")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) =>
        {
            await Task.Run(() =>
            {
                if (filesystem.Verify(false))
                {
                    PURGE.Execute(new string[] { "res", "/f" });
                    generator.ResetGenerationParameters();
                    int amountToMint = Configurator.Options.Generation.SerieCount;
                    if (amountToMint == 0)
                    {
                        cli.Logger.LogWarning("Nothing to generate, amount to mint is set to 0");
                    }
                    else if (amountToMint < 0)
                    {
                        cli.Logger.LogError("Negative amount to mint (" + amountToMint + ") in config file");
                    }
                    else
                    {
                        for (int j = 0; j < amountToMint; j++)
                        {
                            generator.GenerateSingle(j);
                        }
                    }
                }
            });
        }))
    .Register(Command.Factory("verify")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify").Flag("/c", "clean invalid assets"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.VerifyCMD(filesystem, generator, handler.GetPositional(0), handler.HasFlag("/c"), cli.Logger))))
    .Register(PURGE)
    .Register(Command.Factory("create-fs")
        .Description("create the blueprint of a filesystem")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("layers number").Positional("assets number per layer"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.CreateFilesystemSchemaCMD(filesystem, generator, handler.GetPositional(0), handler.GetPositional(1), cli.Logger))));

    cli.Register(Command.Factory("help")
        .Description("display the available commands")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) => cli.Logger.LogInfo(cli.Print())));
}

Console.Title = "NFTGenerator";
CommandLine cli = CommandLine.Factory().ExitOn("exit", "quit").Build();
cli.Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

Dependencies.Resolve(cli.Logger);
Configurator.Load(cli.Logger);
Filesystem filesystem = new(cli.Logger);
Generator generator = new(filesystem, cli.Logger);

filesystem.Verify(false);
cli.Logger.LogInfo("\n");
RegisterCommands(cli, filesystem, generator);
await cli.Run();