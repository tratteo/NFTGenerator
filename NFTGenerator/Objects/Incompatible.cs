// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NFTGenerator.Objects;

[Serializable]
internal class Incompatible
{
    public enum Action
    {
        Replace,
        ChangeOrder
    }

    [JsonProperty("priority")]
    public int Priority { get; set; }

    [JsonProperty("action")]
    public Action FallbackAction { get; set; }

    [JsonProperty("instructions")]
    public List<Instruction> Instructions { get; init; }

    public Incompatible()
    {
        Priority = 0;
        FallbackAction = Action.Replace;
        Instructions = new List<Instruction>();
    }

    public bool Verify() => true;

    public void HandleIncompatible(List<LayerPick> picks, IMediaProvider[] sideEffectMedia)
    {
        if (Instructions.Count <= 0) return;
        foreach (Instruction instruction in Instructions)
        {
            var pick = picks.Find(a => a.Layer.Name.Equals(instruction.LayerName));
            if (!(pick != null && instruction.AssetIndexes.Count > 0 && (instruction.AssetIndexes[0].Equals(-1) || instruction.AssetIndexes.Contains(pick.Asset.Id))))
            {
                return;
            }
        }
        List<int> indexes = new List<int>();
        for (var i = 0; i < Instructions.Count; i++)
        {
            Instruction instruction = Instructions[i];

            var pick = picks.Find(a => a.Layer.Name.Equals(instruction.LayerName));
            int index = picks.IndexOf(pick);
            instruction.CachedHitId = pick.Asset.Id;
            if (FallbackAction == Action.Replace)
            {
                if (instruction.InstructionAction == Instruction.Action.Remove)
                {
                    sideEffectMedia[index] = null;
                }
                else if (instruction.InstructionAction == Instruction.Action.Replace)
                {
                    sideEffectMedia[index] = instruction;
                }
                else if (instruction.InstructionAction == Instruction.Action.Keep)
                {
                    //Do nothing
                }
            }
            else if (FallbackAction == Action.ChangeOrder)
            {
                indexes.Add(index);
            }
        }
        if (FallbackAction == Action.ChangeOrder)
        {
            List<Instruction> sorted = Instructions.OrderBy(i => i.Order).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sideEffectMedia[indexes[i]] = sorted[i];
            }
        }
        return;
    }

    [Serializable]
    public class Instruction : IMediaProvider
    {
        public enum Action
        { Keep, Remove, Replace }

        [JsonProperty("action")]
        public Action InstructionAction { get; init; }

        [JsonProperty("order")]
        public int Order { get; init; }

        [JsonProperty("media_path")]
        public string MediaPath { get; init; } = string.Empty;

        [JsonProperty("media_name")]
        public string MediaName { get; init; }

        [JsonProperty("layer")]
        public string LayerName { get; init; }

        [JsonProperty("assets")]
        public List<int> AssetIndexes { get; init; }

        public int CachedHitId { get; set; }

        public Instruction()
        {
            InstructionAction = Action.Keep;
            CachedHitId = -1;
            MediaName = "*";
            Order = 0;
        }

        public string ProvideMediaPath()
        {
            string res = $"{Paths.FALLBACKS}";
            if (!MediaPath.Equals(string.Empty)) res += $"{MediaPath}\\";
            if (MediaName.Equals("*"))
                res += $"{CachedHitId}.png";
            else
                res += MediaName;
            return res;
        }
    }
}