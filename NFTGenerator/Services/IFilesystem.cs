﻿// Copyright Matteo Beltrame

using NFTGenerator.Metadata;
using NFTGenerator.Objects;
using System.Collections.Generic;

namespace NFTGenerator.Services;

internal interface IFilesystem
{
    public List<Layer> Layers { get; }

    public FallbackMetadata FallbackMetadata { get; }

    public double MinRarity { get; }

    public double MaxRarity { get; }

    public bool Verify();

    public float CalculateDispositions();
}