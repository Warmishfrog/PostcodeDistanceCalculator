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

    /// <summary>Brief popup confirming exactly what was copied to the clipboard.</summary>
    private readonly ToolTip copyToast;

    private DistanceUnit Unit => rdoMiles.Checked ? DistanceUnit.Miles : DistanceUnit.Kilometers;

    public PostcodeDistanceCalculator()
    {
        InitializeComponent();

        copyToast = new ToolTip(components!); // components is created by InitializeComponent and disposes the toast with the form

        // Optional date: the hint shows today's date in the expected format
        txtDate.Text = string.Empty;
        txtDate.PlaceholderText = DateTime.Now.ToString("dd/MM");

        this.Load += PostcodeDistanceCalculator_Load;
        txtPostcodes.TextChanged += TxtPostcodes_TextChanged;

        // Make total labels interactive (hand cursor & click to copy)
        lblJourneyTotal.Cursor = Cursors.Hand;
        lblHistoryTotal.Cursor = Cursors.Hand;
        lblJourneyTotal.Click += LblJourneyTotal_Click;
        lblHistoryTotal.Click += LblHistoryTotal_Click;

        // Set explicit button text with copy icon
        btnCopyJourneyTotal.Text = "📋 Copy";
        btnCopyHistoryTotal.Text = "📋 Copy";

        btnAddNew.Enabled = false;
    }

    private void PostcodeDistanceCalculator_Load(object? sender, EventArgs e)
    {
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
            MessageBox.Show($"Couldn't open saved history at {historyPath}:\n{ex.Message}", "History Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        var parsed = JourneyParser.Parse(rawInput);
        if (parsed.Routes.Count == 0)
        {
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
        lblJourneyTotal.Text = $"{title} Total: {Unit.FormatNumber(shownJourneys.Sum(j => j.TotalKm))} {Unit.ShortName()}";
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
        object item = lstHistory.Items[e.Index];

        // Batch headers get a shaded band so groups stand out
        if (item is JourneyBatch && !isSelected)
        {
            using var headerBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
            e.Graphics.FillRectangle(headerBrush, e.Bounds);
        }
        else
        {
            e.DrawBackground();
        }

        Brush textBrush = isSelected ? Brushes.White : Brushes.Black;
        Brush dateBrush = isSelected ? Brushes.LightGray : Brushes.Gray;
        Font font = e.Font ?? SystemFonts.DefaultFont;
        int y = e.Bounds.Y + 4;

        if (item is JourneyBatch batch)
        {
            using var boldFont = new Font(font, FontStyle.Bold);
            e.Graphics.DrawString(
                $"{batch.CreatedAt:dd/MM/yyyy HH:mm}  ({JourneyCount(batch.Journeys.Count)},{Unit.FormatNumber(batch.TotalKm)} {Unit.ShortName()})",
                boldFont, textBrush, e.Bounds.X + 5, y);
        }
        else if (item is JourneyRow row)
        {
            // Journeys are indented under their batch header
            int textOffset = 20;

            // Draw date only if present, pushing the text past however wide the date is
            if (!string.IsNullOrWhiteSpace(row.Journey.DateTag))
            {
                string dateText = $"[{row.Journey.DateTag}]";
                e.Graphics.DrawString(dateText, font, dateBrush, e.Bounds.X + textOffset, y);
                textOffset += (int)Math.Ceiling(e.Graphics.MeasureString(dateText, font).Width) + 4;
            }

            e.Graphics.DrawString(
                $"{row.Journey.FirstPostcode}... ({Unit.FormatNumber(row.Journey.TotalKm)} {Unit.ShortName()})",
                font, textBrush, e.Bounds.X + textOffset, y);
        }

        Rectangle xRect = GetDeleteButtonBounds(e.Bounds);
        Brush xBrush = isSelected ? Brushes.White : Brushes.Crimson;
        e.Graphics.DrawString("✕", font, xBrush, xRect.X, xRect.Y);

        e.DrawFocusRectangle();
    }

    private void LstHistory_SelectedIndexChanged(object sender, EventArgs e)
    {
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
        // Restore the date only when every journey shares it (empty string if none had one)
        txtDate.Text = journeys.Select(j => j.DateTag).Distinct().Count() == 1 ? journeys[0].DateTag : string.Empty;

        // Already in history, so nothing to save. Set after the text changes above, which also disable Save.
        ShowJourneys(journeys, savable: null);

        lblStatus.Text = item is JourneyBatch b
            ? $"Loaded batch from {b.CreatedAt:dd/MM/yyyy HH:mm} ({JourneyCount(journeys.Count)})."
            : "Loaded journey from history.";
    }

    /// <summary>Deletes a whole batch (with confirmation if it holds several journeys) or a single journey.</summary>
    private void DeleteHistoryItem(object item)
    {
        if (item is JourneyBatch batch)
        {
            if (batch.Journeys.Count > 1 &&
                MessageBox.Show($"Delete this batch of {batch.Journeys.Count} journeys from {batch.CreatedAt:dd/MM/yyyy}?",
                    "Delete Batch", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

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
    }

    private void BtnClearHistory_Click(object sender, EventArgs e)
    {
        if (history.Count == 0) return;

        int journeyCount = history.Sum(b => b.Journeys.Count);
        if (MessageBox.Show($"Delete all {journeyCount} saved journeys? This can't be undone.\n\nTip: copy the History Total first if you still need it.",
                "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        history.Clear();
        SaveHistory();
        RefreshHistoryList();
        lblStatus.Text = "History cleared.";
    }

    /// <summary>Index of the history row whose delete button is at <paramref name="location"/>, or -1.</summary>
    private int DeleteButtonHitIndex(Point location)
    {
        int index = lstHistory.IndexFromPoint(location);
        if (index == ListBox.NoMatches) return -1;

        return GetDeleteButtonBounds(lstHistory.GetItemRectangle(index)).Contains(location) ? index : -1;
    }

    private static string JourneyCount(int count) => count == 1 ? "1 journey" : $"{count} journeys";

    private static Rectangle GetDeleteButtonBounds(Rectangle itemBounds)
        => new(itemBounds.Right - 22, itemBounds.Y + 4, 18, 18);

    private void RecalculateOverallTotal()
        => lblHistoryTotal.Text = $"History Total: {Unit.FormatNumber(history.Sum(b => b.TotalKm))} {Unit.ShortName()}";

    /// <summary>Writes history to disk. On failure the in-memory history is kept and the user is told why.</summary>
    private void SaveHistory()
    {
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
            SaveHistory();
            RefreshHistoryList();

            lblStatus.Text = unsavedBatch.Journeys.Count > 1
                ? $"Saved {unsavedBatch.Journeys.Count} journeys to history & cleared."
                : "Saved to history & cleared.";
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
    private void BtnCopyJourneyTotal_Click(object sender, EventArgs e) => CopyDistance(shownJourneys.Sum(j => j.TotalKm), btnCopyJourneyTotal);
    private void LblJourneyTotal_Click(object? sender, EventArgs e) => CopyDistance(shownJourneys.Sum(j => j.TotalKm), btnCopyJourneyTotal);

    private void BtnCopyHistoryTotal_Click(object sender, EventArgs e) => CopyDistance(history.Sum(b => b.TotalKm), btnCopyHistoryTotal);
    private void LblHistoryTotal_Click(object? sender, EventArgs e) => CopyDistance(history.Sum(b => b.TotalKm), btnCopyHistoryTotal);

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

    private void TxtPostcodes_TextChanged(object? sender, EventArgs e)
        => btnAddNew.Enabled = false;

    private void BtnAddNew_EnabledChanged(object sender, EventArgs e)
    {
        if (btnAddNew.Enabled)
        {
            btnAddNew.BackColor = Color.FromArgb(226, 232, 240);
            btnAddNew.ForeColor = Color.FromArgb(51, 65, 85);
        }
        else
        {
            btnAddNew.BackColor = Color.FromArgb(148, 163, 184);
            btnAddNew.ForeColor = Color.FromArgb(71, 85, 105);
        }
    }

    private void BtnHelp_Click(object sender, EventArgs e)
    {
        string helpText =
            "POSTCODE DISTANCE CALCULATOR - HELP\n" +
            "───────────────────────────────────────────────\n\n" +
            "1. SINGLE JOURNEY:\n" +
            "Enter postcodes separated by commas (or one per line).\n" +
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
            "• Click the cross on a row to delete it, or 'Clear' to delete all history (e.g. at month end).\n\n" +
            "5. QUICK COPY:\n" +
            "• Click a total or its Copy button to copy just the number (e.g. 123.4) for pasting into a spreadsheet.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
