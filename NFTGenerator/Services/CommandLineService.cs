using BetterHaveIt;
using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace NFTGenerator.Services;

public class CommandLineService : ICoreRunner
{
    private readonly IServiceProvider services;

    private readonly ILogger logger;

    public CommandLine Cli { get; private set; }

    public CommandLineService(IServiceProvider services, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        this.services = services;
        this.logger = loggerFactory.CreateLogger("CLI");
        BuildCli();
    }

    public Task Run() => Cli.Run();

    private void BuildCli()
    {
        IFilesystem filesystem = services.GetService<IFilesystem>();

        Cli = CommandLine.Factory().ExitOn("exit", "quit").OnUnrecognized((cmd) => logger.LogError($"{cmd} not recognized")).Build();
        Command PURGE = Command.Factory("purge")
        .Description("verify a certain path")
        .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to purge").Flag("/f", "skip confirmation"))
        .AddAsync(async (handler) => CommandsDelegates.PurgePath(handler, services, logger));

        Cli.Register(PURGE);

        Cli.Register(Command.Factory("rename-progr")
            .Description("rename all files in a folder with a progressive index")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("path to folder").Keyed("-pm", "pattern matching string"))
            .AddAsync(async (handler) =>
            {
                int counter = 0;
                bool usingPattern = handler.GetKeyed("-pm", out var pattern);
                var files = usingPattern ? Directory.GetFiles(handler.GetPositional(0), pattern) : Directory.GetFiles(handler.GetPositional(0));
                var dirs = usingPattern ? Directory.GetDirectories(handler.GetPositional(0), pattern) : Directory.GetDirectories(handler.GetPositional(0));
                for (int i = 0; i < dirs.Length; i++, counter++)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(dirs[i]);
                    (string path, string name) = PathExtensions.Split(directoryInfo.FullName);
                    if (directoryInfo.Name.Equals($"{counter}")) continue;
                    directoryInfo.MoveTo($"{path}\\{counter}");
                }
                for (int i = 0; i < files.Length; i++, counter++)
                {
                    FileInfo fileInfo = new FileInfo(files[i]);
                    fileInfo.MoveTo($"{fileInfo.Directory.FullName}\\{counter}{fileInfo.Extension}", true);
                }
            }));

        Cli.Register(Command.Factory("open")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file path").Keyed("-n", "layer number"))
            .Description("opens a path in the explorer")
            .AddAsync(async (handler) => CommandsDelegates.OpenPath(handler, services, logger)));

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
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file").Positional("output"))
            .AddAsync(async (handler) =>
            {
                using Bitmap bitmap = new Bitmap(handler.GetPositional(0));
                Media.ApplyVideoDegradationFilter(bitmap);
                bitmap.Save(handler.GetPositional(1));
            }));

        Cli.Register(Command.Factory("refactor")
            .ArgumentsHandler(ArgumentsHandler.Factory().Positional("root folder"))
            .AddAsync(async (handler) =>
            {
                string[] allfiles = Directory.GetFiles(handler.GetPositional(0), "*.json", SearchOption.AllDirectories);
                foreach (string file in allfiles)
                {
                    if (Serializer.DeserializeJson<AssetMetadata>(string.Empty, file, out var meta))
                    {
                        var newMeta = new AssetMetadata()
                        { Amount = meta.Amount };
                        Serializer.SerializeJson(string.Empty, file, newMeta);
                    }
                }
            }));

        //Cli.Register(Command.Factory("compress")
        //    .ArgumentsHandler(ArgumentsHandler.Factory().Positional("file").Positional("output"))
        //    .AddAsync(async (handler) =>
        //    {
        //        var quantizer = new WuQuantizer();
        //        using var bitmap = new Bitmap(handler.GetPositional(0));
        //        using var quantized = quantizer.QuantizeImage(bitmap);
        //        quantized.Save(handler.GetPositional(1), ImageFormat.Png);
        //    }));

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