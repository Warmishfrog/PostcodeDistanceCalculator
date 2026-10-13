using System.Runtime.InteropServices;
using PostcodeDistanceCalculator.Extensions;
using PostcodeDistanceCalculator.Helpers;
using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// Main window: postcode input, journey breakdown, and the saved-history sidebar.
/// Parsing, formatting, API calls and persistence live in <see cref="JourneyParser"/>,
/// <see cref="BreakdownFormatter"/>, <see cref="RouteApi"/> and <see cref="HistoryStore"/>;
/// this class only wires them to the UI.
/// </summary>
public partial class PostcodeDistanceCalculator : Form
{
    /// <summary>A journey row in the history list, linked to the batch it belongs to.</summary>
    private sealed record JourneyRow(JourneyBatch Batch, Journey Journey);

    private readonly string historyPath = HistoryStore.DefaultPath;

    /// <summary>Saved batches, oldest first (displayed newest first). Persisted on every change.</summary>
    private List<JourneyBatch> history = [];

    /// <summary>Journeys currently shown in the breakdown (freshly calculated or loaded from history).</summary>
    private IReadOnlyList<Journey> shownJourneys = [];

    /// <summary>The last calculation, waiting to be saved. Null when the shown journeys are already in history.</summary>
    private JourneyBatch? unsavedBatch;

    /// <summary>
    /// Set when history.json exists but couldn't be loaded. Saving is then disabled for the session,
    /// because writing the (empty) in-memory history would overwrite the user's real saved history.
    /// </summary>
    private bool historySaveBlocked;

    /// <summary>Set while code (not the user) changes the history selection, so the change doesn't load that row.</summary>
    private bool suppressHistoryLoad;

    /// <summary>
    /// Distinct date tags of the batch most recently loaded from history, when its journeys had different dates.
    /// Recalculating would give every journey the single date in the Date box, so the user is warned first.
    /// </summary>
    private string[]? loadedMixedDates;

    /// <summary>Brief popup confirming exactly what was copied to the clipboard.</summary>
    private readonly ToolTip copyToast;

    // Light slate used for batch headers and the enabled Save button. The Designer's light buttons use the same
    // value as a literal (generated code can't reference constants), so change both together.
    private static readonly Color LightSlateColor = Color.FromArgb(226, 232, 240);
    private static readonly Color SaveButtonForeColor = Color.FromArgb(51, 65, 85);
    private static readonly Color SaveButtonDisabledBackColor = Color.FromArgb(148, 163, 184);
    private static readonly Color SaveButtonDisabledForeColor = Color.FromArgb(71, 85, 105);

    /// <summary>The app version from the csproj <c>&lt;Version&gt;</c>, e.g. "2.0.0".</summary>
    private static string AppVersion => typeof(PostcodeDistanceCalculator).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    private DistanceUnit Unit => rdoMiles.Checked ? DistanceUnit.Miles : DistanceUnit.Kilometers;

    private double ShownTotalKm => shownJourneys.Sum(j => j.TotalKm);
    private double HistoryTotalKm => history.Sum(b => b.TotalKm);

    public PostcodeDistanceCalculator()
    {
        InitializeComponent();

        copyToast = new ToolTip(components!); // components is created by InitializeComponent and disposes the toast with the form

        // Optional date: the hint shows today's date in the expected format
        txtDate.PlaceholderText = DateTime.Now.ToString("dd/MM");

        // Use the exe's own icon (csproj ApplicationIcon) for the window and taskbar instead of the WinForms default
        Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? Icon;

        this.Load += PostcodeDistanceCalculator_Load;
        // Control.Dispose doesn't dispose assigned images, so free the rendered icons with the form
        this.Disposed += (_, _) => DisposeIcons();

        btnAddNew.Enabled = false;
    }

