using System;
using System.Collections.Generic;

namespace NFTGenerator
{
    internal class Generator
    {
        private readonly Filesystem filesystem;

        public List<int[]> GeneratedHashes { get; private set; }

        public Generator(Filesystem filesystem)
        {
            GeneratedHashes = new List<int[]>();
            this.filesystem = filesystem;
        }

        public void GenerateSingle(int index)
        {
            int[] mintedHash = new int[filesystem.Layers.Count];
            string resPath = Configurator.GetSetting<string>(Configurator.RESULTS_PATH) + "\\res_" + index + filesystem.MediaExtension;
            List<Asset> toMerge = new List<Asset>();
            for (int i = 0; i < filesystem.Layers.Count; i++)
            {
                //TODO get random asset inside each layer
                Asset pick = filesystem.Layers[i].GetRandom();
                mintedHash[i] = pick.Id;
                //We are in last layer and there is a duplicate
                if (!Configurator.GetSetting<bool>(Configurator.ALLOW_DUPLICATES) && i == filesystem.Layers.Count - 1 && !IsHashValid(mintedHash))
                {
                    pick = GetLastLayerValidAsset(mintedHash);
                    mintedHash[i] = pick.Id;
                }
                pick.MintedAmount++;
                toMerge.Add(pick);
            }
            //At this point i have all the assets to be merged
            if (toMerge.Count < 2)
            {
                throw new Exception("Unable to merge less than 2 assets!");
            }
            // Create the first gif
            Media.ComposeMedia(toMerge[0].AssetAbsolutePath, toMerge[1].AssetAbsolutePath, resPath);
            for (int i = 2; i < toMerge.Count; i++)
            {
                Media.ComposeMedia(resPath, toMerge[i].AssetAbsolutePath, resPath);
            }
            GeneratedHashes.Add(mintedHash);
        }

        private Asset GetLastLayerValidAsset(int[] hash)
        {
            if (!filesystem.Layers[^1].HasMintableAssets())
            {
                throw new Exception("Ok this should never happen, the last layer has no mintable assets");
            }
            foreach (Asset asset in filesystem.Layers[^1].Assets)
            {
                if (asset.MintedAmount < asset.Data.Amount && asset.Id != hash[^1])
                {
                    //Valid asset
                    return asset;
                }
            }
            Logger.LogError("Wait, WTF, unable to find a last layer available asset, this means that assets in the last layer are less than expected!");
            return null;
        }

        private bool IsHashValid(int[] current)
        {
            return GeneratedHashes.FindAll((h) =>
            {
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i] != h[i])
                    {
                        return false;
                    }
                }
                return true;
            }).Count <= 0;
        }
    }
}