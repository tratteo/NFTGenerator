// Copyright Matteo Beltrame

using Newtonsoft.Json;
using NFTGenerator.Objects;
using NFTGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static NFTGenerator.Metadata.TokenMetadata;

namespace NFTGenerator.Metadata;

[Serializable]
internal partial class FallbackMetadata
{
    [JsonProperty("incompatibles")]
    public List<Incompatible> incompatibles;

    private bool ordered = false;

    public FallbackMetadata()
    {
        incompatibles = new List<Incompatible>();
    }

    public List<Incompatible> GetFallbacksByPriority()
    {
        if (!ordered)
        {
            incompatibles = incompatibles.OrderByDescending(f => f.Priority).ToList();
            ordered = true;
        }
        return incompatibles;
    }

    public List<string> BuildMediaProviders(List<LayerPick> picks, ref double rarityScore, List<AttributeMetadata> attributes, ref int[] mintedHash)
    {
        string[] medias = picks.ConvertAll(p => p.Asset.ProvideMediaPath()).ToArray();
        foreach (var fallbackDef in GetFallbacksByPriority())
        {
            fallbackDef.HandleIncompatible(picks, medias, ref rarityScore, attributes, ref mintedHash);
        }

        List<string> res = new List<string>();
        for (int i = 0; i < medias.Length; i++)
        {
            if (medias[i] != null)
            {
                res.Add(medias[i]);
            }
        }
        return res;
    }

    public bool Verify(IFilesystem filesystem)
    {
        foreach (var incompatible in incompatibles)
        {
            if (!incompatible.Verify(filesystem)) return false;
        }
        return true;
    }
}