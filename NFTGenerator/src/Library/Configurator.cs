using System;
using System.Configuration;
using System.IO;

namespace NFTGenerator
{
    internal static class Configurator
    {
        public const string ALLOW_DUPLICATES = "allowDuplicates";
        public const string FILESYSTEM_PATH = "fileSystemPath";
        public const string RESULTS_PATH = "resultsPath";
        public const string AMOUNT_TO_MINT = "amountToMint";

        private static FileSystemWatcher configWatcher;

        public static void Load()
        {
            Logger.LogInfo("Loading configuration file and setting up config watcher...");
            configWatcher = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory)
            {
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastAccess | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter = "*.config",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            configWatcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                ConfigurationManager.RefreshSection("appSettings");
                ConfigurationManager.RefreshSection("configuration");
            };
        }

        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            string val = ConfigurationManager.AppSettings[key] ?? "";
            T result = defaultValue;
            if (!string.IsNullOrEmpty(val))
            {
                T typeDefault = default(T);
                if (typeof(T) == typeof(string))
                {
                    typeDefault = (T)(object)string.Empty;
                }
                result = (T)Convert.ChangeType(val, typeDefault.GetType());
            }
            return result;
        }
    }
}