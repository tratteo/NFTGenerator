using System;
using System.Threading;

namespace NFTGenerator
{
    internal class Program
    {
        private static Context context;

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
                context.CommandsDispatcher.Process(command);
            }
        }

        private static void RegisterCommands()
        {
            context.CommandsDispatcher.Register(Commands.OPEN_PATH);
            context.CommandsDispatcher.Register(Commands.HELP);
            context.CommandsDispatcher.Register(Commands.GENERATE);
            context.CommandsDispatcher.Register(Commands.VERIFY);
            context.CommandsDispatcher.Register(Commands.PURGE);
        }

        private static void Initialize()
        {
            Dependencies.Resolve();
            Configurator.Load();

            Filesystem filesystem = new Filesystem();
            Generator generator = new Generator(filesystem);
            CommandsDispatcher commandsDispatcher = new CommandsDispatcher();

            context = new Context()
            {
                Filesystem = filesystem,
                CommandsDispatcher = commandsDispatcher,
                Generator = generator
            };
            context.Filesystem.Verify();
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
                .Then("path", (ctx) => CommandsDelegates.OpenPathCMD(context, ctx));

            public static Command HELP = Command.Literal(
                "help",
                "Display available commands",
                (ctx) =>
                {
                    context.CommandsDispatcher.ForEachCommand(c =>
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
                    if (context.Filesystem.Verify(false))
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
                                context.Generator.GenerateSingle(j);
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
                .Then("path", (ctx) => CommandsDelegates.VerifyCMD(context, ctx));

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
                .Then("path", (ctx) => CommandsDelegates.PurgePathCMD(context, ctx));
        }

        internal class Context
        {
            public Generator Generator { get; init; }

            public Filesystem Filesystem { get; init; }

            public CommandsDispatcher CommandsDispatcher { get; init; }
        }
    }
}