// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Configurator.cs
//
// All Rights Reserved

using GibNet.Logging;
using GibNet.Serialization;
using System;
using System.IO;

namespace NFTGenerator
{
    internal static class Configurator
    {
        public const string OPTIONS_PATH = "config\\";
        public const string OPTIONS_NAME = "options.json";

        private static FileSystemWatcher configWatcher;

        public static Options Options { get; private set; }

        public static void Load(Logger logger)
        {
            if (!File.Exists(OPTIONS_PATH + OPTIONS_NAME))
            {
                logger.LogWarning("Options file not found, creating a new one in " + OPTIONS_PATH + OPTIONS_NAME);
                Directory.CreateDirectory(OPTIONS_PATH);
                Options = new Options();
                Serializer.SerializeJson(Options, string.Empty, OPTIONS_PATH + OPTIONS_NAME);
            }
            else
            {
                Options = Serializer.DeserializeJson<Options>(string.Empty, OPTIONS_PATH + OPTIONS_NAME);
            }
            logger.LogInfo("Loading configuration file and setting up config watcher...");
            configWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory + OPTIONS_PATH)
            {
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter = "options.json",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            configWatcher.Changed += (sender, e) =>
            {
                Options = Serializer.DeserializeJson<Options>(string.Empty, OPTIONS_PATH + OPTIONS_NAME);
            };
        }
    }
}