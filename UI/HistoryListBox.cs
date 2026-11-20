using System.ComponentModel;
using PostcodeDistanceCalculator.Helpers;
using PostcodeDistanceCalculator.Models;

namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// The saved-history list: each batch is a collapsible header row followed by its journeys, newest batch first.
/// Every row has a delete cross, and journeys with possible postcode typos are underlined.
/// </summary>
internal sealed class HistoryListBox : ListBox
{
    public sealed record JourneyRow(JourneyBatch Batch, Journey Journey);

    private static readonly Color HeaderBackColor = Color.FromArgb(226, 232, 240);

    private readonly HashSet<JourneyBatch> collapsedBatches = [];
    private readonly ToolTip warningTip = new();
    private int warningTipIndex = -1;
    private DistanceUnit unit;

    /// <summary>Double-click or Enter on a single row.</summary>
    public event EventHandler? LoadRequested;

    /// <summary>A row's cross was clicked, or Delete was pressed. The handler removes the items and refreshes the list.</summary>
    public event EventHandler<IReadOnlyList<object>>? DeleteRequested;

    public HistoryListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        SelectionMode = SelectionMode.MultiExtended;
        IntegralHeight = false;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DistanceUnit Unit
    {
        get => unit;
        set
        {
            unit = value;
            Invalidate();
        }
    }

    /// <summary>The typo warning for a normalised postcode, or null if it looks fine.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, string?> WarningFor { get; set; } = _ => null;

    public void ShowBatches(IEnumerable<JourneyBatch> batches)
    {
        var batchList = batches.ToList();
        BeginUpdate();
        Items.Clear();
        foreach (var batch in batchList.OrderByDescending(b => b.CreatedAt))
        {
            Items.Add(batch);
            if (collapsedBatches.Contains(batch)) continue;
            foreach (var journey in batch.Journeys)
                Items.Add(new JourneyRow(batch, journey));
        }
        EndUpdate();

        collapsedBatches.IntersectWith(batchList);
    }

    /// <summary>
    /// Every journey in the selected rows, in list order, each once. A selected batch header stands for all its
    /// journeys, including any hidden while it's collapsed.
    /// </summary>
    public List<Journey> SelectedJourneys()
        => SelectedItems.Cast<object>().SelectMany(JourneysOf).Distinct(ReferenceEqualityComparer.Instance).Cast<Journey>().ToList();

    /// <summary>The journeys a row stands for: a copy of a batch's list, or the one journey.</summary>
    public static IReadOnlyList<Journey> JourneysOf(object item) => item switch
    {
        JourneyBatch batch => [.. batch.Journeys],
        JourneyRow row => [row.Journey],
        _ => []
    };

    public void SelectOnly(int index)
    {
        ClearSelected();
        if (index >= 0 && index < Items.Count) SetSelected(index, true);
    }

