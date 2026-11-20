using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using PostcodeDistanceCalculator.Helpers;
using PostcodeDistanceCalculator.Models;
using PostcodeDistanceCalculator.Services;

namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// Main window: postcode input, journey breakdown and the history panel. Parsing, formatting, API calls,
/// history edits and persistence live in Helpers, Services and Models; this class wires them to the controls.
/// </summary>
public partial class MainForm : Form
{
    private readonly string historyPath = HistoryStore.DefaultPath;
    private readonly string settingsPath = SettingsStore.DefaultPath;
    private bool settingsLoaded;

    private JourneyHistory history = new([]);

    private IReadOnlyList<Journey> shownJourneys = [];

    /// <summary>The last calculation, waiting to be saved. Null when the shown journeys are already in history.</summary>
    private JourneyBatch? unsavedBatch;

    /// <summary>Things to know about the last calculation (skipped routes, retired postcodes), listed under the breakdown.</summary>
    private IReadOnlyList<string> calculationNotes = [];

    /// <summary>The postcode box text the shown results came from, to tell whether loading from history would lose typing.</summary>
    private string inputOfShownResults = "";

    /// <summary>How often each postcode appears across history and the input box, for spotting likely typos.</summary>
    private Dictionary<string, int> postcodeUsage = [];

    // Checking a postcode compares it against every postcode in use, and history rows redraw often (e.g. while dragging a splitter)
    private readonly Dictionary<string, string?> warningByPostcode = [];

    private List<(string Postcode, string Message)> postcodeWarnings = [];

    private CancellationTokenSource? calculation;

    /// <summary>
    /// Set when history.json exists but couldn't be loaded. Saving is then disabled for the session,
    /// because writing the (empty) in-memory history would overwrite the user's real saved history.
    /// </summary>
    private bool historySaveBlocked;

    private readonly ToolTip copyToast;

    // The Designer's light buttons use the same value as a literal (generated code can't reference constants), so change both together
    private static readonly Color LightSlateColor = Color.FromArgb(226, 232, 240);
    private static readonly Color GripColor = Color.FromArgb(100, 116, 139);
    private static readonly Color SaveButtonForeColor = Color.FromArgb(51, 65, 85);
    private static readonly Color SaveButtonDisabledBackColor = Color.FromArgb(148, 163, 184);
    private static readonly Color SaveButtonDisabledForeColor = Color.FromArgb(71, 85, 105);

    // From the informational version, because AssemblyVersion drops prerelease labels such as "-beta.1"; "+commit" is cut off
    private static string AppVersion =>
        typeof(MainForm).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    // Embedded rather than extracted from the exe, which only gives the shell's 32 px image (or dotnet.exe's icon when run through a host)
    private static readonly Icon AppIcon = new(
        typeof(MainForm).Assembly.GetManifestResourceStream("app.ico")
            ?? throw new InvalidOperationException("Embedded resource 'app.ico' is missing."));

    private DistanceUnit Unit => rdoMiles.Checked ? DistanceUnit.Miles : DistanceUnit.Kilometers;

    private double ShownTotalKm => shownJourneys.Sum(j => j.TotalKm);

