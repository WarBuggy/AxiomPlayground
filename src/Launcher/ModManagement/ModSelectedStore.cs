using System.Text.Json;

namespace Launcher.ModManagement;

public static class ModSelectedStore
{
    private static readonly string FilePath =
       Path.Combine(AppContext.BaseDirectory, "launchModList.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Save(IEnumerable<ModSelectedState> selectedMods)
    {
        try
        {
            string json = JsonSerializer.Serialize(
                selectedMods,
                _jsonOptions);

            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                Shared.T("errorModSelectedStoreFailToSave", FilePath, ex.Message));
        }
    }

    public static List<ModSelectedState> Load(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string filePath =
          Path.Combine(AppContext.BaseDirectory, fileName);

        if (!File.Exists(filePath))
            return [];

        try
        {
            string json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<List<ModSelectedState>>(
                       json,
                       _jsonOptions)
                   ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                Shared.T("errorModSelectedStoreFailToLoad", filePath, ex.Message));

            return [];
        }
    }

    public static List<ModSelectedState> Load()
    {
        return Load(FilePath);
    }
}