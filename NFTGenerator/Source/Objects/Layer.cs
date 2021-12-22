// Copyright Matteo Beltrame

using System;
using System.Collections.Generic;

namespace NFTGenerator;

internal class Layer
{
    private readonly Random random;

    public List<Asset> Assets { get; private set; }

    public string Path { get; private set; }

    public string Name { get; private set; }

    public Layer(string path)
    {
        Assets = new List<Asset>();
        random = new Random();
        Path = path;
        Name = Paths.Split(path).Item2;
    }

    public Asset GetRandom()
    {
        List<Asset> match = Assets.FindAll((a) => a.UsedAmount < a.Metadata.Amount);
        return match.Count <= 0
            ? throw new System.Exception("Wrong error number in layer: " + Path + ", this should never happen")
            : match[random.Next(0, match.Count)];
    }

    public bool HasMintableAssets() => Assets.FindAll((a) => a.UsedAmount < a.Metadata.Amount).Count > 0;
}