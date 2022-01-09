using HandierCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nQuant;
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

        Cli = CommandLine.Factory().ExitOn("exit", "quit").OnUnrecognized((cmd) => Logger.ConsoleInstance.LogError($"{cmd} not recognized")).Build();
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
                var files = handler.GetKeyed("-pm", out var pattern)
                    ? Directory.GetFiles(handler.GetPositional(0), pattern)
                    : Directory.GetFiles(handler.GetPositional(0));
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo fileInfo = new FileInfo(files[i]);
                    fileInfo.MoveTo($"{fileInfo.Directory.FullName}\\{i}{fileInfo.Extension}");
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

        Cli.Register(Command.Factory("dispositions")
            .Description("calculate the currently available dispositions")
            .ArgumentsHandler(ArgumentsHandler.Factory())
            .AddAsync(async (handler) =>
            {
                if (!filesystem.Verify()) return;
                Cli.Logger.LogInfo($"The current filesystem can yield up to {filesystem.CalculateDispositions()} dispositions");
            }));

        Cli.Register(Command.Factory("compress")
            .ArgumentsHandler(ArgumentsHandler.Factory())
            .AddAsync(async (handler) =>
            {
                //ImageOptimizer optimizer = new ImageOptimizer();
                //var info = new FileInfo(@"C:\Users\matteo\Desktop\Cloudies\test.png");
                //optimizer.OptimalCompression = true;
                //optimizer.LosslessCompress(info);
                //info.Refresh();
                ////TODO compress
                var quantizer = new WuQuantizer();
                using var bitmap = new Bitmap(@"C:\Users\matteo\Desktop\Cloudies\ksiCloudy.png");
                using var quantized = quantizer.QuantizeImage(bitmap);
                quantized.Save(@"C:\Users\matteo\Desktop\Cloudies\test1.png", ImageFormat.Png);
                //using (MagickImage image = new MagickImage(@"C:\Users\matteo\Desktop\Cloudies\ksiCloudy.png"))
                //{
                //    image.Quality = int.MaxValue; // This is the Compression level.
                //    image.Write(@"C:\Users\matteo\Desktop\Cloudies\test1.png");
                //}
            }));

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