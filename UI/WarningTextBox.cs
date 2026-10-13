namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// A text box that draws a wavy amber underline under ranges of text, like a spelling checker, and shows each
/// range's message as a tooltip on hover. Used to flag postcodes that may be typos.
/// </summary>
internal sealed class WarningTextBox : TextBox
{
    /// <summary>A range of text to underline and the message to show for it.</summary>
    public sealed record TextWarning(int Start, int Length, string Message);

    /// <summary>Amber: noticeable without looking like an error.</summary>
    public static readonly Color WarningColor = Color.FromArgb(217, 119, 6);

    private const int WM_PAINT = 0x000F;

    private readonly ToolTip tooltip = new();
    private IReadOnlyList<TextWarning> warnings = [];
    private TextWarning? hovered;

    /// <summary>Ranges to underline. Setting this redraws the box. Set from code only, never by the designer.</summary>
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<TextWarning> Warnings
    {
        get => warnings;
        set
        {
            warnings = value;
            hovered = null;
            tooltip.Hide(this);
            Invalidate();
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // The native edit control paints its own text, so draw the underlines on top once it has
        if (m.Msg == WM_PAINT && warnings.Count > 0)
        {
            using var graphics = CreateGraphics();
            DrawUnderlines(graphics);
        }
    }

    private void DrawUnderlines(Graphics graphics)
    {
        using var pen = new Pen(SystemInformation.HighContrast ? SystemColors.Highlight : WarningColor, Math.Max(1, LogicalToDeviceUnits(1)));
        int amplitude = Math.Max(1, LogicalToDeviceUnits(1));
        int step = LogicalToDeviceUnits(2);
        int lineHeight = Font.Height;
        int textLength = TextLength;

        foreach (var warning in warnings)
        {
            if (warning.Start < 0 || warning.Start + warning.Length > textLength) continue;

            // A range can wrap onto the next line, so underline each line's part separately
            foreach (var (left, right, top) in LineSegments(warning.Start, warning.Length))
                DrawSquiggle(graphics, pen, left, right, top + lineHeight - amplitude - 1, amplitude, step);
        }
    }

    /// <summary>Draws a wavy line from <paramref name="left"/> to <paramref name="right"/> around <paramref name="baseline"/>.</summary>
    internal static void DrawSquiggle(Graphics graphics, Pen pen, int left, int right, int baseline, int amplitude, int step)
    {
        var points = new List<Point>();
        for (int x = left, i = 0; x <= right; x += step, i++)
            points.Add(new Point(x, baseline + (i % 2 == 0 ? amplitude : -amplitude)));
        if (points.Count >= 2) graphics.DrawLines(pen, points.ToArray());
    }

    /// <summary>The on-screen extent of a text range, one (left, right, top) segment per line it covers.</summary>
    private IEnumerable<(int Left, int Right, int Top)> LineSegments(int start, int length)
    {
        // Read once: Text asks the native control for a fresh copy every time
        string text = Text;
        Point first = GetPositionFromCharIndex(start);
        int left = first.X, top = first.Y, right = first.X;

        for (int i = start; i < start + length; i++)
        {
            Point position = GetPositionFromCharIndex(i);
            if (position.Y != top)
            {
                yield return (left, right, top);
                left = position.X;
                top = position.Y;
            }
            right = position.X + TextRenderer.MeasureText(text[i].ToString(), Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
        }
        yield return (left, right, top);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        TextWarning? under = WarningAt(e.Location);
        if (under == hovered) return;

        hovered = under;
        if (under == null)
            tooltip.Hide(this);
        else
            tooltip.Show(under.Message, this, e.X, e.Y + Font.Height + LogicalToDeviceUnits(4), 5000);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hovered = null;
        tooltip.Hide(this);
    }

    /// <summary>The warning whose underlined text is under <paramref name="point"/>, if any.</summary>
    private TextWarning? WarningAt(Point point)
    {
        if (warnings.Count == 0 || TextLength == 0) return null;

        int index = GetCharIndexFromPosition(point);
        Point charTop = GetPositionFromCharIndex(index);
        // GetCharIndexFromPosition picks the nearest character even far away from it, so check the pointer is on its line
        if (point.Y < charTop.Y || point.Y > charTop.Y + Font.Height) return null;

        return warnings.FirstOrDefault(w => index >= w.Start && index < w.Start + w.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) tooltip.Dispose();
        base.Dispose(disposing);
    }
}
