// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator;
using System;

void RegisterCommands(CommandLine cli, Filesystem filesystem)
{
    Command PURGE = Command.Factory("purge")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
        .AddAsync(async (handler) => CommandsDelegates.PurgePath(filesystem, handler, cli.Logger));

    cli.Register(PURGE);

    cli.Register(Command.Factory("open")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path").Keyed("-n", "layer number"))
        .Description("opens a path in the explorer")
        .AddAsync(async (handler) => CommandsDelegates.OpenPath(filesystem, handler, cli.Logger)));

    cli.Register(Command.Factory("generate")
        .Description("start the generation process")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) =>
        {
            await PURGE.Execute(new string[] { "res", "/f" });
            await CommandsDelegates.Generate(filesystem, handler, cli.Logger);
        }));

    cli.Register(Command.Factory("dispositions")
        .Description("calculate the currently available dispositions")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) =>
        {
            if (!filesystem.Verify(false)) return;
            cli.Logger.LogInfo($"The current filesystem can yield up to {filesystem.CalculateDispositions()} dispositions");
        }));

    cli.Register(Command.Factory("scale-serie")
        .Description("scale the serie amount by an integer factor")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("the amount to scale"))
        .AddAsync(async (handler) => CommandsDelegates.ScaleSerie(filesystem, handler, cli.Logger)));

    cli.Register(Command.Factory("verify")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify"))
        .AddAsync(async (handler) => CommandsDelegates.Verify(filesystem, handler, cli.Logger)));

    cli.Register(Command.Factory("help")
        .InhibitHelp()
        .Description("display the available commands")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) => cli.Logger.LogInfo(cli.Print())));
}

Console.Title = "NFTGenerator";
CommandLine cli = CommandLine.Factory().ExitOn("exit", "quit").OnUnrecognized((cmd) => Logger.ConsoleInstance.LogError($"{cmd} not recognized")).Build();
cli.Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

Configurator.Load(cli.Logger);
Filesystem filesystem = new(cli.Logger);

cli.Logger.LogInfo("\n");
RegisterCommands(cli, filesystem);
await cli.Run();