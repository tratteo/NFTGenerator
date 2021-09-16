using System;

namespace NFTGenerator
{
    internal static class Logger
    {
        public static void LogInfo(string log = "", ConsoleColor color = ConsoleColor.White, bool newLine = true)
        {
            Console.ForegroundColor = color;
            if (newLine)
            {
                Console.WriteLine(log);
            }
            else
            {
                Console.Write(log);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void LogWarning(string log, bool newLine = true)
        {
            LogInfo("W: " + log, ConsoleColor.DarkYellow, newLine);
        }

        public static void LogError(string log, bool newLine = true)
        {
            LogInfo("E: " + log, ConsoleColor.Red, newLine);
        }
    }
}