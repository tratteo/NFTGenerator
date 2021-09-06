using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Program
    {
        private static List<Asset>[] layers;
        private static Random random = new Random();

        private static void Main(string[] args)
        {
            Console.WriteLine("NFT Generator");
            LoadLayers();
            Test();
        }

        private static void Test()
        {
            for (int j = 0; j < 10; j++)
            {
                string resPath = "filesystem/results/res_" + j + ".gif";
                List<Asset> toMerge = new List<Asset>();
                for (int i = 0; i < layers.Length; i++)
                {
                    //TODO get random asset inside each layer
                    toMerge.Add(layers[i][random.Next(0, layers[i].Count)]);
                }
                //At this point i have all the assets to be merged
                if (toMerge.Count < 2)
                {
                    throw new Exception("Unable to merge less than 2 assets!");
                }
                // Create the first gif
                GifHandler.MergeGifs(toMerge[0].AssetAbsolutePath, toMerge[1].AssetAbsolutePath, resPath);
                for (int i = 2; i < toMerge.Count; i++)
                {
                    GifHandler.MergeGifs(resPath, toMerge[i].AssetAbsolutePath);
                }
            }
        }

        private static void LoadLayers()
        {
            Console.WriteLine("Loading layers");
            string[] dirs = Directory.GetDirectories("filesystem/layers");
            layers = new List<Asset>[dirs.Length];
            for (int i = 0; i < dirs.Length; i++)
            {
                Console.Write(".");
                layers[i] = new List<Asset>();
                string[] assets = Directory.GetDirectories(dirs[i]);
                foreach (string assetPath in assets)
                {
                    layers[i].Add(new Asset(assetPath));
                }
            }
            Console.WriteLine("\nLayers loaded");
            for (int i = 0; i < layers.Length; i++)
            {
                Console.WriteLine("LAYER " + i);
                foreach (Asset asset in layers[i])
                {
                    Console.WriteLine(asset);
                }
            }
        }

        //Workflow
        // 1. Load all assets into predefined data types
        // 2. Layers are lists of Assets
        // 3. Load all layers into list
    }
}