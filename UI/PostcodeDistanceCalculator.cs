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
    private readonly string settingsPath = SettingsStore.DefaultPath;
    private bool settingsLoaded;

    /// <summary>Saved batches, oldest first (displayed newest first). Persisted on every change.</summary>
    private List<JourneyBatch> history = [];

    /// <summary>Journeys currently shown in the breakdown (freshly calculated or loaded from history).</summary>
    private IReadOnlyList<Journey> shownJourneys = [];

    /// <summary>The last calculation, waiting to be saved. Null when the shown journeys are already in history.</summary>
    private JourneyBatch? unsavedBatch;

    /// <summary>How often each postcode appears across history and the input box, for spotting likely typos.</summary>
    private Dictionary<string, int> postcodeUsage = [];

    /// <summary>
    /// Each postcode's warning (null if none) for the current <see cref="postcodeUsage"/>. History rows are redrawn
    /// often (e.g. while dragging a splitter), and checking means comparing against every postcode in use.
    /// </summary>
    private readonly Dictionary<string, string?> warningByPostcode = [];

    /// <summary>Postcodes flagged in the input box and breakdown, listed when the warnings link is clicked.</summary>
    private List<(string Postcode, string Message)> postcodeWarnings = [];

    /// <summary>Shows a history row's postcode warnings on hover.</summary>
    private readonly ToolTip historyWarningTip = new();
    private int historyWarningTipIndex = -1;

    /// <summary>Set while a calculation is running; cancelling it stops the calculation. Null otherwise.</summary>
    private CancellationTokenSource? calculation;

    /// <summary>Batches whose journey rows are hidden in the history list. Kept for the session only.</summary>
    private readonly HashSet<JourneyBatch> collapsedBatches = [];

    /// <summary>
    /// Set when history.json exists but couldn't be loaded. Saving is then disabled for the session,
    /// because writing the (empty) in-memory history would overwrite the user's real saved history.
    /// </summary>
    private bool historySaveBlocked;

    /// <summary>Set while code (not the user) changes the history selection, so the change doesn't load that row.</summary>
    private bool suppressHistoryLoad;

    /// <summary>Brief popup confirming exactly what was copied to the clipboard.</summary>
    private readonly ToolTip copyToast;

    // Light slate used for batch headers and the enabled Save button. The Designer's light buttons use the same
    // value as a literal (generated code can't reference constants), so change both together.
    private static readonly Color LightSlateColor = Color.FromArgb(226, 232, 240);
    private static readonly Color GripColor = Color.FromArgb(100, 116, 139);
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

        // Owner-drawn rows only repaint the newly exposed strip on resize, so redraw them all to reflow the route text
        lstHistory.Resize += (_, _) => lstHistory.Invalidate();
        lstHistory.MouseMove += LstHistory_MouseMove;
        lstHistory.MouseLeave += (_, _) => HideHistoryWarningTip();
        this.Disposed += (_, _) => historyWarningTip.Dispose();

        // Grab handles on the bars between the panels, so it's clear they can be dragged to resize the boxes
        splitContainer.Paint += SplitContainer_Paint;
        splitContainerLeft.Paint += SplitContainer_Paint;

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

    /// <summary>Draws a row of dots in the middle of a splitter bar to show it can be dragged.</summary>
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

        var settings = SettingsStore.Load(settingsPath);
        chkCompact.Checked = settings.CompactBreakdown;
        chkLegDistances.Checked = settings.ShowLegDistances;
        chkShowEstimate.Checked = settings.ShowEstimate;
        (settings.Unit == DistanceUnit.Kilometers ? rdoKm : rdoMiles).Checked = true;
        settingsLoaded = true;

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
        // While calculating, the button reads "Cancel"
        if (calculation != null)
        {
            calculation.Cancel();
            return;
        }

        string rawInput = txtPostcodes.Text;

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            MessageBox.Show("Please enter postcodes separated by commas.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
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

        // Everything Cancel puts back. The input text only changes on success, so it needs no copy.
        var before = (Journeys: shownJourneys, Savable: unsavedBatch, CanSave: btnAddNew.Enabled, Status: lblStatus.Text);

        // Lock input & history so nothing can touch journey state while requests are in flight
        calculation = new CancellationTokenSource();
        CancellationToken cancel = calculation.Token;
        SetCalculating(true);
        lblStatus.Text = "Looking up postcodes...";
        ShowJourneys([], savable: null);

        try
        {
            // Journeys without a date label use the Date box; empty if the user provided nothing
            string dateTag = txtDate.Text.Trim();

            // Built locally and only shown on full success, so a failure can't leave a partial batch
            var journeys = new List<Journey>();

            // Every postcode in the batch in one lookup, then one routing request per journey
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
                journeys.Add(new Journey(route.DateTag ?? dateTag, JourneyParser.FormatRoute(route), legs));
            }

            // Tidy the input to one journey per line. Skipped journeys would be lost, so leave the text alone then.
            // Set before ShowJourneys, since changing the text disables Save.
            if (parsed.Skipped.Count == 0)
                txtPostcodes.Text = JourneyParser.Format(parsed.Routes);

            // Past the last request, so Cancel can no longer apply
            calculation.Dispose();
            calculation = null;
            SetCalculating(false);

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
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            // Put the results, Save button and status back exactly as they were before Calculate
            ShowJourneys(before.Journeys, before.Savable);
            btnAddNew.Enabled = before.CanSave;
            lblStatus.Text = before.Status;
            copyToast.Show("Calculation cancelled. Nothing was changed.", btnCalculate, 0, -btnCalculate.Height - 4, 2000);
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
            calculation?.Dispose();
            calculation = null;
            SetCalculating(false);
        }
    }

    /// <summary>
    /// Locks the input and history while a calculation runs, and turns Calculate into Cancel. The postcode box is
    /// read-only rather than disabled so the user can still scroll and read it.
    /// </summary>
    private void SetCalculating(bool calculating)
    {
        txtPostcodes.ReadOnly = calculating;
        txtDate.ReadOnly = calculating;
        grpUnits.Enabled = !calculating;
        splitContainer.Panel2.Enabled = !calculating;
        if (calculating) btnAddNew.Enabled = false;

        btnCalculate.Text = calculating ? "Cancel" : "Calculate Route";
        toolTipInput.SetToolTip(btnCalculate, calculating
            ? "Stop calculating and go back to how things were"
            : "Calculate (Ctrl+Enter in the postcode box)");
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
        btnVerifyJourney.Enabled = journeys.Count > 0;
        btnVerifyJourney.Text = journeys.Count > 0 ? $"Verify in Google Maps ({journeys.Count})" : "Verify in Google Maps";
    }

    private void RenderResults()
    {
        txtResults.Text = BreakdownFormatter.Format(shownJourneys, Unit, chkCompact.Checked, chkLegDistances.Checked, chkShowEstimate.Checked);

        string title = shownJourneys.Count > 1 ? "Combined" : "Journey";
        lblJourneyTotal.Text = $"{title} Total: {Unit.FormatNumber(ShownTotalKm)} {Unit.ShortName()}";

        UpdatePostcodeWarnings();
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
            if (collapsedBatches.Contains(batch)) continue;
            foreach (var journey in batch.Journeys)
                lstHistory.Items.Add(new JourneyRow(batch, journey));
        }
        lstHistory.EndUpdate();

        // Forget batches that no longer exist (deleted or cleared)
        collapsedBatches.IntersectWith(history);

        RecalculateOverallTotal();
        UpdatePostcodeWarnings();
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
            // Chevron points right when collapsed and down when expanded
            Rectangle chevron = GetChevronBounds(e.Bounds);
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

            // Distance pinned to the right, the full route filling the space left of it, so widening the panel shows more stops
            string distance = JourneyDistanceText(row.Journey);
            int distanceWidth = TextRenderer.MeasureText(e.Graphics, distance, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            Rectangle distanceBounds = Rectangle.FromLTRB(Math.Max(left, deleteBounds.Left - padding - distanceWidth), e.Bounds.Top, deleteBounds.Left - padding, e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, distance, font, distanceBounds, textColor, flags | TextFormatFlags.Right);

            Rectangle routeBounds = Rectangle.FromLTRB(left, e.Bounds.Top, Math.Max(left, distanceBounds.Left - padding), e.Bounds.Bottom);
            TextRenderer.DrawText(e.Graphics, JourneyRoute(row.Journey), font, routeBounds, textColor, flags);
            UnderlineFlaggedStops(e.Graphics, row.Journey, font, routeBounds, isSelected || highContrast ? e.ForeColor : WarningTextBox.WarningColor);
        }

        IconFont.Draw(e.Graphics, MaterialIcon.Close, deleteBounds, deleteColor);

        e.DrawFocusRectangle();
    }

    /// <summary>
    /// Draws a wavy underline under each stop of <paramref name="journey"/>'s route that may be a typo, where it's
    /// visible within <paramref name="routeBounds"/> (the route is cut short with "..." when the panel is narrow).
    /// </summary>
    private void UnderlineFlaggedStops(Graphics graphics, Journey journey, Font font, Rectangle routeBounds, Color color)
    {
        const TextFormatFlags measure = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        // DrawText without NoPadding indents the text slightly; measure that once to line up with it
        int indent = (TextRenderer.MeasureText(graphics, "W", font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width
                      - TextRenderer.MeasureText(graphics, "W", font, Size.Empty, measure).Width) / 2;
        int amplitude = Math.Max(1, lstHistory.LogicalToDeviceUnits(1));
        int baseline = routeBounds.Top + (routeBounds.Height + font.Height) / 2 - amplitude;
        using var pen = new Pen(color, Math.Max(1, lstHistory.LogicalToDeviceUnits(1)));

        string prefix = "";
        foreach (string stop in journey.Stops)
        {
            string shown = GoogleMapsLink.FormatPostcode(stop);
            if (WarningFor(stop) != null)
            {
                int start = routeBounds.Left + indent + (prefix.Length == 0 ? 0 : TextRenderer.MeasureText(graphics, prefix, font, Size.Empty, measure).Width);
                int end = Math.Min(start + TextRenderer.MeasureText(graphics, shown, font, Size.Empty, measure).Width, routeBounds.Right);
                if (end - start > amplitude * 4)
                    WarningTextBox.DrawSquiggle(graphics, pen, start, end, baseline, amplitude, lstHistory.LogicalToDeviceUnits(2));
            }
            prefix += shown + " -> ";
        }
    }

    /// <summary>The warning for one normalised postcode, or null if it looks fine.</summary>
    private string? WarningFor(string postcode)
    {
        if (!warningByPostcode.TryGetValue(postcode, out string? message))
            warningByPostcode[postcode] = message = PostcodeWarnings.Check(postcode, postcodeUsage);
        return message;
    }

    /// <summary>Postcode warnings for a journey's stops, one line each, for tooltips and screen readers.</summary>
    private List<string> JourneyWarnings(Journey journey)
        => journey.Stops.Distinct()
            .Select(stop => (stop, message: WarningFor(stop)))
            .Where(w => w.message != null)
            .Select(w => $"{GoogleMapsLink.FormatPostcode(w.stop)}: {w.message}")
            .ToList();

    /// <summary>Shows the postcode warnings for the journey row under the pointer.</summary>
    private void LstHistory_MouseMove(object? sender, MouseEventArgs e)
    {
        int index = lstHistory.IndexFromPoint(e.Location);
        if (index == historyWarningTipIndex) return;

        HideHistoryWarningTip();
        if (index == ListBox.NoMatches || lstHistory.Items[index] is not JourneyRow row) return;

        var warnings = JourneyWarnings(row.Journey);
        if (warnings.Count == 0) return;

        historyWarningTipIndex = index;
        Rectangle bounds = lstHistory.GetItemRectangle(index);
        historyWarningTip.Show(string.Join("\n", warnings), lstHistory, bounds.Left + lstHistory.LogicalToDeviceUnits(20), bounds.Bottom + 2, 5000);
    }

    private void HideHistoryWarningTip()
    {
        historyWarningTipIndex = -1;
        historyWarningTip.Hide(lstHistory);
    }

    /// <summary>
    /// Re-checks every postcode for likely typos and updates the underlines in the input box and breakdown, the
    /// warnings link, and the history rows. Usage counts come from history plus the input box, so a postcode
    /// used all month makes a one-key slip from it stand out.
    /// </summary>
    private void UpdatePostcodeWarnings()
    {
        var inputSpans = PostcodeWarnings.FindInInput(txtPostcodes.Text);
        postcodeUsage = PostcodeWarnings.CountUsage(
            history.SelectMany(b => b.Journeys).SelectMany(j => j.Stops).Concat(inputSpans.Select(s => s.Postcode)));
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

    /// <summary>Where <paramref name="word"/> appears in <paramref name="text"/> not as part of a longer word.</summary>
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
            string.Join("\n", postcodeWarnings.Select(w => $"\u2022 {GoogleMapsLink.FormatPostcode(w.Postcode)}: {w.Message}")) +
            "\n\nHover over an underlined postcode to see its warning.",
            "Postcodes to Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private string BatchText(JourneyBatch batch)
        => $"{batch.CreatedAt:dd/MM/yyyy HH:mm}  ({JourneyCount(batch.Journeys.Count)}, {Unit.FormatNumber(batch.TotalKm)} {Unit.ShortName()})";

    private string JourneyText(Journey journey) => $"{JourneyRoute(journey)} ({JourneyDistanceText(journey)})";

    /// <summary>Every stop, e.g. "BS15 8DB -> BS5 8NG -> BS15 8DB".</summary>
    private static string JourneyRoute(Journey journey) => string.Join(" -> ", journey.Stops.Select(GoogleMapsLink.FormatPostcode));

    private string JourneyDistanceText(Journey journey)
        => $"{Unit.FormatNumber(journey.TotalKm)} {Unit.ShortName()}{(journey.IsVerified ? " (Google)" : "")}";

    /// <summary>
    /// Text for each history row as screen readers (and ListBox.GetItemText) see it. The rows are owner-drawn,
    /// so without this they'd be announced by their type names.
    /// </summary>
    private void LstHistory_Format(object sender, ListControlConvertEventArgs e)
    {
        e.Value = e.ListItem switch
        {
            JourneyBatch batch => $"Batch {BatchText(batch)}{(collapsedBatches.Contains(batch) ? ", collapsed" : "")}",
            JourneyRow { Journey: var journey } => (string.IsNullOrWhiteSpace(journey.DateTag) ? "" : $"[{journey.DateTag}] ") +
                JourneyText(journey) + (JourneyWarnings(journey) is { Count: > 0 } w ? $", check postcodes: {string.Join("; ", w)}" : ""),
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

        // Shift+arrow extends a multi-selection rather than moving to a row to load
        if (IsMultiSelecting || lstHistory.SelectedIndices.Count != 1) return;

        if (lstHistory.SelectedItem is { } item)
            LoadHistoryItem(item);
    }

    private void LstHistory_MouseDown(object sender, MouseEventArgs e)
    {
        // Right-click on a selected row keeps the whole selection, so the menu acts on all of it. On any other row
        // it selects just that row (without loading it); on empty space it clears the selection so no menu opens.
        if (e.Button == MouseButtons.Right)
        {
            int rowIndex = lstHistory.IndexFromPoint(e.Location);
            if (rowIndex == ListBox.NoMatches || !lstHistory.SelectedIndices.Contains(rowIndex))
                SelectHistoryRowWithoutLoading(rowIndex == ListBox.NoMatches ? -1 : rowIndex);
            return;
        }

        // Left single-clicks only: the second press of a double-click would otherwise delete the row that moved up under the cursor
        if (e.Button != MouseButtons.Left || e.Clicks > 1) return;

        // Ctrl/Shift clicks build a multi-selection for the right-click menu; they don't load or delete
        if (IsMultiSelecting) return;

        int clicked = lstHistory.IndexFromPoint(e.Location);
        int index = DeleteButtonHitIndex(e.Location);
        if (index != -1)
        {
            DeleteHistoryItem(lstHistory.Items[index]);
        }
        else if (clicked != ListBox.NoMatches && lstHistory.Items[clicked] is JourneyBatch batch &&
                 GetChevronBounds(lstHistory.GetItemRectangle(clicked)).Contains(e.Location))
        {
            ToggleBatchCollapsed(batch);
        }
        else if (clicked == lstHistory.SelectedIndex && lstHistory.SelectedItem is { } item)
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

        // A date shared by every journey goes in the Date box (empty if none had one). Different dates go on each
        // journey's line as a date label, so recalculating keeps them.
        string[] dates = journeys.Select(j => j.DateTag).Distinct().ToArray();
        bool sharedDate = dates.Length == 1;
        txtDate.Text = sharedDate ? dates[0] : string.Empty;
        txtPostcodes.Text = JourneyParser.Format(journeys.Select(j => new JourneyParser.ParsedRoute(
            j.RawInput, j.Stops, sharedDate || string.IsNullOrWhiteSpace(j.DateTag) ? null : j.DateTag)));

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

    /// <summary>
    /// Deletes several selected rows (batch headers and journeys) after one confirmation. Returns false if the user cancelled.
    /// </summary>
    private bool DeleteHistoryItems(IReadOnlyList<object> items)
    {
        var batches = items.OfType<JourneyBatch>().ToList();
        // Journeys whose whole batch is also selected are removed with it
        var rows = items.OfType<JourneyRow>().Where(r => !batches.Contains(r.Batch)).ToList();
        int journeyCount = batches.Sum(b => b.Journeys.Count) + rows.Count;

        if (MessageBox.Show($"Delete the {JourneyCount(journeyCount)} in the selected rows?", "Delete Selected",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return false;

        foreach (var batch in batches) history.Remove(batch);
        foreach (var row in rows)
        {
            row.Batch.Journeys.Remove(row.Journey);
            if (row.Batch.Journeys.Count == 0) history.Remove(row.Batch);
        }
        lblStatus.Text = $"Removed {JourneyCount(journeyCount)} from history.";

        SaveHistory();
        RefreshHistoryList();
        return true;
    }

    #region Google Maps verification

    private void BtnVerifyJourney_Click(object sender, EventArgs e)
        => VerifyWithGoogleMaps(shownJourneys);

    /// <summary>
    /// Opens each journey in Google Maps in turn and asks for the distance Google shows. Entered figures replace the
    /// app's estimate in the shown results, the unsaved batch and saved history, once the run finishes. Cancel in
    /// the dialog discards every figure from the run, leaving everything as it was.
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

            if (!OpenInBrowser(link)) break; // Still save and show anything entered before this

            string stops = string.Join(", ", journey.Stops.Select(GoogleMapsLink.FormatPostcode));
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

        if (entered.Count == 0) return;

        bool historyChanged = false;
        foreach (var (old, updated) in entered)
            historyChanged |= ReplaceJourney(old, updated);
        AfterJourneysChanged(historyChanged);
        lblStatus.Text = $"Updated {JourneyCount(entered.Count)} with Google Maps figures.";
    }

    /// <summary>
    /// Swaps <paramref name="old"/> for <paramref name="updated"/> everywhere it appears: shown results, the unsaved
    /// batch and saved history. Returns true if saved history changed.
    /// </summary>
    private bool ReplaceJourney(Journey old, Journey updated)
    {
        static bool ReplaceIn(List<Journey> list, Journey old, Journey updated)
        {
            int index = list.FindIndex(j => ReferenceEquals(j, old));
            if (index >= 0) list[index] = updated;
            return index >= 0;
        }

        shownJourneys = shownJourneys.Select(j => ReferenceEquals(j, old) ? updated : j).ToList();
        if (unsavedBatch != null) ReplaceIn(unsavedBatch.Journeys, old, updated);

        bool historyChanged = false;
        foreach (var batch in history)
            historyChanged |= ReplaceIn(batch.Journeys, old, updated);
        return historyChanged;
    }

    /// <summary>Re-renders after journeys change, saving history first if it changed.</summary>
    private void AfterJourneysChanged(bool historyChanged)
    {
        if (historyChanged)
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

    /// <summary>Sets up the history right-click menu for the selected rows; cancels it when nothing is selected.</summary>
    private void MnuHistory_Opening(object sender, CancelEventArgs e)
    {
        var journeys = SelectedHistoryJourneys();
        if (journeys.Count == 0)
        {
            e.Cancel = true;
            return;
        }

        mnuVerify.Text = journeys.Count > 1 ? $"Verify {journeys.Count} Journeys with Google Maps..." : "Verify with Google Maps...";
        mnuOpenInMaps.Visible = lstHistory.SelectedItems.Count == 1 && lstHistory.SelectedItem is JourneyRow;
        mnuUseEstimate.Enabled = journeys.Any(j => j.IsVerified);
    }

    private void MnuVerify_Click(object sender, EventArgs e) => VerifyWithGoogleMaps(SelectedHistoryJourneys());

    private void MnuOpenInMaps_Click(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is not JourneyRow row) return;

        if (GoogleMapsLink.ForPostcodes(row.Journey.Stops) is { } link)
            OpenInBrowser(link);
        else
            MessageBox.Show($"This journey has more than {GoogleMapsLink.MaxStops} stops, which Google Maps links don't support.", "Too Many Stops", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Removes Google Maps figures from the selected batches and journeys, going back to the app's estimate.</summary>
    private void MnuUseEstimate_Click(object sender, EventArgs e)
    {
        bool historyChanged = false;
        foreach (var journey in SelectedHistoryJourneys().Where(j => j.IsVerified))
            historyChanged |= ReplaceJourney(journey, journey with { GoogleMapsKm = null });

        AfterJourneysChanged(historyChanged);
        lblStatus.Text = "Using this app's estimate instead of Google Maps.";
    }

    /// <summary>
    /// Every journey in the selected history rows, in list order, each once: a selected batch header stands for all
    /// its journeys (including any hidden while it's collapsed).
    /// </summary>
    private List<Journey> SelectedHistoryJourneys()
        => lstHistory.SelectedItems.Cast<object>().SelectMany(HistoryItemJourneys).Distinct(ReferenceEqualityComparer.Instance).Cast<Journey>().ToList();

    /// <summary>The journeys a history row stands for (a copy of a batch's list, or the one journey).</summary>
    private static IReadOnlyList<Journey> HistoryItemJourneys(object item) => item switch
    {
        JourneyBatch batch => [.. batch.Journeys],
        JourneyRow row => [row.Journey],
        _ => []
    };

    #endregion

    /// <summary>Saves the whole history as a readable .txt report wherever the user picks, starting next to the app.</summary>
    private void BtnExportHistory_Click(object sender, EventArgs e)
    {
        if (history.Count == 0)
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
            string report = BreakdownFormatter.FormatHistory(history, Unit, DateTime.Now);
            File.WriteAllText(dialog.FileName, HistoryStore.ToExport(report, history));
            lblStatus.Text = $"History exported to {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Couldn't save the file:\n\n{ex.Message}", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Adds the batches from a .txt file made by Export to the current history. Batches already in history
    /// (same date and time, e.g. importing the same file twice) are skipped.
    /// </summary>
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

        var existing = history.Select(b => b.CreatedAt).ToHashSet();
        var added = imported.Where(b => existing.Add(b.CreatedAt)).ToList();
        int skipped = imported.Count - added.Count;

        history.AddRange(added);
        history.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

        string skippedNote = skipped == 0 ? "" : $" ({skipped} already in history)";
        lblStatus.Text = $"Imported {added.Count} batch{(added.Count == 1 ? "" : "es")}{skippedNote}.";
        SaveHistory(); // Sets its own status if saving is blocked, so it runs after the success message
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
    /// Delete removes the selected history rows, the same as clicking their crosses, then selects the row that
    /// took their place (without loading it) so rows can be deleted one after another from the keyboard.
    /// Left collapses a selected batch header and Right expands it.
    /// </summary>
    private void LstHistory_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Right && lstHistory.SelectedIndices.Count == 1 && lstHistory.SelectedItem is JourneyBatch batch)
        {
            e.Handled = true;
            if (collapsedBatches.Contains(batch) == (e.KeyCode == Keys.Right))
                ToggleBatchCollapsed(batch);
            return;
        }

        if (e.KeyCode != Keys.Delete || lstHistory.SelectedItems.Count == 0) return;

        e.Handled = true;
        int index = lstHistory.SelectedIndex;
        bool deleted = lstHistory.SelectedItems.Count == 1
            ? DeleteHistoryItem(lstHistory.SelectedItem!)
            : DeleteHistoryItems(lstHistory.SelectedItems.Cast<object>().ToList());
        if (!deleted || lstHistory.Items.Count == 0) return;

        SelectHistoryRowWithoutLoading(Math.Min(index, lstHistory.Items.Count - 1));
    }

    /// <summary>True while Ctrl or Shift is held, i.e. the user is building a multi-selection.</summary>
    private static bool IsMultiSelecting => (ModifierKeys & (Keys.Control | Keys.Shift)) != 0;

    /// <summary>Shows or hides a batch's journey rows, keeping the batch header selected.</summary>
    private void ToggleBatchCollapsed(JourneyBatch batch)
    {
        if (!collapsedBatches.Remove(batch)) collapsedBatches.Add(batch);
        RefreshHistoryList();
        SelectHistoryRowWithoutLoading(lstHistory.Items.IndexOf(batch));
    }

    /// <summary>The chevron at the left of a batch header row, sized from the row height so it follows DPI.</summary>
    private Rectangle GetChevronBounds(Rectangle itemBounds)
    {
        int size = itemBounds.Height - lstHistory.LogicalToDeviceUnits(6);
        int left = itemBounds.Left + lstHistory.LogicalToDeviceUnits(3);
        return new Rectangle(left, itemBounds.Top + (itemBounds.Height - size) / 2, size, size);
    }

    /// <summary>Selects only the row at <paramref name="index"/> (or nothing, for -1) without loading it.</summary>
    private void SelectHistoryRowWithoutLoading(int index)
    {
        suppressHistoryLoad = true;
        try
        {
            lstHistory.ClearSelected();
            if (index >= 0) lstHistory.SetSelected(index, true);
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

    private void BreakdownOption_CheckedChanged(object sender, EventArgs e)
    {
        RenderResults();
        SaveSettings();
    }

    private void SaveSettings()
    {
        // Skip the changes made while applying the saved settings on load
        if (settingsLoaded)
            SettingsStore.Save(settingsPath, new AppSettings(chkCompact.Checked, chkLegDistances.Checked, chkShowEstimate.Checked, Unit));
    }

    private void UnitToggle_CheckedChanged(object sender, EventArgs e)
    {
        // Both radios are wired; only react to the one becoming checked
        if (sender is RadioButton { Checked: false }) return;

        RenderResults();
        lstHistory.Invalidate();
        RecalculateOverallTotal();
        SaveSettings();
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
        if (e.KeyCode == Keys.Enter && e.Control && calculation == null)
        {
            e.SuppressKeyPress = true;
            btnCalculate.PerformClick();
        }
    }

    /// <summary>Enter in the date box calculates. (Not a form AcceptButton, which would also fire from the history list.)</summary>
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
        btnAddNew.Enabled = false;
        UpdatePostcodeWarnings();
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
            "• '+ Save to History' saves the current journeys as one dated batch.\n" +
            "• History is saved to history.json next to the app, so it survives restarts and moves with the app folder.\n" +
            "• Click a batch header to load all its journeys, or a journey to load just that one.\n" +
            "• Click the cross on a row (or select it and press Delete) to remove it, or 'Clear' to delete all history (e.g. at month end).\n" +
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
            "• 'Estimate vs Google' shows this app's estimate beside Google's figure for verified journeys.\n\n" +
            "7. QUICK COPY:\n" +
            "• Click a total or its Copy button to copy just the number (e.g. 123.4) for pasting into a spreadsheet.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
