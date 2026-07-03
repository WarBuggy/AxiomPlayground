using System.Text.Json;
using AxiomPlayground.Modding.Metadata.Json;
using AxiomPlayground.Modding.Metadata.Model;

namespace AxiomPlayground.Modding.Metadata;

public static class ModInfoParser
{
    private const string INFO_FILE_NAME = "info.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ModInfo Parse(string modFolder)
    {
        string path = Path.Combine(modFolder, INFO_FILE_NAME);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"[ModInfoParser] Missing required mod metadata file: {path}");

        var json = File.ReadAllText(path);

        var dto = Deserialize(json, path);

        Validate(dto, path);

        return Map(dto);
    }

    private static ModInfoFile Deserialize(string json, string path)
    {
        ModInfoFile? dto;

        try
        {
            dto = JsonSerializer.Deserialize<ModInfoFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Invalid JSON in {path}: {ex.Message}", ex);
        }

        if (dto == null)
            throw new InvalidOperationException(
                $"Failed to deserialize {path}");

        return dto;
    }

    private static void Validate(ModInfoFile dto, string path)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new InvalidOperationException(
                $"{path}: 'id' is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException(
                $"{path}: 'name' is required.");

        if (string.IsNullOrWhiteSpace(dto.Author))
            throw new InvalidOperationException(
                $"{path}: 'author' is required.");
    }

    private static ModInfo Map(ModInfoFile dto)
    {
        return new ModInfo
        {
            Id = dto.Id!.Trim(),
            Name = dto.Name!.Trim(),
            Author = dto.Author!.Trim()
        };
    }
}