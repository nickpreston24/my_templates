using CodeMechanic.Types;
using Newtonsoft.Json;

namespace worker1;

public static class ConfigReader
{
    public static T LoadConfig<T>(string filename, T fallback)
    {
        string cwd = Directory.GetCurrentDirectory();
        string file_path = Path.Combine(cwd, filename);
        // Console.WriteLine("file path : " + file_path);
        string json = file_path.NotEmpty() ? File.ReadAllText(file_path) : string.Empty;
        // Console.WriteLine($"config for {typeof(T).Name} " + file_path);
        // Console.WriteLine("raw json: " + json);
        var settings = json.Length > 0 ? JsonConvert.DeserializeObject<T>(json) : fallback;
        return settings;
    }
}