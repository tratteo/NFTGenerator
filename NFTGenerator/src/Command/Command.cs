using System;
using System.Collections.Generic;

namespace NFTGenerator
{
    public class Command : IEquatable<Command>
    {
        public List<Action<Context>> Delegates;
        public List<Action<Context>> Helpers;
        public List<string> argumentsNames;

        public string Key { get; private set; }

        public string Description { get; init; }

        private Command()
        {
            Helpers = new List<Action<Context>>();
            Delegates = new List<Action<Context>>();
            argumentsNames = new List<string>();
        }

        public static Builder Literal(string key, string description, Action<Context> Delegate, Action<Context> Helper = null) => new Builder(key, Delegate, description, Helper);

        public static implicit operator Command(Builder b) => b.Build();

        public bool Match(string key)
        {
            return key.Equals(this.Key);
        }

        public void Execute(string[] arguments)
        {
            if (arguments != null && arguments.Length > 0)
            {
                if (arguments.Length > argumentsNames.Count && arguments[^1].Equals("-h"))
                {
                    Logger.LogError("Too many arguments");
                    return;
                }
            }
            Dictionary<string, string> args = new Dictionary<string, string>();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!arguments[i].Equals("-h"))
                {
                    args.Add(argumentsNames[i], arguments[i]);
                }
            }

            if (arguments.Length > 0 && arguments[^1].Equals("-h"))
            {
                Logger.LogInfo(Description);
                Helpers[arguments.Length - 1]?.Invoke(new Context(args));
            }
            else
            {
                Action<Context> del = Delegates[arguments.Length];
                del?.Invoke(new Context(args));
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
                };
                command.Delegates.Add(Delegate);
                command.Helpers.Add(Helper);
            }

            public Command Build() => command;

            public Builder Then(string argumentName, Action<Context> Delegate, Action<Context> Helper = null)
            {
                command.Helpers.Add(Helper);
                command.Delegates.Add(Delegate);
                command.argumentsNames.Add(argumentName);
                return this;
            }
        }
    }
}