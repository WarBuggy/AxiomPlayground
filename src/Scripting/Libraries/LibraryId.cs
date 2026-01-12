namespace AxiomPlayground.Scripting.Libraries;

/// <summary>
/// Represents a unique library identifier (PublishingModId + LibraryName)
/// </summary>
public readonly struct LibraryId : IEquatable<LibraryId>
{
    public string PublishingModId { get; }
    public string LibraryName { get; }

    public LibraryId(string publishingModId, string libraryName)
    {
        if (string.IsNullOrWhiteSpace(publishingModId))
            throw new ArgumentException("[LibraryId] Publishing mod id cannot be null or empty.", nameof(publishingModId));
        if (string.IsNullOrWhiteSpace(libraryName))
            throw new ArgumentException("[LibraryId] Library name cannot be null or empty.", nameof(libraryName));

        PublishingModId = publishingModId;
        LibraryName = libraryName;
    }

    public override string ToString() => $"{PublishingModId}.{LibraryName}";

    public bool Equals(LibraryId other) =>
        string.Equals(PublishingModId, other.PublishingModId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(LibraryName, other.LibraryName, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is LibraryId other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(PublishingModId);
            hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(LibraryName);
            return hash;
        }
    }

    public static bool operator ==(LibraryId left, LibraryId right) => left.Equals(right);
    public static bool operator !=(LibraryId left, LibraryId right) => !left.Equals(right);

    public static LibraryId Parse(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("[LibraryId] Full library name cannot be null or empty.", nameof(fullName));

        int dotIndex = fullName.IndexOf('.');
        if (dotIndex <= 0 || dotIndex == fullName.Length - 1)
            throw new ArgumentException(
                $"[LibraryId] Invalid library id '{fullName}'. Expected format: ModId.LibraryName");

        string publishingModId = fullName[..dotIndex];
        string libraryName = fullName[(dotIndex + 1)..];

        return new LibraryId(publishingModId, libraryName);
    }
}