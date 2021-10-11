// Copyright (c) Matteo Beltrame
//
// NFTGenerator -> Command.cs
//
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator
{
    public class Command : IEquatable<Command>
    {
        public Action<Context> Delegate;
        public Action<Context> Helper;
        public List<string> argumentsKeys;

        public string Key { get; private set; }

        public string Description { get; init; }

        private Command()
        {
            argumentsKeys = new List<string>();
        }

        public static Builder Literal(string key, string description, Action<Context> Delegate, Action<Context> Helper = null) => new Builder(key, Delegate, description, Helper);

        public static implicit operator Command(Builder b) => b.Build();

        public bool Match(string key)
        {
            return key.Equals(this.Key);
        }

        public void Execute(string[] arguments, Logger logger)
        {
            List<string> argsList = arguments.ToList();
            Dictionary<string, string> args = new();
            for (int i = 0; i < argsList.Count && i < argumentsKeys.Count; i++)
            {
                if (!arguments[i].Equals("-h"))
                {
                    args.Add(argumentsKeys[i], arguments[i]);
                }
            }

            int index = argsList.FindIndex(a => a.Equals("-h"));
            if (index != -1)
            {
                logger.LogInfo("[" + Description + "]");
                Helper?.Invoke(new Context(args));
            }
            else
            {
                if (arguments.Length > argumentsKeys.Count)
                {
                    logger.LogError("Too many arguments");
                    return;
                }

                Delegate?.Invoke(new Context(args));
            }
        }

        public override string ToString()
        {
            return Key + ": " + Description;
        }

        public bool Equals(Command other)
        {
            return Key.Equals(other.Key);
        }

        public class Context
        {
            private readonly Dictionary<string, string> arguments;

            public Context(Dictionary<string, string> dic)
            {
                arguments = dic;
            }

            public string GetArg(string name)
            {
                if (arguments.TryGetValue(name, out string value))
                {
                    return value;
                }
                return string.Empty;
            }
        }

        public class Builder
        {
            private readonly Command command;

            public Builder(string key, Action<Context> Delegate, string descprition, Action<Context> Helper)
            {
                command = new Command
                {
                    Key = key,
                    Description = descprition,
                    Delegate = Delegate,
                    Helper = Helper
                };
            }

            public Command Build() => command;

            public Builder ArgsKeys(params string[] args)
            {
                foreach (string arg in args)
                {
                    if (!command.argumentsKeys.Contains(arg))
                    {
                        command.argumentsKeys.Add(arg);
                    }
                }
                return this;
            }
        }
    }
}