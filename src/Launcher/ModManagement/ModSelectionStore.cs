using System.Text.Json;

namespace Launcher.ModManagement;

public static class ModSelectionStore
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "modSelectionState.json");

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public static Dictionary<string, ModSelectionState> Load()
    {
        if (!File.Exists(FilePath))
            return new Dictionary<string, ModSelectionState>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string json = File.ReadAllText(FilePath);

            var list =
                JsonSerializer.Deserialize<List<ModSelectionState>>(json, _jsonOptions) ?? [];

            return list.ToDictionary(
                s => s.ModId,
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                Shared.T("errorModSelectionStoreFailToLoad", FilePath, ex.Message));

            return new Dictionary<string, ModSelectionState>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Save(Dictionary<string, ModSelectionState> states)
    {
        try
        {
            var list = states.Values.ToList();

            string json = JsonSerializer.Serialize(list, _jsonOptions);

            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                Shared.T("errorModSelectionStoreFailToSave", FilePath, ex.Message));
        }
    }
}