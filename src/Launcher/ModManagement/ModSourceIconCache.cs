using AxiomPlayground.Modding;
using Launcher.Properties;

namespace Launcher.ModManagement;

public static class ModSourceIconCache
{
    private sealed class IconPair
    {
        public Image Normal { get; }
        public Image Disabled { get; }

        public IconPair(Image normal)
        {
            Normal = normal;
            Disabled = ToGrayscale(normal);
        }
    }

    private static readonly Dictionary<ModSource, IconPair> _icons = new()
    {
        {
            ModSource.Steam, new IconPair(AppResources.SteamIcon)
        },
        {
            ModSource.Local, new IconPair(AppResources.LocalIcon)
        }
    };

    public static Image Get(ModSource source, bool disabled = false)
    {
        if (!_icons.TryGetValue(source, out var icon))
            throw new InvalidOperationException(
                $"No icon registered for source {source}");

        return disabled ? icon.Disabled : icon.Normal;
    }

    private static Bitmap ToGrayscale(Image original)
    {
        var bmp = new Bitmap(original.Width, original.Height);

        using var g = Graphics.FromImage(bmp);

        var colorMatrix = new System.Drawing.Imaging.ColorMatrix(
        [
            [0.3f, 0.3f, 0.3f, 0, 0],
            [0.59f,0.59f,0.59f,0, 0],
            [0.11f,0.11f,0.11f,0, 0],
            [0,    0,    0,    1, 0],
            [0,    0,    0,    0, 1]
        ]);

        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(colorMatrix);

        g.DrawImage(
            original,
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            0,
            0,
            original.Width,
            original.Height,
            GraphicsUnit.Pixel,
            attributes);

        return bmp;
    }
}