using System;

namespace NFTGenerator
{
    [System.Serializable]
    internal class Attribute : IEquatable<Attribute>
    {
        public string Name { get; init; }

        public string Value { get; init; }

        public static Attribute Define(string name, string value)
        {
            return new Attribute()
            {
                Name = name,
                Value = value
            };
        }

        public bool Equals(Attribute other)
        {
            return Name.Equals(other.Name) && Value.Equals(other.Value);
        }

        public override string ToString()
        {
            return "{" + Name + ": " + Value + "}";
        }
    }
}