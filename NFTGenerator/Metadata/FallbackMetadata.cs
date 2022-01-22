// Copyright Matteo Beltrame

using HandierCli;
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

    public List<IMediaProvider> BuildMediaProviders(List<LayerPick> picks)
    {
        List<(int index, IMediaProvider media)> incompatibles = new List<(int, IMediaProvider)>();
        IMediaProvider[] medias = picks.ConvertAll(p => p.Asset as IMediaProvider).ToArray();
        foreach (var fallbackDef in GetFallbacksByPriority())
        {
            fallbackDef.HandleIncompatible(picks, medias);
        }

        List<IMediaProvider> res = new List<IMediaProvider>();
        for (int i = 0; i < medias.Length; i++)
        {
            if (medias[i] != null)
            {
                res.Add(medias[i]);
            }
        }
        return res;
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