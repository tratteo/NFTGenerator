// Copyright Matteo Beltrame

using HandierCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NFTGenerator.Services;

public class CommandLineService
{
    private readonly IServiceProvider services;

    private readonly ILogger logger;

    public CommandLine Cli { get; private set; }

    public CommandLineService(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        this.services = services;
        logger = loggerFactory.CreateLogger("CLI");
        BuildCli();
    }

    public Task Run() => Cli.Run();

    private void BuildCli()
    {
        var filesystem = services.GetRequiredService<IFilesystem>();

        Cli = CommandLine.Factory().ExitOn("exit", "quit").OnUnrecognized((cmd) => logger.LogError($"{cmd} not recognized")).Build();
        Command PURGE = Command.Factory("purge")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
        .AddAsync(async (handler) => CommandsDelegates.PurgePath(handler, services, logger));

        Cli.Register(PURGE);

        Cli.Register(Command.Factory("rename-progr")
            .Description("rename all files in a folder with a progressive index")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to folder").Keyed("-pm", "pattern matching string").Keyed("-si", "start index to rename with"))
            .AddAsync(async (handler) => CommandsDelegates.RenameProgressively(handler, services, logger)));

        Cli.Register(Command.Factory("open")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path").Keyed("-n", "layer number"))
            .Description("opens a path in the explorer")
            .AddAsync(async (handler) => CommandsDelegates.OpenPath(handler, services, logger)));

        Cli.Register(Command.Factory("batch")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path"))
            .Description("prepare a batch out of a collection")
            .AddAsync(async (handler) => CommandsDelegates.PrepareBatch(handler, services, logger)));

        Cli.Register(Command.Factory("generate")
            .Description("start the generation process")
            .ArgumentsHandler(ArgumentsHandler.Factory())
            .AddAsync(async (handler) =>
            {
                await PURGE.Execute(new string[] { "res", "/f" });
                await CommandsDelegates.Generate(handler, services, logger);
            }));

        Cli.Register(Command.Factory("query")
            .Description("query a certain data from the generated results")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("match pattern").Flag("/p", "print the id of the matches"))
            .AddAsync(async (handler) => await CommandsDelegates.QueryResults(handler, services, logger)));

        Cli.Register(Command.Factory("dispositions")
            .Description("calculate the currently available dispositions")
            .ArgumentsHandler(ArgumentsHandler.Factory())
            .AddAsync(async (handler) =>
            {
                if (!filesystem.Verify()) return;
                Cli.Logger.LogInfo($"The current filesystem can yield up to {filesystem.CalculateDispositions()} dispositions");
            }));

        Cli.Register(Command.Factory("filter")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file").Positional("output").Positional("filter"))
            .AddAsync(async (handler) => CommandsDelegates.ApplyFilter(handler, services, logger)));

        Cli.Register(Command.Factory("scale-serie")
            .Description("scale the serie amount by an integer factor")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("the amount to scale"))
            .AddAsync(async (handler) => CommandsDelegates.ScaleSerie(handler, services, logger)));

        Cli.Register(Command.Factory("verify")
            .Description("verify a certain path")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify"))
            .AddAsync(async (handler) => CommandsDelegates.Verify(handler, services, logger)));

        Cli.Register(Command.Factory("help")
            .InhibitHelp()
            .Description("display the available commands")
            .ArgumentsHandler(ArgumentsHandler.Factory())
            .AddAsync(async (handler) => Cli.Logger.LogInfo(Cli.Print())));
    }
}