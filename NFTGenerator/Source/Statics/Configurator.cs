// Copyright Matteo Beltrame

using GibNet.Logging;
using GibNet.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NFTGenerator;

internal static class Configurator
{
    public const string OPTIONS_NAME = "options.json";

    private static FileSystemWatcher CONFIGWATCHER;

    public static Options Options { get; private set; }

    public static void Load(Logger logger)
    {
        if (!File.Exists(Paths.CONFIG_PATH + OPTIONS_NAME))
        {
            logger.LogWarning("Options file not found, creating a new one in " + Paths.CONFIG_PATH + OPTIONS_NAME);
            Options = new Options();
            Serializer.SerializeJson(Options, Paths.CONFIG_PATH, OPTIONS_NAME);
        }
        else
        {
            Options = Serializer.DeserializeJson<Options>(Paths.CONFIG_PATH, OPTIONS_NAME);
        }
        logger.LogInfo("Loading configuration file and setting up config watcher...");
        CONFIGWATCHER = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory + Paths.CONFIG_PATH)
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
                    Options = Serializer.DeserializeJson<Options>(Paths.CONFIG_PATH, OPTIONS_NAME);
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