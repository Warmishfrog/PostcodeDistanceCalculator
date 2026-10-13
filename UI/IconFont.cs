using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// Icons from Google's Material Symbols (Outlined, weight 400, <a href="https://fonts.google.com/icons">fonts.google.com/icons</a>).
/// Each value is the icon's codepoint in the font.
/// </summary>
/// <remarks>
/// The font is stored in the repo and embedded in the executable, so the app never contacts Google at
/// runtime and the icons keep working offline or if the Google Fonts API changes or disappears.
/// The URL below is only needed when adding icons during development.
/// To add an icon, re-download the subset with its name added to <c>icon_names</c> (names must be in
/// alphabetical order), replace <c>Resources/MaterialSymbolsOutlined.ttf</c>, and add the codepoint here:
/// <c>https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@24,400,0,0&amp;icon_names=chevron_right,close,content_copy,help,history,info</c>
/// (the CSS links to the TrueType file). Codepoints are listed in the material-design-icons repository's
/// <c>MaterialSymbolsOutlined[FILL,GRAD,opsz,wght].codepoints</c> file.
/// </remarks>
public enum MaterialIcon
{
    ChevronRight = 0xE5CC,
    Close = 0xE5CD,
    ContentCopy = 0xE14D,
    Help = 0xE8FD,
    History = 0xE8B3,
    Info = 0xE88E
}

/// <summary>
/// Draws <see cref="MaterialIcon"/>s from the icon font embedded in the executable, so icons render
/// crisply at any DPI and in any colour, unlike emoji (which GDI draws monochrome or as boxes).
/// </summary>
/// <remarks>
/// The font is loaded into a <see cref="PrivateFontCollection"/>, which only GDI+ (<see cref="Graphics.DrawString(string, Font, Brush, RectangleF, StringFormat)"/>)
/// can use. Control text is drawn with GDI, so controls get icons as images via <see cref="Render"/> rather than as text.
/// </remarks>
public static class IconFont
{
    private const string ResourceName = "MaterialSymbolsOutlined.ttf";

    private static readonly PrivateFontCollection Fonts = LoadFonts();

    private static PrivateFontCollection LoadFonts()
    {
        using var stream = typeof(IconFont).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data);

        // AddMemoryFont needs the memory to stay valid while the font is in use. The font is used for the
        // whole life of the app, so the memory is intentionally never freed.
        IntPtr memory = Marshal.AllocCoTaskMem(data.Length);
        Marshal.Copy(data, 0, memory, data.Length);

        var collection = new PrivateFontCollection();
        collection.AddMemoryFont(memory, data.Length);
        return collection;
    }

    /// <summary>Draws <paramref name="icon"/> centred in <paramref name="bounds"/>, sized to fill its shorter side.</summary>
    public static void Draw(Graphics graphics, MaterialIcon icon, Rectangle bounds, Color color)
    {
        FontFamily family = Fonts.Families[0];
        float size = Math.Min(bounds.Width, bounds.Height);
        using var font = new Font(family, size, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);

        // Each Material Symbol is designed on a square grid that is exactly the font's em square, sitting on the
        // baseline. DrawString positions text from the top of the line (the cell ascent), which is taller than the
        // em, so shift up by the difference to put the icon grid exactly on the target square. (A layout rectangle
        // with StringFormat.GenericTypographic draws nothing here: its LineLimit flag drops the over-tall line.)
        float em = family.GetEmHeight(FontStyle.Regular);
        float ascentAboveEm = size * (family.GetCellAscent(FontStyle.Regular) - em) / em;
        float x = bounds.Left + (bounds.Width - size) / 2f;
        float y = bounds.Top + (bounds.Height - size) / 2f - ascentAboveEm;

        var previousHint = graphics.TextRenderingHint;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.DrawString(char.ConvertFromUtf32((int)icon), font, brush, x, y, StringFormat.GenericTypographic);
        graphics.TextRenderingHint = previousHint;
    }

    /// <summary>
    /// Renders <paramref name="icon"/> onto a new transparent <paramref name="size"/> x <paramref name="size"/> bitmap,
    /// e.g. for <see cref="ButtonBase.Image"/>. The caller owns (and disposes) the bitmap.
    /// </summary>
    public static Bitmap Render(MaterialIcon icon, int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        Draw(graphics, icon, new Rectangle(0, 0, size, size), color);
        return bitmap;
    }
}
