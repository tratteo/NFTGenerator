using System;
using System.Configuration;

namespace NFTGenerator
{
    internal class Program
    {
        private const int AMOUNT_TO_MINT = 4;
        private static Generator generator;
        private static Filesystem filesystem;

        private static void Main(string[] args)
        {
            Console.WriteLine("NFT Generator");
            Console.WriteLine(GetSetting<bool>("test"));
            filesystem = new Filesystem(GetSetting<string>("fileSystemPath"));
            filesystem.Load();
            if (!filesystem.Verify(AMOUNT_TO_MINT))
            {
                return;
            }
            generator = new Generator(filesystem);
            Test();
        }

        private static void Test()
        {
            for (int j = 0; j < AMOUNT_TO_MINT; j++)
            {
                generator.GenerateSingle(j);
            }
            foreach (int[] hash in generator.GeneratedHashes)
            {
                foreach (int i in hash)
                {
                    Console.Write(i);
                }
                Console.Write("\n\n");
            }
        }

        private static T GetSetting<T>(string key, T defaultValue = default(T))
        {
            string val = ConfigurationManager.AppSettings[key] ?? "";
            T result = defaultValue;
            if (!string.IsNullOrEmpty(val))
            {
                T typeDefault = default(T);
                if (typeof(T) == typeof(string))
                {
                    typeDefault = (T)(object)string.Empty;
                }
                result = (T)Convert.ChangeType(val, typeDefault.GetType());
            }
            return result;
        }
    }
}