// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NFTGenerator.Objects;

[Serializable]
internal class Incompatible : IMediaProvider
{
    public enum Action
    {
        ReplaceAll,
        DelegateToInstructions
    }

    [JsonProperty("priority")]
    public int Priority { get; set; }

    [JsonProperty("action")]
    public Action? FallbackAction { get; set; }

    [JsonProperty("media_name")]
    public string MediaName { get; set; }

    [JsonProperty("instructions")]
    public List<Instruction> Instructions { get; init; }

    public bool Verify() => FallbackAction != null && FallbackAction != Action.ReplaceAll || MediaName != null && MediaName != string.Empty;

    public bool HasInstructionsHit(List<LayerPick> picks, out int firstIndex, out List<LayerPick> toRemove)
    {
        toRemove = new List<LayerPick>();
        firstIndex = -1;
        if (Instructions.Count <= 0) return false;
        foreach (var instruction in Instructions)
        {
            var pick = picks.Find(a => a.Layer.Name.Equals(instruction.LayerName));
            if (pick != null && instruction.AssetIndexes.Count > 0 && (instruction.AssetIndexes[0].Equals(-1) || instruction.AssetIndexes.Contains(pick.Asset.Id)))
            {
                if (FallbackAction == Action.ReplaceAll || (FallbackAction == Action.DelegateToInstructions && instruction.InstructionAction == Instruction.Action.Remove))
                {
                    toRemove.Add(pick);
                }
                firstIndex = firstIndex == -1 ? pick.Layer.Index : firstIndex;
            }
            else
            {
                return false;
            }
        }
        return firstIndex > 0;
    }

    public string ProvideMediaPath() => $"{Paths.FALLBACKS}\\{MediaName}";

    [Serializable]
    public struct Instruction
    {
        public enum Action
        { Keep, Remove }

        [JsonProperty("action")]
        public Action? InstructionAction { get; init; }

        [JsonProperty("layer")]
        public string LayerName { get; init; }

        [JsonProperty("assets")]
        public List<int> AssetIndexes { get; init; }
    }
}