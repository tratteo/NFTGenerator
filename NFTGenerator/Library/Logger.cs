using System;

namespace NFTGenerator
{
    internal static class Logger
    {
        public enum LogType { WARNING, INFO, ERROR }

        public static void Log(string log, LogType type = LogType.INFO)
        {
            string prefix = "";
            switch (type)
            {
                case LogType.INFO:
                    break;

                case LogType.WARNING:
                    prefix = "[WARNING]";
                    break;

                case LogType.ERROR:
                    prefix = "[ERROR]";
                    break;
            }
            Console.WriteLine(prefix + ": " + log);
        }
    }
}