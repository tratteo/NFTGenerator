// Copyright Matteo Beltrame

using Newtonsoft.Json;
using NFTGenerator.Metadata;
using NFTGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static NFTGenerator.Metadata.TokenMetadata;

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

    [JsonProperty("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("action")]
    public Action FallbackAction { get; set; }

    [JsonProperty("instructions")]
    public List<Instruction> Instructions { get; init; }

    public Incompatible()
    {
        Enabled = true;
        Priority = 0;
        FallbackAction = Action.Replace;
        Instructions = new List<Instruction>();
    }

    public bool Verify(IFilesystem filesystem)
    {
        foreach (Instruction instruction in Instructions)
        {
            if (filesystem.Layers.Find(l => l.Name.Equals(instruction.LayerName)) is null) return false;
        }
        return true;
    }

    public void HandleIncompatible(List<LayerPick> picks, string[] sideEffectMedia, ref double rarityScore, List<AttributeMetadata> attributes, ref int[] mintedHash)
    {
        if (Instructions.Count <= 0 || !Enabled) return;
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

            int cachedHit = pick.Asset.Id;
            if (FallbackAction == Action.Replace)
            {
                if (instruction.InstructionAction == Instruction.Action.Remove)
                {
                    mintedHash[index] = -1;
                    sideEffectMedia[index] = null;
                    rarityScore *= pick.Asset.Metadata.Attribute.Rarity;
                    attributes.RemoveAll(a => a.Equals(pick.Asset.Metadata.Attribute));
                }
                else if (instruction.InstructionAction == Instruction.Action.Replace)
                {
                    string res = $"{Paths.FALLBACKS}";
                    if (!instruction.MediaPath.Equals(string.Empty)) res += $"{instruction.MediaPath}\\";
                    if (instruction.MediaName.Equals("*"))
                        res += $"{cachedHit}.png";
                    else
                        res += instruction.MediaName;

                    sideEffectMedia[index] = res;
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
                LayerPick pick = picks.Find(p => p.Layer.Name.Equals(sorted[i].LayerName));
                sideEffectMedia[indexes[i]] = pick.Asset.ProvideMediaPath();
            }
        }
        return;
    }

    [Serializable]
    public class Instruction
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

        public Instruction()
        {
            InstructionAction = Action.Keep;
            MediaName = "*";
            Order = 0;
        }

        //public string ProvideMediaPath()
        //{
        //    string res = $"{Paths.FALLBACKS}";
        //    if (!MediaPath.Equals(string.Empty)) res += $"{MediaPath}\\";
        //    if (MediaName.Equals("*"))
        //        res += $"{cachedHitId}.png";
        //    else
        //        res += MediaName;
        //    return res;
        //}
    }
}