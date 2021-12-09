// Copyright Matteo Beltrame

using HandierCli;
using NFTGenerator;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

void RegisterCommands(CommandLine cli, Filesystem filesystem)
{
    Command PURGE = Command.Factory("purge")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.PurgePathCMD(filesystem, handler.GetPositional(0), handler.HasFlag("/f"), cli.Logger)));

    cli.Register(Command.Factory("open")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path"))
        .Description("opens a path in the explorer")
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.OpenPathCMD(filesystem, handler.GetPositional(0), cli.Logger))))
    .Register(Command.Factory("generate")
        .Description("start the generation process")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) =>
        {
            if (filesystem.Verify(false))
            {
                await PURGE.Execute(new string[] { "res", "/f" });
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
                    Generator generator = new Generator(filesystem, cli.Logger);
                    int currentCount = 0;
                    Progress<int> generationProgressReporter = new Progress<int>((p) =>
                    {
                        currentCount++;
                        ConsoleExtensions.ClearConsoleLine();
                        cli.Logger.LogInfo($"{currentCount / (float)amountToMint * 100F:0} %", false);
                    });
                    Stopwatch watch = Stopwatch.StartNew();
                    cli.Logger.LogInfo("Generating NFTs...");
                    cli.Logger.LogInfo($"0 %", false);
                    watch.Restart();
                    Parallel.ForEach(Enumerable.Range(0, amountToMint), new ParallelOptions() { MaxDegreeOfParallelism = 64 }, (i, token) => generator.GenerateSingle(i, generationProgressReporter));
                    watch.Stop();
                    await Task.Delay(100);
                    ConsoleExtensions.ClearConsoleLine();
                    cli.Logger.LogInfo($"Completed in {watch.ElapsedMilliseconds / 1000F:0.000} s", ConsoleColor.Green);
                }
            }
        }))
    .Register(Command.Factory("verify")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.VerifyCMD(filesystem, handler.GetPositional(0), cli.Logger))))
    .Register(PURGE)
    .Register(Command.Factory("create-fs")
        .Description("create the blueprint of a filesystem")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("layers number").Positional("assets number per layer"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.CreateFilesystemSchemaCMD(filesystem, handler.GetPositional(0), handler.GetPositional(1), cli.Logger))));
    cli.Register(Command.Factory("help")
        .InhibitHelp()
        .Description("display the available commands")
        .ArgumentsHandler(ArgumentsHandler.Factory())
        .AddAsync(async (handler) => cli.Logger.LogInfo(cli.Print())));
}

Console.Title = "NFTGenerator";
CommandLine cli = CommandLine.Factory().ExitOn("exit", "quit").OnUnrecognized((cmd) => Logger.ConsoleInstance.LogError($"{cmd} not recognized")).Build();
cli.Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

//Dependencies.Resolve(cli.Logger);
Configurator.Load(cli.Logger);
Filesystem filesystem = new(cli.Logger);

filesystem.Verify(false);
cli.Logger.LogInfo("\n");
RegisterCommands(cli, filesystem);
await cli.Run();