    /// <summary>
    /// Re-renders after the window moves to a monitor with a different scale. The DpiChanged event fires
    /// before child controls are rescaled, so this runs after base.OnDpiChanged, once they have their new DPI.
    /// </summary>
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        RefreshScaledVisuals();
    }

    /// <summary>Re-renders icons when Windows colours change, e.g. a high-contrast theme is switched on or off.</summary>
    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        RefreshScaledVisuals();
        lstHistory.Invalidate();
    }

    /// <summary>
    /// Sizes the owner-drawn history rows and renders the Material icons for the current DPI and colour scheme.
    /// Runs on load, after DPI changes and after system colour changes.
    /// </summary>
    private void RefreshScaledVisuals()
    {
        lstHistory.ItemHeight = lstHistory.LogicalToDeviceUnits(26);

        SetIcon(btnHelp, MaterialIcon.Help, 16);
        SetIcon(btnCopyJourneyTotal, MaterialIcon.ContentCopy, 16);
        SetIcon(btnCopyHistoryTotal, MaterialIcon.ContentCopy, 16);
        SetIcon(btnReopenHistory, MaterialIcon.History, 16);
        SetIcon(btnToggleHistory, MaterialIcon.ChevronRight, 20);

        lblInputInfo.Image?.Dispose();
        lblInputInfo.Image = IconFont.Render(MaterialIcon.Info, lblInputInfo.LogicalToDeviceUnits(18), IconColor(lblInputInfo));
    }

    /// <summary>Replaces <paramref name="button"/>'s image with <paramref name="icon"/>, <paramref name="logicalSize"/> pixels at 100% scale.</summary>
    private static void SetIcon(ButtonBase button, MaterialIcon icon, int logicalSize)
    {
        button.Image?.Dispose();
        button.Image = IconFont.Render(icon, button.LogicalToDeviceUnits(logicalSize), IconColor(button));
    }

    /// <summary>The control's text colour, or the system text colour in high contrast so icons stay visible.</summary>
    private static Color IconColor(Control control)
        => SystemInformation.HighContrast ? SystemColors.ControlText : control.ForeColor;

    private void DisposeIcons()
    {
        foreach (var button in new ButtonBase[] { btnHelp, btnCopyJourneyTotal, btnCopyHistoryTotal, btnReopenHistory, btnToggleHistory })
            button.Image?.Dispose();
        lblInputInfo.Image?.Dispose();
    }

    private void PostcodeDistanceCalculator_Load(object? sender, EventArgs e)
    {
        // Here rather than the constructor so DeviceDpi reflects the monitor the window opens on
        RefreshScaledVisuals();

        try
        {
            history = HistoryStore.Load(historyPath, out string? corruptBackupPath);
            if (corruptBackupPath != null)
            {
                MessageBox.Show(
                    $"The saved history file couldn't be read, so it was set aside as:\n{corruptBackupPath}\n\nStarting with empty history.",
                    "History Unreadable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The file is still there (e.g. locked by OneDrive or antivirus), so never overwrite it this session
            historySaveBlocked = true;
            MessageBox.Show(
                $"Couldn't open saved history at {historyPath}:\n{ex.Message}\n\n" +
                "To protect it, nothing will be saved to history this session. Close and reopen the app to try again.",
                "History Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        RefreshHistoryList();
    }

    private async void BtnCalculate_Click(object sender, EventArgs e)
    {
        string rawInput = txtPostcodes.Text;

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            MessageBox.Show("Please enter postcodes separated by commas.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Recalculating a reloaded batch applies the one Date box value to every journey
        if (loadedMixedDates != null)
        {
            string newDate = string.IsNullOrWhiteSpace(txtDate.Text) ? "no date" : $"'{txtDate.Text.Trim()}'";
            if (MessageBox.Show(
                    $"The loaded batch had different dates per journey ({string.Join(", ", loadedMixedDates.Select(d => d.Length == 0 ? "none" : d))}).\n\n" +
                    $"Recalculating will give every journey {newDate}. Continue?",
                    "Journey Dates Will Change", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            loadedMixedDates = null;
        }

        var parsed = JourneyParser.Parse(rawInput);
        if (parsed.Routes.Count == 0)
        {
            // Clear the previous results so they aren't left showing (and copyable) next to the error
            ShowJourneys([], savable: null);
            MessageBox.Show("No valid journeys found. Please ensure each route has at least two postcodes separated by commas.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            lblStatus.Text = "No valid journeys.";
            return;
        }

        // Lock input & history so nothing can touch journey state while requests are in flight
        splitContainerLeft.Panel1.Enabled = false;
        splitContainer.Panel2.Enabled = false;
        btnAddNew.Enabled = false;
        lblStatus.Text = "Fetching coordinates...";
        ShowJourneys([], savable: null);

        try
        {
            // Leave date string empty if user provided nothing
            string dateTag = txtDate.Text.Trim();

            // Built locally and only shown on full success, so a failure can't leave a partial batch
            var journeys = new List<Journey>();
            var coords = new Dictionary<string, (double Lat, double Lon)>();

            foreach (var route in parsed.Routes)
            {
                int journeyNum = journeys.Count + 1;

                foreach (var code in route.Postcodes.Where(c => !coords.ContainsKey(c)).Distinct())
                {
                    lblStatus.Text = $"Locating {code}...";
                    coords[code] = await RouteApi.GetCoordinatesAsync(code);
                }

                var legs = new List<Leg>();
                for (int i = 0; i < route.Postcodes.Count - 1; i++)
                {
                    string start = route.Postcodes[i];
                    string end = route.Postcodes[i + 1];

                    lblStatus.Text = $"Journey {journeyNum}: Routing {start} -> {end}...";
                    double meters = await RouteApi.GetDrivingDistanceMetersAsync(coords[start], coords[end]) * RouteApi.CalibrationFactor;
                    legs.Add(new Leg(start, end, meters));
                }

                journeys.Add(new Journey(dateTag, route.RawInput, legs));
            }

            ShowJourneys(journeys, savable: new JourneyBatch { CreatedAt = DateTime.Now, Journeys = journeys });

            lblStatus.Text = journeys.Count > 1
                ? $"Calculation complete for {journeys.Count} journeys!"
                : "Calculation Complete!";

            if (parsed.Skipped.Count > 0)
            {
                MessageBox.Show(
                    "These routes were skipped because they have fewer than two postcodes:\n\n" + string.Join("\n", parsed.Skipped.Select(s => $"• {s}")),
                    "Some Routes Skipped", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"Failed. {ex.Message}";
        }
        finally
        {
            splitContainerLeft.Panel1.Enabled = true;
            splitContainer.Panel2.Enabled = true;
        }
    }

    /// <summary>
    /// Displays <paramref name="journeys"/> in the breakdown. <paramref name="savable"/> is the batch that
    /// "Save to History" would add, or null when there's nothing new to save.
    /// </summary>
    private void ShowJourneys(IReadOnlyList<Journey> journeys, JourneyBatch? savable)
    {
        shownJourneys = journeys;
        unsavedBatch = savable;
        RenderResults();
        btnAddNew.Enabled = savable != null;
    }

    private void RenderResults()
    {
        txtResults.Text = BreakdownFormatter.Format(shownJourneys, Unit);

        string title = shownJourneys.Count > 1 ? "Combined" : "Journey";
        lblJourneyTotal.Text = $"{title} Total: {Unit.FormatNumber(ShownTotalKm)} {Unit.ShortName()}";
    }

    #region History List

    /// <summary>Rebuilds the history list: each batch is a header row followed by its journeys, newest batch first.</summary>
    private void RefreshHistoryList()
    {
        lstHistory.BeginUpdate();
        lstHistory.Items.Clear();
        foreach (var batch in history.OrderByDescending(b => b.CreatedAt))
        {
            lstHistory.Items.Add(batch);
            foreach (var journey in batch.Journeys)
                lstHistory.Items.Add(new JourneyRow(batch, journey));
        }
        lstHistory.EndUpdate();

        RecalculateOverallTotal();
    }

    private void LstHistory_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstHistory.Items.Count) return;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool highContrast = SystemInformation.HighContrast;
        object item = lstHistory.Items[e.Index];

        // Batch headers get a shaded band so groups stand out (not in high contrast, which must use system colours)
        if (item is JourneyBatch && !isSelected && !highContrast)
        {
            using var headerBrush = new SolidBrush(LightSlateColor);
            e.Graphics.FillRectangle(headerBrush, e.Bounds);
        }
        else
        {
            e.DrawBackground();
        }

        // e.ForeColor is the system text colour, or the highlight text colour when the row is selected
        Color textColor = e.ForeColor;
        Color mutedColor = isSelected || highContrast ? e.ForeColor : SystemColors.GrayText;
        Color deleteColor = isSelected || highContrast ? e.ForeColor : Color.Crimson;
        Font font = e.Font ?? SystemFonts.DefaultFont;

        const TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        Rectangle deleteBounds = GetDeleteButtonBounds(e.Bounds);
        int padding = lstHistory.LogicalToDeviceUnits(5);

        // Text runs from 'left' up to the delete button, vertically centred in the row
        Rectangle TextBounds(int left) => Rectangle.FromLTRB(left, e.Bounds.Top, Math.Max(left, deleteBounds.Left - padding), e.Bounds.Bottom);

        if (item is JourneyBatch batch)
        {
            using var boldFont = new Font(font, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, BatchText(batch), boldFont, TextBounds(e.Bounds.Left + padding), textColor, flags);
        }
        else if (item is JourneyRow row)
        {
            // Journeys are indented under their batch header
            int left = e.Bounds.Left + lstHistory.LogicalToDeviceUnits(20);

            // Draw date only if present, pushing the text past however wide the date is
            if (!string.IsNullOrWhiteSpace(row.Journey.DateTag))
            {
                string dateText = $"[{row.Journey.DateTag}]";
                TextRenderer.DrawText(e.Graphics, dateText, font, TextBounds(left), mutedColor, flags);
                // Measure without EndEllipsis/VerticalCenter: with no proposed size they make the measured width collapse
                left += TextRenderer.MeasureText(e.Graphics, dateText, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + padding;
            }

            TextRenderer.DrawText(e.Graphics, JourneyText(row.Journey), font, TextBounds(left), textColor, flags);
        }

        IconFont.Draw(e.Graphics, MaterialIcon.Close, deleteBounds, deleteColor);

        e.DrawFocusRectangle();
    }

    private string BatchText(JourneyBatch batch)
        => $"{batch.CreatedAt:dd/MM/yyyy HH:mm}  ({JourneyCount(batch.Journeys.Count)}, {Unit.FormatNumber(batch.TotalKm)} {Unit.ShortName()})";

    private string JourneyText(Journey journey)
        => $"{journey.FirstPostcode}... ({Unit.FormatNumber(journey.TotalKm)} {Unit.ShortName()})";

    /// <summary>
    /// Text for each history row as screen readers (and ListBox.GetItemText) see it. The rows are owner-drawn,
    /// so without this they'd be announced by their type names.
    /// </summary>
    private void LstHistory_Format(object sender, ListControlConvertEventArgs e)
    {
        e.Value = e.ListItem switch
        {
            JourneyBatch batch => $"Batch {BatchText(batch)}",
            JourneyRow { Journey: var journey } when !string.IsNullOrWhiteSpace(journey.DateTag) => $"[{journey.DateTag}] {JourneyText(journey)}",
            JourneyRow { Journey: var journey } => JourneyText(journey),
            _ => e.Value
        };
    }

    private void LstHistory_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (suppressHistoryLoad) return;

        // The ListBox selects on mouse down before MouseDown fires, so a click on the delete button would load
        // the row (clobbering the user's input) just before deleting it. Mouse selections are handled in
        // LstHistory_MouseDown instead; only keyboard selections load from here.
        if (Control.MouseButtons != MouseButtons.None) return;

        if (lstHistory.SelectedItem is { } item)
            LoadHistoryItem(item);
    }

    private void LstHistory_MouseDown(object sender, MouseEventArgs e)
    {
        // Left single-clicks only: the second press of a double-click would otherwise delete the row that moved up under the cursor
        if (e.Button != MouseButtons.Left || e.Clicks > 1) return;

        int index = DeleteButtonHitIndex(e.Location);
        if (index != -1)
        {
            DeleteHistoryItem(lstHistory.Items[index]);
        }
        else if (lstHistory.IndexFromPoint(e.Location) == lstHistory.SelectedIndex && lstHistory.SelectedItem is { } item)
        {
            // Index check: clicking empty space below the rows must not reload the still-selected row
            LoadHistoryItem(item);
        }
    }

    /// <summary>Shows a saved batch (all its journeys) or a single saved journey in the breakdown.</summary>
    private void LoadHistoryItem(object item)
    {
        IReadOnlyList<Journey> journeys = item switch
        {
            JourneyBatch batch => [.. batch.Journeys], // Copy so deleting from history doesn't change what's shown
            JourneyRow row => [row.Journey],
            _ => []
        };
        if (journeys.Count == 0) return;

        txtPostcodes.Text = string.Join(" ; ", journeys.Select(j => j.RawInput));

        // Restore the date only when every journey shares it (empty string if none had one).
        // Otherwise remember the dates so recalculating can warn that they'll be replaced.
        string[] dates = journeys.Select(j => j.DateTag).Distinct().ToArray();
        txtDate.Text = dates.Length == 1 ? dates[0] : string.Empty;
        loadedMixedDates = dates.Length > 1 ? dates : null;

        // Already in history, so nothing to save. Set after the text changes above, which also disable Save.
        ShowJourneys(journeys, savable: null);

        lblStatus.Text = item is JourneyBatch b
            ? $"Loaded batch from {b.CreatedAt:dd/MM/yyyy HH:mm} ({JourneyCount(journeys.Count)})."
            : "Loaded journey from history.";
    }

    /// <summary>
    /// Deletes a whole batch (with confirmation if it holds several journeys) or a single journey.
    /// Returns false if the user cancelled.
    /// </summary>
    private bool DeleteHistoryItem(object item)
    {
        if (item is JourneyBatch batch)
        {
            if (batch.Journeys.Count > 1 &&
                MessageBox.Show($"Delete this batch of {batch.Journeys.Count} journeys from {batch.CreatedAt:dd/MM/yyyy}?",
                    "Delete Batch", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return false;

            history.Remove(batch);
            lblStatus.Text = "Removed batch from history.";
        }
        else if (item is JourneyRow row)
        {
            row.Batch.Journeys.Remove(row.Journey);
            if (row.Batch.Journeys.Count == 0) history.Remove(row.Batch);
            lblStatus.Text = "Removed journey from history.";
        }

        SaveHistory();
        RefreshHistoryList();
        return true;
    }

    private void BtnClearHistory_Click(object sender, EventArgs e)
    {
        if (history.Count == 0) return;

        int journeyCount = history.Sum(b => b.Journeys.Count);
        if (MessageBox.Show($"Delete all {journeyCount} saved journeys? This can't be undone.\n\nTip: copy the History Total first if you still need it.",
                "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        history.Clear();
        lblStatus.Text = "History cleared.";
        SaveHistory(); // Sets its own status if saving is blocked, so it runs after the success message
        RefreshHistoryList();
    }

    /// <summary>Index of the history row whose delete button is at <paramref name="location"/>, or -1.</summary>
    private int DeleteButtonHitIndex(Point location)
    {
        int index = lstHistory.IndexFromPoint(location);
        if (index == ListBox.NoMatches) return -1;

        return GetDeleteButtonBounds(lstHistory.GetItemRectangle(index)).Contains(location) ? index : -1;
    }

    private static string JourneyCount(int count) => count == 1 ? "1 journey" : $"{count} journeys";

    /// <summary>The square delete icon at the right of a history row, sized from the row height so it follows DPI.</summary>
    private Rectangle GetDeleteButtonBounds(Rectangle itemBounds)
    {
        int size = itemBounds.Height - lstHistory.LogicalToDeviceUnits(8);
        int right = itemBounds.Right - lstHistory.LogicalToDeviceUnits(4);
        return new Rectangle(right - size, itemBounds.Top + (itemBounds.Height - size) / 2, size, size);
    }

    /// <summary>
    /// Delete removes the selected history row, the same as clicking its cross, then selects the row that
    /// took its place (without loading it) so several rows can be deleted in a row from the keyboard.
    /// </summary>
    private void LstHistory_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete || lstHistory.SelectedItem is not { } item) return;

        e.Handled = true;
        int index = lstHistory.SelectedIndex;
        if (!DeleteHistoryItem(item) || lstHistory.Items.Count == 0) return;

        suppressHistoryLoad = true;
        try
        {
            lstHistory.SelectedIndex = Math.Min(index, lstHistory.Items.Count - 1);
        }
        finally
        {
            suppressHistoryLoad = false;
        }
    }

    private void RecalculateOverallTotal()
        => lblHistoryTotal.Text = $"History Total: {Unit.FormatNumber(HistoryTotalKm)} {Unit.ShortName()}";

    /// <summary>Writes history to disk. On failure the in-memory history is kept and the user is told why.</summary>
    private void SaveHistory()
    {
        if (historySaveBlocked)
        {
            lblStatus.Text = "History not saved: the existing history file couldn't be loaded. Restart the app to retry.";
            return;
        }

        try
        {
            HistoryStore.Save(historyPath, history);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Couldn't save history to {historyPath}:\n{ex.Message}\n\nYour journeys are kept until the app closes. " +
                "Move the app to a folder you can write to (not Program Files) to keep history between sessions.",
                "History Not Saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    #endregion

    private void BtnAddNew_Click(object sender, EventArgs e)
    {
        if (unsavedBatch != null)
        {
            history.Add(unsavedBatch);
            lblStatus.Text = unsavedBatch.Journeys.Count > 1
                ? $"Saved {unsavedBatch.Journeys.Count} journeys to history & cleared."
                : "Saved to history & cleared.";

            SaveHistory(); // Sets its own status if saving is blocked, so it runs after the success message
            RefreshHistoryList();
        }

        txtPostcodes.Clear();
        txtDate.Clear();
        ShowJourneys([], savable: null);
    }

    private void UnitToggle_CheckedChanged(object sender, EventArgs e)
    {
        // Both radios are wired; only react to the one becoming checked
        if (sender is RadioButton { Checked: false }) return;

        RenderResults();
        lstHistory.Invalidate();
        RecalculateOverallTotal();
    }

    private void BtnToggleHistory_Click(object sender, EventArgs e)
    {
        // Collapsing Panel2 keeps SplitterDistance, so reopening restores wherever the user left the splitter
        splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;

        btnReopenHistory.Visible = splitContainer.Panel2Collapsed;
        if (btnReopenHistory.Visible) btnReopenHistory.BringToFront();
    }

    #region Copy Event Handlers
    private void BtnCopyJourneyTotal_Click(object sender, EventArgs e) => CopyDistance(ShownTotalKm, btnCopyJourneyTotal);
    private void LblJourneyTotal_Click(object? sender, EventArgs e) => CopyDistance(ShownTotalKm, btnCopyJourneyTotal);

    private void BtnCopyHistoryTotal_Click(object sender, EventArgs e) => CopyDistance(HistoryTotalKm, btnCopyHistoryTotal);
    private void LblHistoryTotal_Click(object? sender, EventArgs e) => CopyDistance(HistoryTotalKm, btnCopyHistoryTotal);

    /// <summary>
    /// Copies just the number (e.g. "123.4", no label or unit) so it pastes cleanly into a spreadsheet,
    /// then pops a toast above <paramref name="anchor"/> showing exactly what was copied.
    /// </summary>
    private void CopyDistance(double km, Control anchor)
    {
        string text = Unit.FormatNumber(km);

        try
        {
            Clipboard.SetText(text);
            string message = $"Copied {text} ({Unit.LongName()}) to clipboard";
            lblStatus.Text = message;
            copyToast.Show(message, anchor, 0, -anchor.Height - 4, 2000);
        }
        catch (ExternalException)
        {
            // Another process has the clipboard open
            lblStatus.Text = "Clipboard is busy in another app. Please try again.";
        }
    }
    #endregion

    /// <summary>Ctrl+Enter calculates. Plain Enter adds a new line, since postcodes can be entered one per line.</summary>
    private void TxtPostcodes_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && e.Control)
        {
            e.SuppressKeyPress = true;
            btnCalculate.PerformClick();
        }
    }

    /// <summary>Enter in the date box calculates. (Not a form AcceptButton, which would also fire from the history list.)</summary>
    private void TxtDate_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            btnCalculate.PerformClick();
        }
    }

    private void TxtPostcodes_TextChanged(object? sender, EventArgs e)
    {
        btnAddNew.Enabled = false;

        // Cleared input means the loaded batch is no longer being edited, so no date warning is needed
        if (txtPostcodes.TextLength == 0) loadedMixedDates = null;
    }

    private void BtnAddNew_EnabledChanged(object sender, EventArgs e)
    {
        // Flat buttons don't change colour when disabled, so darken it to show there's nothing to save
        btnAddNew.BackColor = btnAddNew.Enabled ? LightSlateColor : SaveButtonDisabledBackColor;
        btnAddNew.ForeColor = btnAddNew.Enabled ? SaveButtonForeColor : SaveButtonDisabledForeColor;
    }

    private void BtnHelp_Click(object sender, EventArgs e)
    {
        string helpText =
            $"POSTCODE DISTANCE CALCULATOR - HELP (version {AppVersion})\n" +
            "───────────────────────────────────────────────\n\n" +
            "1. SINGLE JOURNEY:\n" +
            "Enter postcodes separated by commas (or one per line), then click Calculate or press Ctrl+Enter.\n" +
            "• Example: SW1A 1AA, M1 1AE, EH1 1YZ\n\n" +
            "2. MULTIPLE JOURNEYS AT ONCE:\n" +
            "Separate individual journeys using a semicolon (;).\n" +
            "• Example: SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR ; CB2 1TN, NR1 1ER\n" +
            "• All journeys will render together in the Breakdown panel.\n\n" +
            "3. DATE FIELD:\n" +
            "• Optional. Tag journeys with a date (e.g. 24/09) if you're tracking specific days. The hint shows today's date.\n" +
            "• Every saved batch is automatically stamped with the date and time it was calculated.\n\n" +
            "4. HISTORY MANAGEMENT:\n" +
            "• '+ Save to History' saves the current journeys as one dated batch.\n" +
            "• History is saved to history.json next to the app, so it survives restarts and moves with the app folder.\n" +
            "• Click a batch header to load all its journeys, or a journey to load just that one.\n" +
            "• Click the cross on a row (or select it and press Delete) to remove it, or 'Clear' to delete all history (e.g. at month end).\n\n" +
            "5. QUICK COPY:\n" +
            "• Click a total or its Copy button to copy just the number (e.g. 123.4) for pasting into a spreadsheet.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
