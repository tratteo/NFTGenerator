using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Filesystem
    {
        public List<Layer> Layers { get; private set; }

        public string Path { get; private set; }

        public Filesystem(string path)
        {
            Path = path;
            Layers = new List<Layer>();
        }

        public bool Verify(int amountToMint)
        {
            Logger.Log("\nVerifying whether layers are fucked up or not...");
            foreach (Layer layer in Layers)
            {
                int amount = 0;
                layer.Assets.ForEach((a) => amount += a.Data.Amount);
                if (amount < amountToMint)
                {
                    Logger.Log("Wrong assets sum in layer: " + layer.Path, Logger.LogType.ERROR);
                    return false;
                }
                else if (amount > amountToMint)
                {
                    Logger.Log("Assets sum in layer: " + layer.Path + " is greater than the AMOUNT_TO_MINT, adjust it if you want amounts in metadata to actually represents probabilities", Logger.LogType.WARNING);
                }
            }
            Logger.Log("Verifying some weird math...");
            int dispositions = 1;
            Layers.ForEach(l =>
            {
                dispositions *= l.Assets.Count;
            });
            if (dispositions < amountToMint)
            {
                Logger.Log("Unable to mint more NFT than the actual dispositions amount!", Logger.LogType.ERROR);
                return false;
            }
            Logger.Log("Verification process passed\n");
            return true;
        }

        public void Load()
        {
            Console.WriteLine("\nLoading layers");
            string[] dirs = Directory.GetDirectories(Path + "\\layers");
            for (int i = 0; i < dirs.Length; i++)
            {
                Layer layer = new Layer(dirs[i]);
                Console.Write(".");
                string[] assets = Directory.GetDirectories(dirs[i]);
                for (int j = 0; j < assets.Length; j++)
                {
                    string assetPath = assets[j];
                    layer.Assets.Add(new Asset(assetPath, (int)j));
                }
                Layers.Add(layer);
            }
            Console.WriteLine();
            //Console.WriteLine("\nLayers loaded");
            //for (int i = 0; i < layers.Length; i++)
            //{
            //    Console.WriteLine("LAYER " + i);
            //    foreach (Asset asset in layers[i])
            //    {
            //        Console.WriteLine(asset);
            //    }
            //}
        }
    }
}