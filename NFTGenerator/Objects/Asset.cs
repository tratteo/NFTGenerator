// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Logging;
using NFTGenerator.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static NFTGenerator.Metadata.TokenMetadata;

namespace NFTGenerator.Objects;

internal class Asset : IMediaProvider
{
    public int usedAmount = 0;
    private readonly Random random;
    private List<PooledAsset> assetsPaths;

    public int Id { get; private set; }

    public AssetMetadata Metadata { get; private set; }

    public double PickProbability { get; set; }

    private Asset()
    {
        random = new Random();
    }

    public static bool TryParse(out Asset asset, string resourceAbsolutePath, int id, ILogger logger)
    {
        asset = new Asset
        {
            Id = id
        };
        var metadata = Directory.GetFiles(resourceAbsolutePath, $"{id}.json");
        if (metadata.Length > 1)
        {
            logger.LogWarning("Found multiple metadata in path: {path}, using {meta}", resourceAbsolutePath, metadata[0]);
        }
        else if (metadata.Length <= 0)
        {
            logger.LogWarning("No metadata in path: {path}, skipping", resourceAbsolutePath);
            asset = null;
            return false;
        }

        if (Serializer.DeserializeJson(metadata[0], out AssetMetadata assetMetadata))
        {
            asset.Metadata = assetMetadata;
        }
        if (asset.Metadata.Attribute is null)
        {
            asset.Metadata.Attribute = new AttributeMetadata();
        }
        asset.assetsPaths = new List<PooledAsset>();
        var isPooled = Directory.Exists($"{resourceAbsolutePath}\\{id}");
        var assetsPaths = isPooled ? $"{resourceAbsolutePath}\\{id}" : resourceAbsolutePath;
        var searchPattern = isPooled ? "*.png" : $"{id}.png";

        var assets = Directory.GetFiles(assetsPaths, searchPattern);
        if (assets.Length <= 0)
        {
            logger.LogWarning("No assets in path: {path}, skipping", assetsPaths);
            asset = null;
            return false;
        }
        var elementsPerAsset = asset.Metadata.Amount / assets.Length;
        var rest = asset.Metadata.Amount - (elementsPerAsset * assets.Length);
        for (var i = 0; i < assets.Length; i++)
        {
            if (i == assets.Length - 1)
            {
                asset.assetsPaths.Add(new PooledAsset() { Path = assets[i], Amount = elementsPerAsset + rest });
            }
            else
            {
                asset.assetsPaths.Add(new PooledAsset() { Path = assets[i], Amount = elementsPerAsset });
            }
        }

        return true;
    }

    public string ProvideMediaPath()
    {
        var list = assetsPaths.FindAll(p => p.usedAmount < p.Amount);
        if (list.Count <= 0) throw new Exception("Unable to find pooled assets");
        var sel = list[random.Next(0, list.Count)];
        Interlocked.Increment(ref sel.usedAmount);
        return sel.Path;
    }

    private struct PooledAsset
    {
        public int usedAmount;

        public string Path { get; init; }

        public int Amount { get; init; }
    }
}