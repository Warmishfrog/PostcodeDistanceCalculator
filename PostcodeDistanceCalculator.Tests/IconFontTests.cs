using System.Drawing;
using PostcodeDistanceCalculator.UI;

namespace PostcodeDistanceCalculator.Tests;

public class IconFontTests
{
    private static List<Color> Pixels(Bitmap bitmap)
        => [.. Enumerable.Range(0, bitmap.Width * bitmap.Height).Select(i => bitmap.GetPixel(i % bitmap.Width, i / bitmap.Width))];

    public static TheoryData<MaterialIcon> AllIcons() => [.. Enum.GetValues<MaterialIcon>()];

    [Theory]
    [MemberData(nameof(AllIcons))]
    public void Every_icon_has_a_real_glyph_in_the_embedded_font(MaterialIcon icon)
    {
        using var rendered = IconFont.Render(icon, 32, Color.Black);
        // A codepoint the subset doesn't contain, to see what "missing glyph" looks like
        using var missing = IconFont.Render((MaterialIcon)0xE000, 32, Color.Black);

        var pixels = Pixels(rendered);
        Assert.Contains(pixels, p => p.A > 0);
        Assert.NotEqual(Pixels(missing), pixels);
    }

    [Fact]
    public void Icons_are_centred_in_their_square()
    {
        // The help icon is a circle centred on the Material icon grid, so its ink should be centred in the bitmap
        const int size = 48;
        using var rendered = IconFont.Render(MaterialIcon.Help, size, Color.Black);

        var ink = Enumerable.Range(0, size * size)
            .Select(i => (X: i % size, Y: i / size))
            .Where(p => rendered.GetPixel(p.X, p.Y).A > 64)
            .ToList();

        double centreX = (ink.Min(p => p.X) + ink.Max(p => p.X)) / 2.0;
        double centreY = (ink.Min(p => p.Y) + ink.Max(p => p.Y)) / 2.0;
        Assert.InRange(centreX, size / 2.0 - 1.5, size / 2.0 + 0.5);
        Assert.InRange(centreY, size / 2.0 - 1.5, size / 2.0 + 0.5);
    }
}
