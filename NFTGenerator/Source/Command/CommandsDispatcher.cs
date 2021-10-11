// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> CommandsDispatcher.cs
//
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator
{
    internal class CommandsDispatcher
    {
        private readonly List<Command> commands;
        private readonly Logger logger;

        public CommandsDispatcher(Logger logger)
        {
            commands = new List<Command>();
            this.logger = logger;
        }

        public CommandsDispatcher Register(Command command)
        {
            if (!commands.Contains(command))
            {
                commands.Add(command);
            }
            else
            {
                logger.LogWarning("Command: [" + command.Key + "] tried to register twice\n");
            }
            return this;
        }

        public void ForEachCommand(Action<Command> Action)
        {
            foreach (Command command in commands)
            {
                Action?.Invoke(command);
            }
        }

        public void Process(string cliLine)
        {
            string[] splits = cliLine.Split(" ");
            if (splits.Length > 0)
            {
                Command cmd = commands.Find(c => c.Match(splits[0]));
                if (cmd != null)
                {
                    string[] args = splits.Skip(1).ToArray();
                    cmd.Execute(args, logger);
                }
                else
                {
                    logger.LogWarning("Command not found");
                }
            }
            else
            {
                logger.LogWarning("Command not found, weird command");
            }
        }
    }
}