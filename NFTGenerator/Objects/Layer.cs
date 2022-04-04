// Copyright Matteo Beltrame

using System;
using System.Collections.Generic;

namespace NFTGenerator.Objects;

internal class Layer
{
    private readonly Random random;

    public List<Asset> Assets { get; private set; }

    public string Path { get; private set; }

    public int Index { get; init; }

    public string Name { get; private set; }

    public Layer(string path, int index)
    {
        Assets = new List<Asset>();
        Index = index;
        random = new Random();
        Path = path;
        Name = System.IO.Path.GetFileName(path);
    }

    public Asset GetRandom()
    {
        var match = Assets.FindAll((a) => a.usedAmount < a.Metadata.Amount);
        var sum = 0;
        foreach (var asset in match)
        {
            sum += asset.Metadata.Amount;
        }
        foreach (var asset in match)
        {
            asset.PickProbability = (double)asset.Metadata.Amount / sum;
        }
        var index = -1;
        var r = random.NextDouble();
        while (r > 0)
        {
            r -= match[++index].PickProbability;
        }

        return match[index];
    }

    public bool HasMintableAssets() => Assets.FindAll((a) => a.usedAmount < a.Metadata.Amount).Count > 0;
}