    public MainForm()
    {
        InitializeComponent();

        copyToast = new ToolTip(components!); // Created by InitializeComponent, so it disposes the toast with the form
        txtDate.PlaceholderText = DateTime.Now.ToString("dd/MM");
        Icon = AppIcon;

        lstHistory.WarningFor = WarningFor;
        lstHistory.LoadRequested += (_, _) => LoadSelectedHistoryItem();
        lstHistory.DeleteRequested += (_, items) => DeleteHistoryItems(items);

        // Grab handles on the bars between the panels, so it's clear they can be dragged
        splitContainer.Paint += SplitContainer_Paint;
        splitContainerLeft.Paint += SplitContainer_Paint;

        // Control.Dispose doesn't dispose assigned images
        Disposed += (_, _) => DisposeIcons();

        btnSave.Enabled = false;
        btnCalculate.Enabled = false;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Here rather than the constructor so DeviceDpi reflects the monitor the window opens on
        RefreshScaledVisuals();

        var settings = SettingsStore.Load(settingsPath);
        chkCompact.Checked = settings.CompactBreakdown;
        chkLegDistances.Checked = settings.ShowLegDistances;
        chkShowEstimate.Checked = settings.ShowEstimate;
        (settings.Unit == DistanceUnit.Kilometers ? rdoKm : rdoMiles).Checked = true;
        lstHistory.Unit = Unit;
        settingsLoaded = true;

        try
        {
            history = new JourneyHistory(HistoryStore.Load(historyPath, out string? corruptBackupPath));
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
        RenderResults();
    }

    #region Scaling and icons

    // DpiChanged fires before child controls are rescaled, so re-render after base.OnDpiChanged
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        RefreshScaledVisuals();
    }

    // E.g. a high-contrast theme switched on or off
    protected override void OnSystemColorsChanged(EventArgs e)
    {
        base.OnSystemColorsChanged(e);
        RefreshScaledVisuals();
    }

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

    private static void SetIcon(ButtonBase button, MaterialIcon icon, int logicalSize)
    {
        button.Image?.Dispose();
        button.Image = IconFont.Render(icon, button.LogicalToDeviceUnits(logicalSize), IconColor(button));
    }

    private static Color IconColor(Control control)
        => SystemInformation.HighContrast ? SystemColors.ControlText : control.ForeColor;

    private void DisposeIcons()
    {
        foreach (var button in new ButtonBase[] { btnHelp, btnCopyJourneyTotal, btnCopyHistoryTotal, btnReopenHistory, btnToggleHistory, btnVerifyJourney })
            button.Image?.Dispose();
        lblInputInfo.Image?.Dispose();
    }

