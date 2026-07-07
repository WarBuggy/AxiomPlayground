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
}