using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Program
    {
        private static List<Asset>[] layers;

        private static void Main(string[] args)
        {
            Console.WriteLine("NFT Generator");
            LoadLayers();
        }

        private static void LoadLayers()
        {
            Console.WriteLine("Loading layers");
            string[] dirs = Directory.GetDirectories("filesystem");
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