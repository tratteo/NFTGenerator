using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NFTGenerator
{
    internal static class JsonHandler
    {
        public static T Deserialize<T>(string path = "metadata.json")
        {
            string jsonString = File.ReadAllText(path);
            T metadata = JsonSerializer.Deserialize<T>(jsonString);
            return metadata;
        }

        public static void Serialize(Metadata metadata, string path = "metadata.json")
        {
            string jsonString = JsonSerializer.Serialize(metadata);
            File.WriteAllText(path, jsonString);
        }

        public static class Async
        {
            public static async ValueTask<Metadata> Deserialize(string path = "metadata.json")
            {
                using FileStream openStream = File.OpenRead(path);
                Metadata metadata = await JsonSerializer.DeserializeAsync<Metadata>(openStream);
                return metadata;
            }

            public static async Task Serialize(Metadata metadata, string path = "metadata.json")
            {
                using FileStream createStream = File.Create(path);
                await JsonSerializer.SerializeAsync(createStream, metadata, new JsonSerializerOptions { WriteIndented = true });
                await createStream.DisposeAsync();
            }
        }
    }
}