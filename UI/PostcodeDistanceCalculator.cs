using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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

    /// <summary>The saved batch the shown journeys were loaded from, so Google Maps figures entered for them are saved too. Null for fresh results.</summary>
    private JourneyBatch? shownFromBatch;

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

    /// <summary>
    /// The csproj <c>&lt;Version&gt;</c> exactly as written, e.g. "2.0.0" or "2.1.0-beta.1". Read from the informational
    /// version (AssemblyVersion would drop prerelease labels) with the "+commit" build suffix removed.
    /// </summary>
    private static string AppVersion =>
        typeof(PostcodeDistanceCalculator).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    /// <summary>
    /// The app icon with all its sizes, embedded from <c>Resources/app.ico</c>. Loaded once for the life of the app.
    /// (Extracting the exe's icon would only give the shell's 32 px image, and would pick up dotnet.exe's icon when
    /// run through a host.)
    /// </summary>
    private static readonly Icon AppIcon = new(
        typeof(PostcodeDistanceCalculator).Assembly.GetManifestResourceStream("app.ico")
            ?? throw new InvalidOperationException("Embedded resource 'app.ico' is missing."));

    private DistanceUnit Unit => rdoMiles.Checked ? DistanceUnit.Miles : DistanceUnit.Kilometers;

    private double ShownTotalKm => shownJourneys.Sum(j => j.TotalKm);
    private double HistoryTotalKm => history.Sum(b => b.TotalKm);

    public PostcodeDistanceCalculator()
    {
        InitializeComponent();

        copyToast = new ToolTip(components!); // components is created by InitializeComponent and disposes the toast with the form

        // Optional date: the hint shows today's date in the expected format
        txtDate.PlaceholderText = DateTime.Now.ToString("dd/MM");

        // Window and taskbar icon; WinForms picks the matching size from the multi-size icon
        Icon = AppIcon;

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
        SetIcon(btnVerifyJourney, MaterialIcon.Map, 16);

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
        foreach (var button in new ButtonBase[] { btnHelp, btnCopyJourneyTotal, btnCopyHistoryTotal, btnReopenHistory, btnToggleHistory, btnVerifyJourney })
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
        lblStatus.Text = "Looking up postcodes...";
        ShowJourneys([], savable: null);

        try
        {
            // Leave date string empty if user provided nothing
            string dateTag = txtDate.Text.Trim();

            // Built locally and only shown on full success, so a failure can't leave a partial batch
            var journeys = new List<Journey>();

            // Every postcode in the batch in one lookup, then one routing request per journey
            var locations = await RouteApi.GetLocationsAsync(parsed.Routes.SelectMany(r => r.Postcodes));

            foreach (var route in parsed.Routes)
            {
                lblStatus.Text = parsed.Routes.Count > 1
                    ? $"Routing journey {journeys.Count + 1} of {parsed.Routes.Count}..."
                    : "Routing...";

                var stops = route.Postcodes.Select(p => locations[p]).ToList();
                var legMeters = await RouteApi.GetLegDistancesMetersAsync(stops);

                var legs = legMeters
                    .Select((meters, i) => new Leg(route.Postcodes[i], route.Postcodes[i + 1], meters * RouteApi.CalibrationFactor))
                    .ToList();
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

            // Retired postcodes still route from their last known location, but say so
            var terminated = locations.Where(l => l.Value.IsTerminated).ToList();
            if (terminated.Count > 0)
            {
                MessageBox.Show(
                    "These postcodes are no longer in use, so their last known location was used:\n\n" +
                    string.Join("\n", terminated.Select(t => t.Value.YearTerminated is int year ? $"• {t.Key} (retired {year})" : $"• {t.Key}")),
                    "Retired Postcodes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (RouteLookupException ex)
        {
            // Expected problems: unknown postcode, no route, network or service errors
            MessageBox.Show(ex.Message, "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"Failed. {ex.Message.ReplaceLineEndings(" ")}";
        }
        catch (Exception ex)
        {
            // Anything else is a bug: say so plainly and include the details rather than passing it off as a lookup failure
            MessageBox.Show($"Something unexpected went wrong. Please report this:\n\n{ex}", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"Unexpected error: {ex.GetType().Name}.";
        }
        finally
        {
            splitContainerLeft.Panel1.Enabled = true;
            splitContainer.Panel2.Enabled = true;
        }
    }

    /// <summary>
    /// Displays <paramref name="journeys"/> in the breakdown. <paramref name="savable"/> is the batch that
    /// "Save to History" would add, or null when there's nothing new to save. <paramref name="savedIn"/> is the
    /// history batch the journeys came from, if any.
    /// </summary>
    private void ShowJourneys(IReadOnlyList<Journey> journeys, JourneyBatch? savable, JourneyBatch? savedIn = null)
    {
        shownJourneys = journeys;
        unsavedBatch = savable;
        shownFromBatch = savedIn;
        RenderResults();
        btnAddNew.Enabled = savable != null;
        btnVerifyJourney.Enabled = journeys.Count > 0;
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
        => $"{journey.FirstPostcode}... ({Unit.FormatNumber(journey.TotalKm)} {Unit.ShortName()}{(journey.IsVerified ? ", Google Maps" : "")})";

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
        // Right-click selects the row under the cursor (without loading it) so the context menu acts on that row
        if (e.Button == MouseButtons.Right)
        {
            // Empty space clears the selection, so the menu doesn't open for a row the user didn't click
            int rowIndex = lstHistory.IndexFromPoint(e.Location);
            SelectHistoryRowWithoutLoading(rowIndex == ListBox.NoMatches ? -1 : rowIndex);
            return;
        }

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
        // A copy, so deleting from history doesn't change what's shown
        IReadOnlyList<Journey> journeys = HistoryItemJourneys(item);
        if (journeys.Count == 0) return;

        txtPostcodes.Text = string.Join(" ; ", journeys.Select(j => j.RawInput));

        // Restore the date only when every journey shares it (empty string if none had one).
        // Otherwise remember the dates so recalculating can warn that they'll be replaced.
        string[] dates = journeys.Select(j => j.DateTag).Distinct().ToArray();
        txtDate.Text = dates.Length == 1 ? dates[0] : string.Empty;
        loadedMixedDates = dates.Length > 1 ? dates : null;

        // Already in history, so nothing to save. Set after the text changes above, which also disable Save.
        ShowJourneys(journeys, savable: null, savedIn: HistoryItemBatch(item));

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

    #region Google Maps verification

    private void BtnVerifyJourney_Click(object sender, EventArgs e)
        => VerifyWithGoogleMaps(shownJourneys, shownFromBatch);

    /// <summary>
    /// Opens each journey in Google Maps in turn and asks for the distance Google shows. Entered figures replace the
    /// app's estimate in the shown results, in the unsaved batch, and in saved history when <paramref name="savedIn"/> is set.
    /// </summary>
    private void VerifyWithGoogleMaps(IReadOnlyList<Journey> journeys, JourneyBatch? savedIn)
    {
        int updated = 0;
        for (int i = 0; i < journeys.Count; i++)
        {
            var journey = journeys[i];
            var link = GoogleMapsLink.ForPostcodes(journey.Stops);
            if (link == null)
            {
                MessageBox.Show(
                    $"Journey {i + 1} has {journey.Stops.Count} stops, but Google Maps links allow at most {GoogleMapsLink.MaxStops}. Check it in Google Maps in parts instead.",
                    "Too Many Stops", MessageBoxButtons.OK, MessageBoxIcon.Information);
                continue;
            }

            if (!OpenInBrowser(link)) break; // Still save and show anything entered before this

            string stops = string.Join(", ", journey.Stops.Select(GoogleMapsLink.FormatPostcode));
            string heading = journeys.Count > 1 ? $"Journey {i + 1} of {journeys.Count}: {stops}" : stops;
            string? current = journey.GoogleMapsKm is double km ? DistanceInput.Format(Unit.FromKm(km)) : null;

            using var dialog = new VerifyDistanceDialog(heading, Unit.FormatNumber(journey.EstimatedKm), Unit.LongName(), current, () => OpenInBrowser(link));
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Cancel) break;
            if (result != DialogResult.OK) continue;

            ReplaceJourney(journey, journey with { GoogleMapsKm = Unit.ToKm(dialog.Distance) }, savedIn);
            updated++;
        }

        if (updated == 0) return;
        AfterJourneysChanged(savedIn);
        lblStatus.Text = $"Updated {JourneyCount(updated)} with Google Maps figures.";
    }

    /// <summary>Swaps <paramref name="old"/> for <paramref name="updated"/> everywhere it appears: shown results, unsaved batch and saved batch.</summary>
    private void ReplaceJourney(Journey old, Journey updated, JourneyBatch? savedIn)
    {
        static void ReplaceIn(List<Journey> list, Journey old, Journey updated)
        {
            int index = list.FindIndex(j => ReferenceEquals(j, old));
            if (index >= 0) list[index] = updated;
        }

        shownJourneys = shownJourneys.Select(j => ReferenceEquals(j, old) ? updated : j).ToList();
        if (unsavedBatch != null) ReplaceIn(unsavedBatch.Journeys, old, updated);
        if (savedIn != null) ReplaceIn(savedIn.Journeys, old, updated);
    }

    /// <summary>Re-renders after journeys change, saving history first if a saved batch was changed.</summary>
    private void AfterJourneysChanged(JourneyBatch? savedIn)
    {
        if (savedIn != null)
        {
            SaveHistory();
            RefreshHistoryList();
        }
        RenderResults();
    }

    /// <summary>Opens <paramref name="uri"/> in the default browser. Returns false (after telling the user) if it couldn't.</summary>
    private bool OpenInBrowser(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Win32Exception ex)
        {
            MessageBox.Show($"Couldn't open the browser:\n{ex.Message}\n\nLink:\n{uri.AbsoluteUri}", "Google Maps", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    /// <summary>Sets up the history right-click menu for the selected row; cancels it when nothing is selected.</summary>
    private void MnuHistory_Opening(object sender, CancelEventArgs e)
    {
        object? item = lstHistory.SelectedItem;
        if (item is not (JourneyBatch or JourneyRow))
        {
            e.Cancel = true;
            return;
        }

        var journeys = HistoryItemJourneys(item);
        mnuVerify.Text = item is JourneyBatch ? "Verify All Journeys with Google Maps..." : "Verify with Google Maps...";
        mnuOpenInMaps.Visible = item is JourneyRow;
        mnuUseEstimate.Enabled = journeys.Any(j => j.IsVerified);
    }

    private void MnuVerify_Click(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is { } item)
            VerifyWithGoogleMaps(HistoryItemJourneys(item), HistoryItemBatch(item));
    }

    private void MnuOpenInMaps_Click(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is not JourneyRow row) return;

        if (GoogleMapsLink.ForPostcodes(row.Journey.Stops) is { } link)
            OpenInBrowser(link);
        else
            MessageBox.Show($"This journey has more than {GoogleMapsLink.MaxStops} stops, which Google Maps links don't support.", "Too Many Stops", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Removes Google Maps figures from the selected batch or journey, going back to the app's estimate.</summary>
    private void MnuUseEstimate_Click(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is not { } item) return;

        var batch = HistoryItemBatch(item);
        foreach (var journey in HistoryItemJourneys(item).Where(j => j.IsVerified))
            ReplaceJourney(journey, journey with { GoogleMapsKm = null }, batch);

        AfterJourneysChanged(batch);
        lblStatus.Text = "Using this app's estimate instead of Google Maps.";
    }

    /// <summary>The journeys a history row stands for (a copy of a batch's list, or the one journey).</summary>
    private static IReadOnlyList<Journey> HistoryItemJourneys(object item) => item switch
    {
        JourneyBatch batch => [.. batch.Journeys],
        JourneyRow row => [row.Journey],
        _ => []
    };

    /// <summary>The saved batch a history row belongs to.</summary>
    private static JourneyBatch? HistoryItemBatch(object item) => item switch
    {
        JourneyBatch batch => batch,
        JourneyRow row => row.Batch,
        _ => null
    };

    #endregion

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

        SelectHistoryRowWithoutLoading(Math.Min(index, lstHistory.Items.Count - 1));
    }

    private void SelectHistoryRowWithoutLoading(int index)
    {
        suppressHistoryLoad = true;
        try
        {
            lstHistory.SelectedIndex = index;
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
            "5. CHECK AGAINST GOOGLE MAPS:\n" +
            "• Click 'Verify' (or right-click a saved journey or batch) to open each journey in Google Maps.\n" +
            "• Type in the distance Google shows and it replaces this app's estimate in the totals and history.\n" +
            "• Right-click a saved journey and choose 'Use This App's Estimate' to undo.\n\n" +
            "6. QUICK COPY:\n" +
            "• Click a total or its Copy button to copy just the number (e.g. 123.4) for pasting into a spreadsheet.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
