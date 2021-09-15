using System;
using System.Threading;

namespace NFTGenerator
{
    internal class Program
    {
        private static Context context;
        private static Generator generator;
        private static Filesystem filesystem;
        private static CommandsDispatcher commandsDispatcher;

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
                commandsDispatcher.Process(command);
            }
        }

        private static void RegisterCommands()
        {
            commandsDispatcher.Register(Commands.OPEN_PATH);
            commandsDispatcher.Register(Commands.HELP);
            commandsDispatcher.Register(Commands.GENERATE);
            commandsDispatcher.Register(Commands.VERIFY);
            commandsDispatcher.Register(Commands.PURGE);
        }

        private static void Initialize()
        {
            Dependencies.Resolve();
            Configurator.Load();
            filesystem = new Filesystem();
            filesystem.Verify();
            generator = new Generator(filesystem);
            commandsDispatcher = new CommandsDispatcher();

            context = new Context()
            {
                Filesystem = filesystem,
                CommandsDispatcher = commandsDispatcher,
                Generator = generator
            };
            RegisterCommands();
        }

        private static void Main(string[] args)
        {
            Console.Title = "NFTGenerator";
            Logger.LogInfo("----- NFT GENERATOR -----\n\n", ConsoleColor.DarkCyan);

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
                (ctx) => Logger.LogWarning("Few arguments, see usage with -h"),
                (ctx) =>
                {
                    Logger.LogInfo("Parameters:");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("fs", ConsoleColor.Green, false);
                    Logger.LogInfo(": opens the file system folder ");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("layers", ConsoleColor.Green, false);
                    Logger.LogInfo(": opens the layers folder ");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("res", ConsoleColor.Green, false);
                    Logger.LogInfo(": opens the results folder ");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("config", ConsoleColor.Green, false);
                    Logger.LogInfo(": opens the config file");
                })
                .Then("path", (ctx) => Delegates.OpenPathCMD(context, ctx));

            public static Command HELP = Command.Literal(
                "help",
                "Display available commands",
                (ctx) =>
                {
                    commandsDispatcher.ForEachCommand(c =>
                    {
                        Logger.LogInfo(" - ", ConsoleColor.White, false);
                        Logger.LogInfo(c.Key + ": ", ConsoleColor.Green, false);
                        Logger.LogInfo(c.Description);
                    });
                });

            public static Command GENERATE = Command.Literal(
                "generate",
                "Start the generation process",
                (ctx) =>
                {
                    if (filesystem.Verify(false))
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
                });

            public static Command VERIFY = Command.Literal(
                "verify",
                "Verify a path",
                (ctx) => Logger.LogWarning("Few arguments, see usage with -h"),
                (ctx) =>
                {
                    Logger.LogInfo("Parameters:");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("fs", ConsoleColor.Green);
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("res", ConsoleColor.Green);
                })
                .Then("path", (ctx) => Delegates.VerifyCMD(context, ctx));

            public static Command PURGE = Command.Literal(
                "purge",
                "Clears all items inside a path",
                (ctx) => Logger.LogWarning("Few arguments, see usage with -h"),
                (ctx) =>
                {
                    Logger.LogInfo("Parameters:");
                    Logger.LogInfo("- ", ConsoleColor.White, false);
                    Logger.LogInfo("res", ConsoleColor.Green);
                })
                .Then("path", (ctx) => Delegates.PurgePathCMD(context, ctx));
        }

        internal class Context
        {
            public Generator Generator { get; init; }

            public Filesystem Filesystem { get; init; }

            public CommandsDispatcher CommandsDispatcher { get; init; }
        }
    }
}