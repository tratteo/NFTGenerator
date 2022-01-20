// Copyright Matteo Beltrame

using BetterHaveIt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTGenerator.Metadata;
using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator.Objects;

internal class Asset : IMediaProvider
{
    private readonly Random random;
    private List<string> assetsPaths;

    public int Id { get; private set; }

    public AssetMetadata Metadata { get; private set; }

    public int UsedAmount { get; set; } = 0;

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

        if (Serializer.DeserializeJson(string.Empty, metadata[0], out AssetMetadata assetMetadata))
        {
            asset.Metadata = assetMetadata;
        }

        asset.assetsPaths = new List<string>();
        bool isPooled = Directory.Exists($"{resourceAbsolutePath}\\{id}");
        string assetsPaths = isPooled ? $"{resourceAbsolutePath}\\{id}" : resourceAbsolutePath;
        string searchPattern = isPooled ? "*.png" : $"{id}.png";

        var assets = Directory.GetFiles(assetsPaths, searchPattern);
        if (assets.Length <= 0)
        {
            logger.LogWarning("No assets in path: {path}, skipping", assetsPaths);
            asset = null;
            return false;
        }

        asset.assetsPaths.AddRange(assets);
        return true;
    }

    public string ProvideMediaPath() => assetsPaths[random.Next(0, assetsPaths.Count)];
}