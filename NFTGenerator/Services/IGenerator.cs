// Copyright Matteo Beltrame

using System;

namespace NFTGenerator.Services;

internal interface IGenerator
{
    public string GenerateSingle(int index, IProgress<int> progress = null);
}