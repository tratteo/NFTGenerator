// Copyright Matteo Beltrame

using Newtonsoft.Json;
using NFTGenerator.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator.Metadata;

[Serializable]
internal partial class FallbackMetadata
{
    [JsonProperty("incompatibles")]
    public List<Incompatible> fallbacks;
    private bool ordered = false;

    public FallbackMetadata()
    {
        fallbacks = new List<Incompatible>();
    }

    public List<Incompatible> GetFallbacksByPriority()
    {
        if (!ordered)
        {
            fallbacks = fallbacks.OrderByDescending(f => f.Priority).ToList();
            ordered = true;
        }
        return fallbacks;
    }

    public bool Verify(int layersNumber)
    {
        foreach (var fallback in fallbacks)
        {
            if (!fallback.Verify()) return false;
        }
        return true;
    }
}