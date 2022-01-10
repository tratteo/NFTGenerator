// Copyright Matteo Beltrame

using BetterHaveIt;
using System;
using System.Collections.Generic;
using System.Linq;

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
        Name = PathExtensions.Split(path).Item2;
    }

    public Asset GetRandom()
    {
        List<Asset> match = Assets.FindAll((a) => a.UsedAmount < a.Metadata.Amount);
        int sum = 0;
        foreach (Asset asset in match)
        {
            sum += asset.Metadata.Amount;
        }
        foreach (Asset asset in match)
        {
            asset.PickProbability = (double)asset.Metadata.Amount / sum;
        }
        int index = -1;
        double r = random.NextDouble();
        while (r > 0)
        {
            r -= match[++index].PickProbability;
        }

        return match[index];
    }

    public bool HasMintableAssets() => Assets.FindAll((a) => a.UsedAmount < a.Metadata.Amount).Count > 0;
}