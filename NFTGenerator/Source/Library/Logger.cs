// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Logger.cs
//
// All Rights Reserved

using Microsoft.Extensions.Logging;
using System;

namespace NFTGenerator
{
    public class Logger : ILogger
    {
        public static bool Enabled { get; set; } = true;

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => Enabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    LogInfo(formatter(state, exception), true);
                    break;

                case LogLevel.Warning:
                    LogWarning(formatter(state, exception), true);
                    break;

                case LogLevel.Error:
                    LogError(formatter(state, exception), true);
                    break;

                default:
                    LogInfo(formatter(state, exception), true);
                    break;
            }
        }

        public void LogInfo(string log, bool newLine = true)
        {
            if (newLine)
            {
                Console.WriteLine(log);
            }
            else
            {
                Console.Write(log);
            }
        }

        public void LogInfo(string log, ConsoleColor color, bool newLine = true)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (newLine)
            {
                Console.WriteLine(log);
            }
            else
            {
                Console.Write(log);
            }
            Console.ForegroundColor = currentColor;
        }

        public void LogWarning(string log, bool newLine = true)
        {
            LogInfo("W: " + log, ConsoleColor.DarkYellow, newLine);
        }

        public void LogError(string log, bool newLine = true)
        {
            LogInfo("E: " + log, ConsoleColor.Red, newLine);
        }
    }
}