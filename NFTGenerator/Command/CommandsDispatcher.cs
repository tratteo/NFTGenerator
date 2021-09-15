using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator
{
    internal class CommandsDispatcher
    {
        private readonly List<Command> commands;

        public CommandsDispatcher()
        {
            commands = new List<Command>();
        }

        public CommandsDispatcher Register(Command command)
        {
            if (!commands.Contains(command))
            {
                commands.Add(command);
            }
            else
            {
                Logger.LogWarning("Command: [" + command.Key + "] tried to register twice\n");
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
                    cmd.Execute(args);
                }
                else
                {
                    Logger.LogWarning("Command not found");
                }
            }
            else
            {
                Logger.LogWarning("Command not found, weird command");
            }
        }
    }
}