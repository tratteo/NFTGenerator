using System;
using System.Collections.Generic;

namespace NFTGenerator
{
    internal class Layer
    {
        private readonly Random random;

        public List<Asset> Assets { get; private set; }

        public string Path { get; init; }

        public Layer(string path)
        {
            Assets = new List<Asset>();
            random = new Random();
            Path = path;
        }

        public Asset GetRandom()
        {
            List<Asset> match = Assets.FindAll((a) => a.MintedAmount < a.Data.Amount);
            if (match.Count <= 0)
            {
                throw new System.Exception("Wrong error number in layer: " + Path + ", this should never happen");
            }
            return match[random.Next(0, match.Count)];
        }

        public bool HasMintableAssets()
        {
            return Assets.FindAll((a) => a.MintedAmount < a.Data.Amount).Count > 0;
        }
    }
}