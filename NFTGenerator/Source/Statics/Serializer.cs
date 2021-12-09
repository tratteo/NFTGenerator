// Copyright Matteo Beltrame

using Newtonsoft.Json;
using System.IO;

namespace NFTGenerator;

public class Serializer
{
    public static bool DeserializeJson<T>(string path, string name, out T? json)
    {
        if (File.Exists(path + name))
        {
            var jsonString = File.ReadAllText(path + name);
            T? metadata = JsonConvert.DeserializeObject<T>(jsonString);
            json = metadata;
            return true;
        }
        json = default;
        return false;
    }

    public static void SerializeJson<T>(string path, string name, T metadata, bool createPath = true)
    {
        if (createPath && !path.Equals(string.Empty))
        {
            Directory.CreateDirectory(path);
        }
        var jsonString = JsonConvert.SerializeObject(metadata, Formatting.Indented);
        File.WriteAllText(path + name, jsonString);
    }

    public static void WriteAll(string path, string name, object obj, bool createPath = true)
    {
        if (createPath && !path.Equals(string.Empty))
        {
            Directory.CreateDirectory(path);
        }
        File.WriteAllText(path + name, obj.ToString());
    }

    public static string ReadAll(string path, string name) => File.Exists(path + name) ? File.ReadAllText(path + name) : string.Empty;
}