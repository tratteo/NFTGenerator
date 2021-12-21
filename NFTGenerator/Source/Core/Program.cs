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
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path").Keyed("-n", "layer number"))
        .Description("opens a path in the explorer")
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.OpenPathCMD(filesystem, handler, cli.Logger))))
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
                    Stopwatch reportWatch = Stopwatch.StartNew();
                    long lastReport = 0;
                    Progress<int> generationProgressReporter = new Progress<int>((p) =>
                    {
                        currentCount++;
                        long currentElapsed = reportWatch.ElapsedMilliseconds;
                        if (currentElapsed - lastReport > 200)
                        {
                            ConsoleExtensions.ClearConsoleLine();
                            cli.Logger.LogInfo($"{currentCount / (float)amountToMint * 100F:0} %", false);
                            lastReport = currentElapsed;
                        }
                    });
                    Stopwatch watch = Stopwatch.StartNew();
                    cli.Logger.LogInfo("Parallelizing work...");
                    watch.Restart();
                    Parallel.ForEach(Enumerable.Range(0, amountToMint), new ParallelOptions() { MaxDegreeOfParallelism = 16 }, (i, token) => generator.GenerateSingle(i, generationProgressReporter));
                    watch.Stop();
                    await Task.Delay(250);
                    ConsoleExtensions.ClearConsoleLine();
                    cli.Logger.LogInfo($"Completed in {watch.ElapsedMilliseconds / 1000F:0.000} s", ConsoleColor.Green);
                }
            }
        }))
    .Register(Command.Factory("scale-serie")
        .Description("scale the serie amount by an integer factor")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("the amount to scale"))
        .AddAsync(async (handler) =>
        {
            cli.Logger.LogInfo("Are you sure you want to scale the serie number? (Y/N)", ConsoleColor.DarkYellow);
            string answer = Console.ReadLine();
            if (!answer.ToLower().Equals("y"))
            {
                return;
            }
            cli.Logger.LogInfo("It will not be possible to scale it back down, consider saving a copy of your filesystem, you want to proceed? (Y/N)", ConsoleColor.DarkYellow);
            answer = Console.ReadLine();
            if (!answer.ToLower().Equals("y"))
            {
                return;
            }
            if (!filesystem.Verify())
            {
                cli.Logger.LogError("Unable to scale, filesystem contains errors");
                return;
            }
            else
            {
                int factor = 1;
                try
                {
                    factor = int.Parse(handler.GetPositional(0));
                    foreach (Layer layer in filesystem.Layers)
                    {
                        foreach (Asset asset in layer.Assets)
                        {
                            asset.Metadata.Amount *= factor;
                            Serializer.SerializeJson($"{Paths.LAYERS}{layer.Name}\\", $"{asset.Id}.json", asset.Metadata);
                        }
                    }
                    Configurator.EditOptions(options => options.Generation.SerieCount *= factor);
                }
                catch (Exception)
                {
                    cli.Logger.LogError("Unable to parse int factor");
                    return;
                }
            }
        }))
    .Register(Command.Factory("verify")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to verify"))
        .AddAsync(async (handler) => await Task.Run(() => CommandsDelegates.VerifyCMD(filesystem, handler.GetPositional(0), cli.Logger))))
    .Register(PURGE);
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
filesystem.Verify(false);

cli.Logger.LogInfo("\n");
RegisterCommands(cli, filesystem);
await cli.Run();