// Copyright Matteo Beltrame

using HandierCli;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NFTGenerator;

internal static class Configurator
{
    public static readonly string OPTIONS_NAME = "options.json";

    private static FileSystemWatcher CONFIGWATCHER;

    public static Options Options { get; private set; }

    public static void EditOptions(Action<Options> action)
    {
        action?.Invoke(Options);
        Serializer.SerializeJson(Paths.CONFIG, OPTIONS_NAME, Options);
    }

    public static void Load(Logger logger)
    {
        if (!File.Exists($"{Paths.CONFIG}{OPTIONS_NAME}"))
        {
            logger.LogWarning("Options file not found, creating a new one in " + Paths.CONFIG + OPTIONS_NAME);
            Options = new Options();
            Serializer.SerializeJson(Paths.CONFIG, OPTIONS_NAME, Options);
        }
        else
        {
            if (Serializer.DeserializeJson<Options>(Paths.CONFIG, OPTIONS_NAME, out var options))
            {
                Options = options;
            }
        }
        logger.LogInfo("Loading configuration file and setting up config watcher...");
        CONFIGWATCHER = new FileSystemWatcher(Paths.CONFIG)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = OPTIONS_NAME,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };
        CONFIGWATCHER.Changed += async (sender, e) =>
        {
            CONFIGWATCHER.EnableRaisingEvents = false;
            int tries = 10;
            int attemptDelay = 50;
            for (int i = 0; i <= tries; ++i)
            {
                try
                {
                    if (Serializer.DeserializeJson<Options>(Paths.CONFIG, OPTIONS_NAME, out var options))
                    {
                        Options = options;
                    }
                    break;
                }
                catch (IOException) when (i <= tries)
                {
                    await Task.Delay(attemptDelay);
                }
            }

            CONFIGWATCHER.EnableRaisingEvents = true;
        };
    }
}