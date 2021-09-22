using System;
using System.Collections.Generic;
using System.IO;

namespace NFTGenerator
{
    internal class Filesystem
    {
        public List<Layer> Layers { get; private set; }

        public string MediaExtension { get; private set; } = string.Empty;

        public Filesystem()
        {
            Layers = new List<Layer>();
        }

        public bool Verify(bool verbose = true)
        {
            List<Action> warnings = new List<Action>();
            int amountToMint = Configurator.GetSetting<int>(Configurator.SERIE_AMOUNT);
            Load(verbose);
            if (verbose)
            {
                Logger.LogInfo("Verifying whether layers are fucked up or not...");
            }
            string fileExtension = string.Empty;
            foreach (Layer layer in Layers)
            {
                int amount = 0;
                foreach (Asset a in layer.Assets)
                {
                    amount += a.Metadata.Amount;
                    FileInfo info = new(a.AssetAbsolutePath);

                    if (info.Extension != fileExtension && fileExtension != string.Empty)
                    {
                        Logger.LogError("Assets are not of the same type at: " + a.AssetAbsolutePath);
                        return false;
                    }
                    fileExtension = info.Extension;
                }
                MediaExtension = fileExtension;
                if (amount < amountToMint)
                {
                    Logger.LogError("Wrong assets sum in layer: " + layer.Path);
                    return false;
                }
                else if (amount > amountToMint)
                {
                    warnings.Add(() => Logger.LogWarning("Assets sum in layer: " + layer.Path + " is greater than the AMOUNT_TO_MINT, adjust it if you want amounts in metadata to actually represents probabilities"));
                }
            }
            if (verbose)
            {
                Logger.LogInfo("Verifying some weird math...");
            }
            int dispositions = 1;
            Layers.ForEach(l =>
            {
                dispositions *= l.Assets.Count;
            });
            if (dispositions < amountToMint)
            {
                Logger.LogError("There are less mathematical available disposition than the amount to mint (" + amountToMint + ")");
                return false;
            }
            if (amountToMint == 0)
            {
                warnings.Add(() => Logger.LogWarning("The amount to mint is set to 0 in the configuration file"));
            }
            if (verbose)
            {
                Logger.LogInfo("Verification process passed with " + warnings.Count + " warnings", ConsoleColor.Green);
                foreach (Action w in warnings)
                {
                    w?.Invoke();
                }
            }
            return true;
        }

        private bool Layout()
        {
            bool created = false;
            if (!Directory.Exists(Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH)))
            {
                Directory.CreateDirectory(Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH));
                Logger.LogInfo("Created FS root directory: " + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH));
                created = true;
            }
            if (!Directory.Exists(Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH) + "\\layers"))
            {
                Directory.CreateDirectory(Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH) + "\\layers");
                Logger.LogInfo("Created FS root directory: " + Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH) + "\\layers");
                created = true;
            }
            if (!Directory.Exists(Configurator.GetSetting<string>(Configurator.RESULTS_PATH)))
            {
                Directory.CreateDirectory(Configurator.GetSetting<string>(Configurator.RESULTS_PATH));
                Logger.LogInfo("Created FS root directory: " + Configurator.GetSetting<string>(Configurator.RESULTS_PATH));
                created = true;
            }
            return created;
        }

        private void Load(bool verbose = true)
        {
            Layers.Clear();
            Layout();
            if (verbose)
            {
                Logger.LogInfo("Loading layers");
            }
            string[] dirs = Directory.GetDirectories(Configurator.GetSetting<string>(Configurator.FILESYSTEM_PATH) + "\\layers");
            for (int i = 0; i < dirs.Length; i++)
            {
                Layer layer = new Layer(dirs[i]);
                string[] assets = Directory.GetDirectories(dirs[i]);
                for (int j = 0; j < assets.Length; j++)
                {
                    string assetPath = assets[j];
                    layer.Assets.Add(new Asset(assetPath, (int)j));
                }
                Layers.Add(layer);
            }
        }
    }
}