    private static void SplitContainer_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not SplitContainer split || split.Panel1Collapsed || split.Panel2Collapsed) return;

        Rectangle bar = split.SplitterRectangle;
        int dot = split.LogicalToDeviceUnits(3);
        int step = dot * 2;
        bool vertical = split.Orientation == Orientation.Vertical;
        Point centre = new(bar.Left + bar.Width / 2, bar.Top + bar.Height / 2);

        using var brush = new SolidBrush(SystemInformation.HighContrast ? SystemColors.ControlText : GripColor);
        for (int i = -2; i <= 2; i++)
        {
            int x = vertical ? centre.X : centre.X + i * step;
            int y = vertical ? centre.Y + i * step : centre.Y;
            e.Graphics.FillRectangle(brush, x - dot / 2, y - dot / 2, dot, dot);
        }
    }

    #endregion

    #region Calculate

    private async void BtnCalculate_Click(object sender, EventArgs e)
    {
        // While calculating, the button reads "Cancel"
        if (calculation != null)
        {
            calculation.Cancel();
            return;
        }

        var parsed = JourneyParser.Parse(txtPostcodes.Text);
        if (parsed.Routes.Count == 0)
        {
            // Clear the previous results so they aren't left showing (and copyable) next to the error
            ShowJourneys([], savable: null);
            MessageBox.Show("No journeys to calculate. Each journey needs at least two postcodes, separated by commas or new lines.", "Nothing to Calculate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            lblStatus.Text = "No journeys to calculate.";
            return;
        }

        // Everything Cancel puts back. The input text only changes on success, so it needs no copy.
        var before = (Journeys: shownJourneys, Savable: unsavedBatch, Notes: calculationNotes, CanSave: btnSave.Enabled, Status: lblStatus.Text);

        calculation = new CancellationTokenSource();
        CancellationToken cancel = calculation.Token;
        SetCalculating(true);
        lblStatus.Text = "Looking up postcodes...";
        ShowJourneys([], savable: null);

        try
        {
            string dateTag = txtDate.Text.Trim();

            // Built locally and only shown on full success, so a failure can't leave a partial batch
            var journeys = new List<Journey>();

            var locations = await RouteApi.GetLocationsAsync(parsed.Routes.SelectMany(r => r.Postcodes), cancel);

            foreach (var route in parsed.Routes)
            {
                lblStatus.Text = parsed.Routes.Count > 1
                    ? $"Routing journey {journeys.Count + 1} of {parsed.Routes.Count}..."
                    : "Routing...";

                var stops = route.Postcodes.Select(p => locations[p]).ToList();
                var legMeters = await RouteApi.GetLegDistancesMetersAsync(stops, cancel);

                var legs = legMeters
                    .Select((meters, i) => new Leg(route.Postcodes[i], route.Postcodes[i + 1], meters))
                    .ToList();
                journeys.Add(new Journey(route.DateTag ?? dateTag, legs));
            }

            // Tidy the input to one journey per line, unless that would lose the skipped journeys.
            // Set before ShowJourneys, since changing the text disables Save.
            if (parsed.Skipped.Count == 0)
                txtPostcodes.Text = JourneyParser.Format(parsed.Routes);
            inputOfShownResults = txtPostcodes.Text;

            // Past the last request, so Cancel can no longer apply
            calculation.Dispose();
            calculation = null;
            SetCalculating(false);

            var notes = new List<string>();
            if (parsed.Skipped.Count > 0)
                notes.Add($"Skipped, fewer than two postcodes: {string.Join("; ", parsed.Skipped)}");
            var retired = locations.Where(l => l.Value.IsTerminated).ToList();
            if (retired.Count > 0)
                notes.Add("No longer in use, so their last known location was used: " +
                    string.Join(", ", retired.Select(r => r.Value.YearTerminated is int year ? $"{Postcode.Format(r.Key)} (retired {year})" : Postcode.Format(r.Key))));

            ShowJourneys(journeys, savable: new JourneyBatch { CreatedAt = DateTime.Now, Journeys = journeys }, notes);

            string done = journeys.Count > 1 ? $"Calculation complete for {journeys.Count} journeys." : "Calculation complete.";
            lblStatus.Text = notes.Count > 0 ? $"{done} See the notes under the breakdown." : done;
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            ShowJourneys(before.Journeys, before.Savable, before.Notes);
            btnSave.Enabled = before.CanSave;
            lblStatus.Text = before.Status;
            copyToast.Show("Calculation cancelled. Nothing was changed.", btnCalculate, 0, -btnCalculate.Height - 4, 2000);
        }
        catch (RouteLookupException ex)
        {
            MessageBox.Show(ex.Message, "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"Failed. {ex.Message.ReplaceLineEndings(" ")}";
        }
        catch (Exception ex)
        {
            // Anything else is a bug: say so plainly rather than passing it off as a lookup failure
            MessageBox.Show($"Something unexpected went wrong. Please report this:\n\n{ex}", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = $"Unexpected error: {ex.GetType().Name}.";
        }
        finally
        {
            calculation?.Dispose();
            calculation = null;
            SetCalculating(false);
        }
    }

    /// <summary>
    /// Locks everything that could change journey state while requests are in flight, and turns Calculate into Cancel.
    /// The postcode box is read-only rather than disabled so it can still be scrolled and read.
    /// </summary>
    private void SetCalculating(bool calculating)
    {
        txtPostcodes.ReadOnly = calculating;
        txtDate.ReadOnly = calculating;
        grpUnits.Enabled = !calculating;
        splitContainer.Panel2.Enabled = !calculating;
        if (calculating) btnSave.Enabled = false;

        btnCalculate.Text = calculating ? "Cancel" : "Calculate Route";
        btnCalculate.Enabled = calculating || !string.IsNullOrWhiteSpace(txtPostcodes.Text);
        toolTipInput.SetToolTip(btnCalculate, calculating
            ? "Stop calculating and go back to how things were"
            : "Calculate (Ctrl+Enter in the postcode box)");
    }

    /// <param name="savable">The batch "Save and Clear" would add, or null when there's nothing new to save.</param>
    private void ShowJourneys(IReadOnlyList<Journey> journeys, JourneyBatch? savable, IReadOnlyList<string>? notes = null)
    {
        shownJourneys = journeys;
        unsavedBatch = savable;
        calculationNotes = notes ?? [];
        RenderResults();
        btnSave.Enabled = savable != null;
        btnVerifyJourney.Enabled = journeys.Count > 0;
        btnVerifyJourney.Text = journeys.Count > 0 ? $"Verify in Google Maps ({journeys.Count})" : "Verify in Google Maps";
    }

    private void RenderResults()
    {
        string breakdown = BreakdownFormatter.Format(shownJourneys, Unit, chkCompact.Checked, chkLegDistances.Checked, chkShowEstimate.Checked);
        if (calculationNotes.Count > 0)
            breakdown += Environment.NewLine + Environment.NewLine + "NOTES" + Environment.NewLine +
                string.Join(Environment.NewLine, calculationNotes.Select(n => $"  {n}"));
        txtResults.Text = breakdown;

        string title = shownJourneys.Count > 1 ? "Combined" : "Journey";
        lblJourneyTotal.Text = $"{title} Total: {Unit.FormatNumber(ShownTotalKm)} {Unit.ShortName()}";

        // Only meaningful once a journey has a Google Maps figure
        chkShowEstimate.Visible = shownJourneys.Any(j => j.IsVerified);

        UpdatePostcodeWarnings();
    }

    #endregion

    #region Postcode warnings

    private string? WarningFor(string postcode)
    {
        if (!warningByPostcode.TryGetValue(postcode, out string? message))
            warningByPostcode[postcode] = message = PostcodeWarnings.Check(postcode, postcodeUsage);
        return message;
    }

    /// <summary>
    /// Re-checks every postcode for likely typos and updates the underlines, the warnings link and the history rows.
    /// Usage counts come from history plus the input box, so a postcode used all month makes a one-key slip from it stand out.
    /// </summary>
    private void UpdatePostcodeWarnings()
    {
        var inputSpans = PostcodeWarnings.FindInInput(txtPostcodes.Text);
        postcodeUsage = PostcodeWarnings.CountUsage(
            history.Batches.SelectMany(b => b.Journeys).SelectMany(j => j.Stops).Concat(inputSpans.Select(s => s.Postcode)));
        warningByPostcode.Clear();

        var inputWarnings = inputSpans
            .Select(s => (Span: s, Message: WarningFor(s.Postcode)))
            .Where(w => w.Message != null)
            .ToList();
        txtPostcodes.Warnings = inputWarnings.Select(w => new WarningTextBox.TextWarning(w.Span.Start, w.Span.Length, w.Message!)).ToList();

        // The breakdown lists postcodes as stored (no space), e.g. "BS158DB"
        var resultWarnings = shownJourneys.SelectMany(j => j.Stops).Distinct()
            .Select(p => (Postcode: p, Message: WarningFor(p)))
            .Where(w => w.Message != null)
            .ToList();
        txtResults.Warnings = resultWarnings
            .SelectMany(w => WholeWordIndexes(txtResults.Text, w.Postcode).Select(i => new WarningTextBox.TextWarning(i, w.Postcode.Length, w.Message!)))
            .ToList();

        postcodeWarnings = inputWarnings.Select(w => (w.Span.Postcode, w.Message!))
            .Concat(resultWarnings.Select(w => (w.Postcode, w.Message!)))
            .DistinctBy(w => w.Item1)
            .ToList();
        lnkWarnings.Text = postcodeWarnings.Count == 1 ? "1 postcode to check" : $"{postcodeWarnings.Count} postcodes to check";
        lnkWarnings.Visible = postcodeWarnings.Count > 0;

        lstHistory.Invalidate();
    }

    private static IEnumerable<int> WholeWordIndexes(string text, string word)
    {
        for (int i = text.IndexOf(word, StringComparison.Ordinal); i >= 0; i = text.IndexOf(word, i + 1, StringComparison.Ordinal))
        {
            bool startsWord = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            bool endsWord = i + word.Length == text.Length || !char.IsLetterOrDigit(text[i + word.Length]);
            if (startsWord && endsWord) yield return i;
        }
    }

    private void LnkWarnings_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        MessageBox.Show(
            "These postcodes may have typos. They're only warnings: check them, and if they're right you can ignore this.\n\n" +
            string.Join("\n", postcodeWarnings.Select(w => $"• {Postcode.Format(w.Postcode)}: {w.Message}")) +
            "\n\nHover over an underlined postcode to see its warning.",
            "Postcodes to Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    #endregion

    #region History

    private void RefreshHistoryList()
    {
        lstHistory.ShowBatches(history.Batches);
        lblHistoryTotal.Text = $"History Total: {Unit.FormatNumber(history.TotalKm)} {Unit.ShortName()}";
        lnkUndo.Visible = history.CanUndoRemove;
        UpdatePostcodeWarnings();
    }

    /// <summary>Saves history, refreshes the list and shows <paramref name="status"/>, or that history couldn't be saved.</summary>
    private void CommitHistory(string status)
    {
        lblStatus.Text = SaveHistory() ? status : "History not saved. Your changes are kept until the app closes.";
        RefreshHistoryList();
    }

    /// <summary>Returns false if history couldn't be written, after telling the user why (unless saving was already blocked on load).</summary>
    private bool SaveHistory()
    {
        if (historySaveBlocked) return false;

        try
        {
            HistoryStore.Save(historyPath, history.Batches);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Couldn't save history to {historyPath}:\n{ex.Message}\n\nYour journeys are kept until the app closes. " +
                "Move the app to a folder you can write to (not Program Files) to keep history between sessions.",
                "History Not Saved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        if (unsavedBatch == null) return;

        history.Add(unsavedBatch);
        CommitHistory($"Saved {BreakdownFormatter.JourneyCount(unsavedBatch.Journeys.Count)} to history.");

        txtPostcodes.Clear();
        txtDate.Clear();
        inputOfShownResults = "";
        ShowJourneys([], savable: null);
    }

    private void LoadSelectedHistoryItem()
    {
        if (lstHistory.SelectedItem is not { } item) return;

        IReadOnlyList<Journey> journeys = HistoryListBox.JourneysOf(item);
        if (journeys.Count == 0) return;

        bool wouldLoseInput = unsavedBatch != null ||
            (!string.IsNullOrWhiteSpace(txtPostcodes.Text) && txtPostcodes.Text != inputOfShownResults);
        if (wouldLoseInput &&
            MessageBox.Show("Replace what's in the postcode box with this saved journey? Anything you haven't saved will be lost.",
                "Load from History", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // A date shared by every journey goes in the Date box; different dates go on each journey's line as a
        // date label, so recalculating keeps them
        string[] dates = journeys.Select(j => j.DateTag).Distinct().ToArray();
        bool sharedDate = dates.Length == 1;
        txtDate.Text = sharedDate ? dates[0] : string.Empty;
        txtPostcodes.Text = JourneyParser.Format(journeys.Select(j => new JourneyParser.ParsedRoute(
            j.Stops, sharedDate || string.IsNullOrWhiteSpace(j.DateTag) ? null : j.DateTag)));
        inputOfShownResults = txtPostcodes.Text;

        // Set after the text changes above, which disable Save
        ShowJourneys(journeys, savable: null);

        lblStatus.Text = item is JourneyBatch batch
            ? $"Loaded batch from {batch.CreatedAt:dd/MM/yyyy HH:mm} ({BreakdownFormatter.JourneyCount(journeys.Count)})."
            : "Loaded journey from history.";
    }

    private void DeleteHistoryItems(IReadOnlyList<object> items)
    {
        var batches = items.OfType<JourneyBatch>().ToList();
        // Journeys whose whole batch is also selected go with it
        var rows = items.OfType<HistoryListBox.JourneyRow>().Where(r => !batches.Contains(r.Batch)).ToList();
        int journeyCount = batches.Sum(b => b.Journeys.Count) + rows.Count;

        history.Remove(batches, rows.Select(r => (r.Batch, r.Journey)));
        CommitHistory($"Removed {BreakdownFormatter.JourneyCount(journeyCount)} from history.");
    }

    private void LnkUndo_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
    {
        history.UndoRemove();
        CommitHistory("Deleted journeys restored.");
    }

    private void BtnClearHistory_Click(object sender, EventArgs e)
    {
        if (history.Batches.Count == 0) return;

        if (MessageBox.Show($"Delete all {history.JourneyCount} saved journeys?\n\nTip: copy the History Total first if you still need it.",
                "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        history.Clear();
        CommitHistory("History cleared.");
    }

    private void BtnExportHistory_Click(object sender, EventArgs e)
    {
        if (history.Batches.Count == 0)
        {
            lblStatus.Text = "No saved journeys to export.";
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export History",
            Filter = "Text files (*.txt)|*.txt",
            DefaultExt = "txt",
            InitialDirectory = AppContext.BaseDirectory,
            FileName = $"Journey history {DateTime.Now:yyyy-MM-dd}.txt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string report = BreakdownFormatter.FormatHistory(history.Batches, Unit, DateTime.Now);
            File.WriteAllText(dialog.FileName, HistoryStore.ToExport(report, history.Batches));
            lblStatus.Text = $"History exported to {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Couldn't save the file:\n\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnImportHistory_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import History",
            Filter = "Text files (*.txt)|*.txt",
            InitialDirectory = AppContext.BaseDirectory,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        List<JourneyBatch> imported;
        try
        {
            imported = HistoryStore.ParseExport(File.ReadAllText(dialog.FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            MessageBox.Show($"Couldn't import the file:\n\n{ex.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        int added = history.Import(imported);
        int skipped = imported.Count - added;
        string skippedNote = skipped == 0 ? "" : $" ({skipped} already in history)";
        CommitHistory($"Imported {added} batch{(added == 1 ? "" : "es")}{skippedNote}.");
    }

    private void BtnToggleHistory_Click(object sender, EventArgs e)
    {
        // Collapsing Panel2 keeps SplitterDistance, so reopening restores wherever the user left the splitter
        splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;

        btnReopenHistory.Visible = splitContainer.Panel2Collapsed;
        if (btnReopenHistory.Visible) btnReopenHistory.BringToFront();
    }

    private void MnuHistory_Opening(object sender, CancelEventArgs e)
    {
        var journeys = lstHistory.SelectedJourneys();
        if (journeys.Count == 0)
        {
            e.Cancel = true;
            return;
        }

        bool single = lstHistory.SelectedItems.Count == 1;
        mnuLoad.Visible = single;
        mnuVerify.Text = journeys.Count > 1 ? $"Verify {journeys.Count} Journeys with Google Maps..." : "Verify with Google Maps...";
        mnuOpenInMaps.Visible = single && lstHistory.SelectedItem is HistoryListBox.JourneyRow;
        mnuUseEstimate.Enabled = journeys.Any(j => j.IsVerified);
    }

    private void MnuLoad_Click(object sender, EventArgs e) => LoadSelectedHistoryItem();

    private void MnuVerify_Click(object sender, EventArgs e) => VerifyWithGoogleMaps(lstHistory.SelectedJourneys());

    private void MnuOpenInMaps_Click(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is not HistoryListBox.JourneyRow row) return;

        if (GoogleMapsLink.ForPostcodes(row.Journey.Stops) is { } link)
            OpenInBrowser(link);
        else
            MessageBox.Show($"This journey has more than {GoogleMapsLink.MaxStops} stops, which Google Maps links don't support.", "Too Many Stops", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MnuUseEstimate_Click(object sender, EventArgs e)
    {
        var verified = lstHistory.SelectedJourneys().Where(j => j.IsVerified).ToList();
        ReplaceJourneys(verified.Select(j => (j, j with { GoogleMapsKm = null })).ToList(), "Using this app's estimate instead of Google Maps.");
    }

    #endregion

    #region Google Maps verification

    private void BtnVerifyJourney_Click(object sender, EventArgs e) => VerifyWithGoogleMaps(shownJourneys);

    /// <summary>
    /// Opens each journey in Google Maps in turn and asks for the distance Google shows. Figures are applied once
    /// the run finishes; Cancel in the dialog discards every figure from the run.
    /// </summary>
    private void VerifyWithGoogleMaps(IReadOnlyList<Journey> journeys)
    {
        var entered = new List<(Journey Old, Journey Updated)>();
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

            if (!OpenInBrowser(link)) break; // Still apply anything entered before this

            string stops = string.Join(", ", journey.Stops.Select(Postcode.Format));
            string heading = journeys.Count > 1 ? $"Journey {i + 1} of {journeys.Count}: {stops}" : stops;
            string? current = journey.GoogleMapsKm is double km ? DistanceInput.Format(Unit.FromKm(km)) : null;

            using var dialog = new VerifyDistanceDialog(heading, Unit.FormatNumber(journey.EstimatedKm), Unit.LongName(), current, () => OpenInBrowser(link), moreJourneys: journeys.Count > 1);
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Cancel)
            {
                lblStatus.Text = "Verification cancelled. Nothing was changed.";
                return;
            }
            if (result == DialogResult.Abort) break;
            if (result != DialogResult.OK) continue;

            entered.Add((journey, journey with { GoogleMapsKm = Unit.ToKm(dialog.Distance) }));
        }

        if (entered.Count > 0)
            ReplaceJourneys(entered, $"Updated {BreakdownFormatter.JourneyCount(entered.Count)} with Google Maps figures.");
    }

    /// <summary>Swaps each old journey for its updated one in the results, the unsaved batch and history.</summary>
    private void ReplaceJourneys(IReadOnlyList<(Journey Old, Journey Updated)> changes, string status)
    {
        if (changes.Count == 0) return;

        bool historyChanged = false;
        foreach (var (old, updated) in changes)
        {
            shownJourneys = shownJourneys.Select(j => ReferenceEquals(j, old) ? updated : j).ToList();
            if (unsavedBatch != null)
            {
                int index = unsavedBatch.Journeys.FindIndex(j => ReferenceEquals(j, old));
                if (index >= 0) unsavedBatch.Journeys[index] = updated;
            }
            historyChanged |= history.ReplaceJourney(old, updated);
        }

        if (historyChanged)
            CommitHistory(status);
        else
            lblStatus.Text = status;
        RenderResults();
    }

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

    #endregion

    #region Options, copy and keyboard

    private void BreakdownOption_CheckedChanged(object sender, EventArgs e)
    {
        RenderResults();
        SaveSettings();
    }

    private void UnitToggle_CheckedChanged(object sender, EventArgs e)
    {
        // Both radios are wired; only react to the one becoming checked
        if (sender is RadioButton { Checked: false }) return;

        RenderResults();
        lstHistory.Unit = Unit;
        lblHistoryTotal.Text = $"History Total: {Unit.FormatNumber(history.TotalKm)} {Unit.ShortName()}";
        SaveSettings();
    }

    private void SaveSettings()
    {
        // Skip the changes made while applying the saved settings on load
        if (settingsLoaded)
            SettingsStore.Save(settingsPath, new AppSettings(chkCompact.Checked, chkLegDistances.Checked, chkShowEstimate.Checked, Unit));
    }

    private void BtnCopyJourneyTotal_Click(object sender, EventArgs e) => CopyDistance(ShownTotalKm, btnCopyJourneyTotal);
    private void LblJourneyTotal_Click(object? sender, EventArgs e) => CopyDistance(ShownTotalKm, btnCopyJourneyTotal);

    private void BtnCopyHistoryTotal_Click(object sender, EventArgs e) => CopyDistance(history.TotalKm, btnCopyHistoryTotal);
    private void LblHistoryTotal_Click(object? sender, EventArgs e) => CopyDistance(history.TotalKm, btnCopyHistoryTotal);

    /// <summary>Copies just the number (no label or unit) so it pastes cleanly into a spreadsheet.</summary>
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

    // Plain Enter adds a new line, since postcodes can be entered one per line
    private void TxtPostcodes_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && e.Control && calculation == null)
        {
            e.SuppressKeyPress = true;
            btnCalculate.PerformClick();
        }
    }

    // Not a form AcceptButton, which would also fire from the history list
    private void TxtDate_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && calculation == null)
        {
            e.SuppressKeyPress = true;
            btnCalculate.PerformClick();
        }
    }

    private void TxtPostcodes_TextChanged(object? sender, EventArgs e)
    {
        btnSave.Enabled = false;
        if (calculation == null) btnCalculate.Enabled = !string.IsNullOrWhiteSpace(txtPostcodes.Text);
        UpdatePostcodeWarnings();
    }

    // Flat buttons don't change colour when disabled, so darken it to show there's nothing to save
    private void BtnSave_EnabledChanged(object sender, EventArgs e)
    {
        btnSave.BackColor = btnSave.Enabled ? LightSlateColor : SaveButtonDisabledBackColor;
        btnSave.ForeColor = btnSave.Enabled ? SaveButtonForeColor : SaveButtonDisabledForeColor;
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
            "• All journeys will render together in the Breakdown panel.\n" +
            "• Or put one journey per line, starting with its date and a colon:\n" +
            "    21st: BS15 8DB, BS5 8NG, BS15 8DB\n" +
            "    22nd: BS15 8DB, BS4 4PR, BS15 8DB\n" +
            "  Each line becomes its own journey tagged with that date.\n\n" +
            "3. DATE FIELD:\n" +
            "• Optional. Tag journeys with a date (e.g. 24/09) if you're tracking specific days. The hint shows today's date.\n" +
            "• A date label at the start of a journey (e.g. 21st:) overrides this box for that journey.\n" +
            "• Every saved batch is automatically stamped with the date and time it was calculated.\n\n" +
            "4. HISTORY MANAGEMENT:\n" +
            "• 'Save and Clear' saves the current journeys as one dated batch and empties the boxes for the next one.\n" +
            "• History is saved to history.json next to the app, so it survives restarts and moves with the app folder.\n" +
            "• Double-click a batch header (or press Enter) to load all its journeys, or a journey to load just that one.\n" +
            "• Click the cross on a row (or select it and press Delete) to remove it, or 'Clear' to delete all history (e.g. at month end). 'Undo' brings back what you just deleted.\n" +
            "• 'Export' saves all history to a .txt file you can read or keep; 'Import' adds an exported file back in.\n\n" +
            "5. CHECK AGAINST GOOGLE MAPS:\n" +
            "• Click 'Verify in Google Maps' (or right-click a saved journey or batch) to open each journey in Google Maps.\n" +
            "• Type in the distance Google shows and it replaces this app's estimate in the totals and history.\n" +
            "• Stop ends early and keeps what you've entered; Cancel discards everything from that run.\n" +
            "• Ctrl+click or Shift+click history rows to verify several at once.\n" +
            "• Right-click a saved journey and choose 'Use This App's Estimate' to undo.\n" +
            "• Google Maps distances can change with traffic, roadworks or closures, so the same route may give a different figure on another day.\n\n" +
            "6. BREAKDOWN OPTIONS:\n" +
            "• 'Compact' shows each journey's legs on one line.\n" +
            "• 'Leg distances' shows or hides each leg's distance.\n" +
            "• 'Estimate vs Google' (shown once a journey is verified) shows this app's estimate beside Google's figure.\n\n" +
            "7. QUICK COPY:\n" +
            "• Click a total or its Copy button to copy just the number (e.g. 123.4) for pasting into a spreadsheet.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    #endregion
}
