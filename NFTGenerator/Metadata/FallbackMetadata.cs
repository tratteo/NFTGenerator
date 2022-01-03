// Copyright Matteo Beltrame

using Newtonsoft.Json;
using NFTGenerator.Source;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator.Metadata;

[Serializable]
public class FallbackMetadata
{
    private bool ordered = false;

    [JsonProperty("fallbacks")]
    private List<FallbackDefinition> fallbacks;

    public FallbackMetadata()
    {
        fallbacks = new List<FallbackDefinition>();
    }

    public List<FallbackDefinition> GetFallbacksByPriority()
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
            if (!fallback.Verify(layersNumber)) return false;
        }
        return true;
    }

    [Serializable]
    public class FallbackDefinition : IMediaProvider
    {
        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("media_name")]
        public string MediaName { get; set; }

        [JsonProperty("incompatible_matrix")]
        public List<int[]> IncompatibleMatrix { get; set; }

        public bool Verify(int layersNumber)
        {
            foreach (var row in IncompatibleMatrix)
            {
                if (row.Length != layersNumber) return false;
            }
            return true;
        }

        public bool CheckIncompatibleHit<T>(IEnumerable<T> set, out int firstIndex, out List<int> toRemove) where T : IMediaProvider, IIdOwner
        {
            toRemove = new List<int>();
            firstIndex = -1;
            if (IncompatibleMatrix.Count <= 0) return false;
            if (IncompatibleMatrix[0].Length != set.Count()) return false;
            foreach (var row in IncompatibleMatrix)
            {
                toRemove.Clear();
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] == -1)
                    {
                        continue;
                    }
                    else if (!row[i].Equals(set.ElementAt(i).Id))
                    {
                        firstIndex = -1;
                        break;
                    }
                    else
                    {
                        firstIndex = firstIndex == -1 ? i : firstIndex;
                        toRemove.Add(i);
                    }
                }
                if (firstIndex >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public string ProvideMediaPath() => $"{Paths.FALLBACKS}\\{MediaName}";
    }
}