    #region Drawing

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count) return;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool plainColors = isSelected || SystemInformation.HighContrast;
        object item = Items[e.Index];

        // High contrast must use system colours, so no shaded header band there
        if (item is JourneyBatch && !plainColors)
        {
            using var headerBrush = new SolidBrush(HeaderBackColor);
            e.Graphics.FillRectangle(headerBrush, e.Bounds);
        }
        else
        {
            e.DrawBackground();
        }

        Color textColor = e.ForeColor;
        Color mutedColor = plainColors ? e.ForeColor : SystemColors.GrayText;
        Color deleteColor = plainColors ? e.ForeColor : Color.Crimson;
        Font font = e.Font ?? SystemFonts.DefaultFont;

        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        Rectangle deleteBounds = DeleteButtonBounds(e.Bounds);
        int padding = LogicalToDeviceUnits(5);

        Rectangle TextBounds(int left) => Rectangle.FromLTRB(left, e.Bounds.Top, Math.Max(left, deleteBounds.Left - padding), e.Bounds.Bottom);

        if (item is JourneyBatch batch)
        {
            Rectangle chevron = ChevronBounds(e.Bounds);
            var state = e.Graphics.Save();
            if (!collapsedBatches.Contains(batch))
            {
                e.Graphics.TranslateTransform(chevron.Left + chevron.Width / 2f, chevron.Top + chevron.Height / 2f);
                e.Graphics.RotateTransform(90);
                e.Graphics.TranslateTransform(-(chevron.Left + chevron.Width / 2f), -(chevron.Top + chevron.Height / 2f));
            }
            IconFont.Draw(e.Graphics, MaterialIcon.ChevronRight, chevron, textColor);
            e.Graphics.Restore(state);

            using var boldFont = new Font(font, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, BatchText(batch), boldFont, TextBounds(chevron.Right + padding / 2), textColor, flags);
        }
        else if (item is JourneyRow row)
        {
            int left = e.Bounds.Left + LogicalToDeviceUnits(20);

            if (!string.IsNullOrWhiteSpace(row.Journey.DateTag))
            {
                string dateText = $"[{row.Journey.DateTag}]";
                TextRenderer.DrawText(e.Graphics, dateText, font, TextBounds(left), mutedColor, flags);
                left += MeasureWidth(e.Graphics, dateText, font) + padding;
            }

            // Distance pinned to the right, so widening the panel shows more of the route
            string distance = DistanceText(row.Journey);
            int distanceWidth = MeasureWidth(e.Graphics, distance, font);
            Rectangle distanceBounds = Rectangle.FromLTRB(Math.Max(left, deleteBounds.Left - padding - distanceWidth), e.Bounds.Top, deleteBounds.Left - padding, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, distance, font, distanceBounds, textColor, flags | TextFormatFlags.Right);

            Rectangle routeBounds = Rectangle.FromLTRB(left, e.Bounds.Top, Math.Max(left, distanceBounds.Left - padding), e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, RouteText(row.Journey), font, routeBounds, textColor, flags);
            UnderlineFlaggedStops(e.Graphics, row.Journey, font, routeBounds, plainColors ? e.ForeColor : WarningTextBox.WarningColor);
        }

        IconFont.Draw(e.Graphics, MaterialIcon.Close, deleteBounds, deleteColor);
        e.DrawFocusRectangle();
    }

    // Measured without EndEllipsis/VerticalCenter: with no proposed size they make the measured width collapse
    private static int MeasureWidth(IDeviceContext graphics, string text, Font font)
        => TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;

    /// <summary>Wavy underlines under the stops that may be typos, where visible (the route is cut short with "..." when narrow).</summary>
    private void UnderlineFlaggedStops(Graphics graphics, Journey journey, Font font, Rectangle routeBounds, Color color)
    {
        const TextFormatFlags measure = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        // DrawText without NoPadding indents the text slightly; measure that to line up with it
        int indent = (MeasureWidth(graphics, "W", font) - TextRenderer.MeasureText(graphics, "W", font, Size.Empty, measure).Width) / 2;
        int amplitude = Math.Max(1, LogicalToDeviceUnits(1));
        int baseline = routeBounds.Top + (routeBounds.Height + font.Height) / 2 - amplitude;
        using var pen = new Pen(color, Math.Max(1, LogicalToDeviceUnits(1)));

        string prefix = "";
        foreach (string stop in journey.Stops)
        {
            string shown = Postcode.Format(stop);
            if (WarningFor(stop) != null)
            {
                int start = routeBounds.Left + indent + (prefix.Length == 0 ? 0 : TextRenderer.MeasureText(graphics, prefix, font, Size.Empty, measure).Width);
                int end = Math.Min(start + TextRenderer.MeasureText(graphics, shown, font, Size.Empty, measure).Width, routeBounds.Right);
                if (end - start > amplitude * 4)
                    WarningTextBox.DrawSquiggle(graphics, pen, start, end, baseline, amplitude, LogicalToDeviceUnits(2));
            }
            prefix += shown + " -> ";
        }
    }

    private Rectangle DeleteButtonBounds(Rectangle itemBounds)
    {
        int size = itemBounds.Height - LogicalToDeviceUnits(8);
        int right = itemBounds.Right - LogicalToDeviceUnits(4);
        return new Rectangle(right - size, itemBounds.Top + (itemBounds.Height - size) / 2, size, size);
    }

    private Rectangle ChevronBounds(Rectangle itemBounds)
    {
        int size = itemBounds.Height - LogicalToDeviceUnits(6);
        int left = itemBounds.Left + LogicalToDeviceUnits(3);
        return new Rectangle(left, itemBounds.Top + (itemBounds.Height - size) / 2, size, size);
    }

    // Owner-drawn rows only repaint the newly exposed strip on resize, so redraw them all to reflow the route text
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        Invalidate();
    }

    #endregion

    #region Text

    private string BatchText(JourneyBatch batch)
        => $"{batch.CreatedAt:dd/MM/yyyy HH:mm}  ({BreakdownFormatter.JourneyCount(batch.Journeys.Count)}, {Unit.FormatNumber(batch.TotalKm)} {Unit.ShortName()})";

    private static string RouteText(Journey journey) => string.Join(" -> ", journey.Stops.Select(Postcode.Format));

    private string DistanceText(Journey journey)
        => $"{Unit.FormatNumber(journey.TotalKm)} {Unit.ShortName()}{(journey.IsVerified ? " (Google)" : "")}";

    private List<string> JourneyWarnings(Journey journey)
        => journey.Stops.Distinct()
            .Select(stop => (stop, message: WarningFor(stop)))
            .Where(w => w.message != null)
            .Select(w => $"{Postcode.Format(w.stop)}: {w.message}")
            .ToList();

    // Rows are owner-drawn, so without this screen readers would announce them by their type names
    protected override void OnFormat(ListControlConvertEventArgs e)
    {
        e.Value = e.ListItem switch
        {
            JourneyBatch batch => $"Batch {BatchText(batch)}{(collapsedBatches.Contains(batch) ? ", collapsed" : "")}",
            JourneyRow { Journey: var journey } => (string.IsNullOrWhiteSpace(journey.DateTag) ? "" : $"[{journey.DateTag}] ") +
                $"{RouteText(journey)} ({DistanceText(journey)})" +
                (JourneyWarnings(journey) is { Count: > 0 } w ? $", check postcodes: {string.Join("; ", w)}" : ""),
            _ => e.Value
        };
        base.OnFormat(e);
    }

    #endregion

    #region Mouse and keyboard

    private static bool IsMultiSelecting => (ModifierKeys & (Keys.Control | Keys.Shift)) != 0;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        int index = IndexFromPoint(e.Location);

        // Right-click on a selected row keeps the selection so the menu acts on all of it; elsewhere it selects
        // just that row, or nothing on empty space so no menu opens
        if (e.Button == MouseButtons.Right)
        {
            if (index == NoMatches || !SelectedIndices.Contains(index))
                SelectOnly(index == NoMatches ? -1 : index);
            return;
        }

        // The second press of a double-click would otherwise delete the row that moved up under the cursor
        if (e.Button != MouseButtons.Left || e.Clicks > 1 || IsMultiSelecting || index == NoMatches) return;

        Rectangle bounds = GetItemRectangle(index);
        if (DeleteButtonBounds(bounds).Contains(e.Location))
            DeleteRequested?.Invoke(this, [Items[index]]);
        else if (Items[index] is JourneyBatch batch && ChevronBounds(bounds).Contains(e.Location))
            ToggleCollapsed(batch);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        int index = IndexFromPoint(e.Location);
        if (e.Button != MouseButtons.Left || index == NoMatches) return;

        Rectangle bounds = GetItemRectangle(index);
        if (DeleteButtonBounds(bounds).Contains(e.Location) || (Items[index] is JourneyBatch && ChevronBounds(bounds).Contains(e.Location)))
            return;

        SelectOnly(index);
        LoadRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode is Keys.Left or Keys.Right && SelectedIndices.Count == 1 && SelectedItem is JourneyBatch batch)
        {
            e.Handled = true;
            if (collapsedBatches.Contains(batch) == (e.KeyCode == Keys.Right))
                ToggleCollapsed(batch);
        }
        else if (e.KeyCode == Keys.Enter && SelectedIndices.Count == 1)
        {
            e.Handled = true;
            LoadRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.KeyCode == Keys.Delete && SelectedItems.Count > 0)
        {
            // Select the row that took the deleted rows' place, so rows can be deleted one after another
            e.Handled = true;
            int index = SelectedIndex;
            DeleteRequested?.Invoke(this, SelectedItems.Cast<object>().ToList());
            SelectOnly(Math.Min(index, Items.Count - 1));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int index = IndexFromPoint(e.Location);
        if (index == warningTipIndex) return;

        HideWarningTip();
        if (index == NoMatches || Items[index] is not JourneyRow row) return;

        var warnings = JourneyWarnings(row.Journey);
        if (warnings.Count == 0) return;

        warningTipIndex = index;
        Rectangle bounds = GetItemRectangle(index);
        warningTip.Show(string.Join("\n", warnings), this, bounds.Left + LogicalToDeviceUnits(20), bounds.Bottom + 2, 5000);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        HideWarningTip();
    }

    private void HideWarningTip()
    {
        warningTipIndex = -1;
        warningTip.Hide(this);
    }

    private void ToggleCollapsed(JourneyBatch batch)
    {
        if (!collapsedBatches.Remove(batch)) collapsedBatches.Add(batch);
        var batches = Items.OfType<JourneyBatch>().ToList();
        ShowBatches(batches);
        SelectOnly(Items.IndexOf(batch));
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing) warningTip.Dispose();
        base.Dispose(disposing);
    }
}
