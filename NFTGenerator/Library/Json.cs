using Newtonsoft.Json;
using System.IO;

namespace NFTGenerator
{
    internal static class Json
    {
        public static T Deserialize<T>(string path = "metadata.json")
        {
            string jsonString = File.ReadAllText(path);
            T metadata = JsonConvert.DeserializeObject<T>(jsonString);
            return metadata;
        }

        public static void Serialize<T>(T metadata, string path = "metadata.json")
        {
            string jsonString = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            File.WriteAllText(path, jsonString);
        }
    }
}