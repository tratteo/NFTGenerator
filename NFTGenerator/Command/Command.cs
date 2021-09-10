using System;
using System.Collections.Generic;

namespace NFTGenerator
{
    public class Command : IEquatable<Command>
    {
        public List<Action<Context>> Delegates;

        public List<string> argumentsNames;

        public string Key { get; private set; }

        public string Description { get; init; }

        private Command()
        {
            Delegates = new List<Action<Context>>();
            argumentsNames = new List<string>();
        }

        public static Builder Literal(string key, string description, Action<Context> Delegate) => new Builder(key, Delegate, description);

        public static implicit operator Command(Builder b) => b.Build();

        public bool Match(string key)
        {
            return key.Equals(this.Key);
        }

        public void Execute(string[] arguments)
        {
            if (arguments.Length > argumentsNames.Count)
            {
                Logger.LogError("Too many arguments");
                return;
            }

            Action<Context> del = Delegates[arguments.Length];
            Dictionary<string, string> args = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i++)
            {
                args.Add(argumentsNames[i], arguments[i]);
            }
            del?.Invoke(new Context(args));
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
            private Command command;

            public Builder(string key, Action<Context> Delegate, string descprition)
            {
                command = new Command
                {
                    Key = key,
                    Description = descprition,
                };
                command.Delegates.Add(Delegate);
            }

            public Command Build() => command;

            public Builder Then(string argumentName, Action<Context> Delegate)
            {
                command.Delegates.Add(Delegate);
                command.argumentsNames.Add(argumentName);
                return this;
            }
        }
    }
}