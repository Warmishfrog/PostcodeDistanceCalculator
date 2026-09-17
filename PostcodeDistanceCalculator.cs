using System.Data;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PostcodeDistanceCalculator;

public partial class PostcodeDistanceCalculator : Form
{
    private static readonly HttpClient client = new();
    private double overallHistoryKm = 0;

    // Active state holding calculated journeys
    private List<ProcessedJourney> currentJourneys = [];

    private bool IsMiles => rdoMiles.Checked;
    private double ConversionFactor => IsMiles ? 0.621371 : 1.0;
    private string UnitText => IsMiles ? "miles" : "kilometers";
    private string ShortUnit => IsMiles ? "mi" : "km";

    private readonly int _lastSplitterDistance = 620;

    // P/Invoke for Win32 cue banner (gray placeholder text)
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
    private const int EM_SETCUEBANNER = 0x1501;

    public PostcodeDistanceCalculator()
    {
        InitializeComponent();

        // Blank date by default
        txtDate.Text = string.Empty;

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
    }

    private void PostcodeDistanceCalculator_Load(object? sender, EventArgs e)
    {
        btnAddNew.Enabled = false;

        // Set gray placeholder hint to today's date (dd/MM)
        string todayHint = DateTime.Now.ToString("dd/MM");
        SendMessage(txtDate.Handle, EM_SETCUEBANNER, (IntPtr)1, todayHint);
    }

    private async void BtnCalculate_Click(object sender, EventArgs e)
    {
        string rawInput = txtPostcodes.Text;

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            MessageBox.Show("Please enter postcodes separated by commas.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rawJourneys = rawInput.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        btnCalculate.Enabled = false;
        lblStatus.Text = "Fetching coordinates...";
        txtResults.Clear();
        currentJourneys.Clear();

        try
        {
            // Leave date string empty if user provided nothing
            string dateTag = txtDate.Text.Trim();

            for (int j = 0; j < rawJourneys.Length; j++)
            {
                string journeyInput = rawJourneys[j];
                var postcodes = journeyInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                             .Select(p => p.Replace(" ", "").ToUpper())
                                             .Where(p => !string.IsNullOrEmpty(p))
                                             .ToList();

                if (postcodes.Count < 2) continue;

                var coords = new Dictionary<string, (double Lat, double Lon)>();
                foreach (var code in postcodes.Distinct())
                {
                    lblStatus.Text = $"Locating {code}...";
                    coords[code] = await GetCoordinatesAsync(code);
                }

                var legs = new List<LegData>();
                double totalMeters = 0;

                for (int i = 0; i < postcodes.Count - 1; i++)
                {
                    string start = postcodes[i];
                    string end = postcodes[i + 1];

                    lblStatus.Text = $"Journey {j + 1}: Routing {start} ──► {end}...";
                    double meters = await GetDrivingDistanceMetersAsync(coords[start], coords[end]) * 1.07; // multiply by 1.07 to roughly match what google maps says

                    legs.Add(new LegData { LegNum = i + 1, Start = start, End = end, Meters = meters });
                    totalMeters += meters;
                }

                double journeyKm = totalMeters / 1000.0;
                string firstCode = postcodes.FirstOrDefault() ?? "";

                currentJourneys.Add(new ProcessedJourney
                {
                    Index = j + 1,
                    DateStr = dateTag,
                    RawInput = journeyInput,
                    FirstCode = firstCode,
                    Legs = legs,
                    TotalKm = journeyKm
                });
            }

            if (currentJourneys.Count == 0)
            {
                MessageBox.Show("No valid journeys found. Please ensure each route has at least two postcodes separated by commas.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RenderResults();
            btnAddNew.Enabled = true;

            lblStatus.Text = currentJourneys.Count > 1
                ? $"Calculation complete for {currentJourneys.Count} journeys!"
                : "Calculation Complete!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Calculation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Failed.";
        }
        finally
        {
            btnCalculate.Enabled = true;
        }
    }

    private void RenderResults()
    {
        if (currentJourneys.Count == 0)
        {
            txtResults.Clear();
            lblJourneyTotal.Text = $"Journey Total: 0.0 {ShortUnit}";
            return;
        }

        double factor = ConversionFactor;
        double overallCalculatedKm = currentJourneys.Sum(j => j.TotalKm);
        double overallConverted = overallCalculatedKm * factor;

        var lines = new List<string>();

        for (int i = 0; i < currentJourneys.Count; i++)
        {
            var journey = currentJourneys[i];
            double journeyConverted = journey.TotalKm * factor;

            string dateFormatted = !string.IsNullOrWhiteSpace(journey.DateStr) ? $" [{journey.DateStr}]" : "";
            string headerTitle = currentJourneys.Count > 1
                ? $"JOURNEY {i + 1}{dateFormatted}"
                : $"ROUTE BREAKDOWN{dateFormatted}";

            lines.Add(headerTitle);
            lines.Add(new string('─', 45));

            foreach (var leg in journey.Legs)
            {
                double dist = (leg.Meters / 1000.0) * factor;
                lines.Add($"  Leg {leg.LegNum}:  {leg.Start}  ──►  {leg.End}");
                lines.Add($"         Distance: {dist:F1} {UnitText}");
            }

            lines.Add(new string('─', 45));
            lines.Add($"  JOURNEY {(currentJourneys.Count > 1 ? (i + 1).ToString() : "")} TOTAL: {journeyConverted:F1} {UnitText.ToUpper()}");

            if (i < currentJourneys.Count - 1)
            {
                lines.Add(string.Empty);
                lines.Add(new string('═', 45));
                lines.Add(string.Empty);
            }
        }

        if (currentJourneys.Count > 1)
        {
            lines.Add(string.Empty);
            lines.Add(new string('═', 45));
            lines.Add($"  HISTORY TOTAL ({currentJourneys.Count} JOURNEYS): {overallConverted:F1} {UnitText.ToUpper()}");
        }

        txtResults.Text = string.Join(Environment.NewLine, lines);
        lblJourneyTotal.Text = $"Journey Total: {overallConverted:F1} {ShortUnit}";
    }

    private void LstHistory_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstHistory.Items.Count) return;

        e.DrawBackground();

        var item = (JourneyHistoryItem)lstHistory.Items[e.Index];
        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        Brush textBrush = isSelected ? Brushes.White : Brushes.Black;
        Brush dateBrush = isSelected ? Brushes.LightGray : Brushes.Gray;

        double convertedDist = item.TotalKm * ConversionFactor;

        int textOffset = 5;

        // Draw date only if present
        if (!string.IsNullOrWhiteSpace(item.DateStr))
        {
            e.Graphics.DrawString($"[{item.DateStr}]", e.Font ?? SystemFonts.DefaultFont, dateBrush, e.Bounds.X + 5, e.Bounds.Y + 4);
            textOffset = 55;
        }

        e.Graphics.DrawString($"{item.DisplayText} ({convertedDist:F1} {ShortUnit})", e.Font ?? SystemFonts.DefaultFont, textBrush, e.Bounds.X + textOffset, e.Bounds.Y + 4);

        Rectangle xRect = GetDeleteButtonBounds(e.Bounds);
        Brush xBrush = isSelected ? Brushes.White : Brushes.Crimson;
        e.Graphics.DrawString("✕", e.Font ?? SystemFonts.DefaultFont, xBrush, xRect.X, xRect.Y);

        e.DrawFocusRectangle();
    }

    private void LstHistory_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lstHistory.SelectedItem is JourneyHistoryItem item)
        {
            txtPostcodes.Text = item.RawInput;
            txtDate.Text = item.DateStr; // Restores empty string if history item had no date

            currentJourneys = [
                new ProcessedJourney
                {
                    Index = 1,
                    DateStr = item.DateStr,
                    RawInput = item.RawInput,
                    FirstCode = item.DisplayText.Replace("...", ""),
                    Legs = [.. item.Legs],
                    TotalKm = item.TotalKm
                }
            ];

            RenderResults();
            btnAddNew.Enabled = true;
            lblStatus.Text = "Loaded entry from history.";
        }
    }

    private void BtnAddNew_Click(object sender, EventArgs e)
    {
        if (currentJourneys.Count > 0)
        {
            foreach (var journey in currentJourneys)
            {
                var item = new JourneyHistoryItem
                {
                    DateStr = journey.DateStr,
                    DisplayText = $"{journey.FirstCode}...",
                    RawInput = journey.RawInput,
                    Legs = [.. journey.Legs],
                    TotalKm = journey.TotalKm
                };

                lstHistory.Items.Insert(0, item);
            }

            RecalculateOverallTotal();
            lblStatus.Text = currentJourneys.Count > 1
                ? $"Saved {currentJourneys.Count} journeys to history & cleared."
                : "Saved to history & cleared.";
        }

        txtPostcodes.Clear();
        txtResults.Clear();
        txtDate.Clear();
        currentJourneys.Clear();
        btnAddNew.Enabled = false;
        lblJourneyTotal.Text = $"Journey Total: 0.0 {ShortUnit}";
    }

    private void UnitToggle_CheckedChanged(object sender, EventArgs e)
    {
        RenderResults();
        lstHistory.Invalidate();
        RecalculateOverallTotal();
    }

    private void BtnToggleHistory_Click(object sender, EventArgs e)
    {
        splitContainer.Panel2Collapsed = !splitContainer.Panel2Collapsed;

        if (splitContainer.Panel2Collapsed)
        {
            btnReopenHistory.Visible = true;
            btnReopenHistory.BringToFront();
        }
        else
        {
            btnReopenHistory.Visible = false;
            if (_lastSplitterDistance > 0 && _lastSplitterDistance < splitContainer.Width)
            {
                splitContainer.SplitterDistance = _lastSplitterDistance;
            }
        }
    }

    #region Copy Event Handlers
    private void BtnCopyJourneyTotal_Click(object sender, EventArgs e) => CopyJourneyTotalToClipboard();
    private void LblJourneyTotal_Click(object? sender, EventArgs e) => CopyJourneyTotalToClipboard();

    private void BtnCopyHistoryTotal_Click(object sender, EventArgs e) => CopyHistoryTotalToClipboard();
    private void LblHistoryTotal_Click(object? sender, EventArgs e) => CopyHistoryTotalToClipboard();

    private void CopyJourneyTotalToClipboard()
    {
        if (string.IsNullOrWhiteSpace(lblJourneyTotal.Text)) return;

        Clipboard.SetText(lblJourneyTotal.Text);
        lblStatus.Text = $"Copied '{lblJourneyTotal.Text}' to clipboard!";
    }

    private void CopyHistoryTotalToClipboard()
    {
        if (string.IsNullOrWhiteSpace(lblHistoryTotal.Text)) return;

        Clipboard.SetText(lblHistoryTotal.Text);
        lblStatus.Text = $"Copied '{lblHistoryTotal.Text}' to clipboard!";
    }
    #endregion

    private void LstHistory_MouseDown(object sender, MouseEventArgs e)
    {
        int index = lstHistory.IndexFromPoint(e.Location);
        if (index != ListBox.NoMatches)
        {
            Rectangle itemBounds = lstHistory.GetItemRectangle(index);
            Rectangle xRect = GetDeleteButtonBounds(itemBounds);

            if (xRect.Contains(e.Location))
            {
                lstHistory.Items.RemoveAt(index);
                RecalculateOverallTotal();
                lblStatus.Text = "Removed entry from history.";
            }
        }
    }

    private static Rectangle GetDeleteButtonBounds(Rectangle itemBounds)
        => new(itemBounds.Right - 22, itemBounds.Y + 4, 18, 18);

    private void RecalculateOverallTotal()
    {
        overallHistoryKm = lstHistory.Items.Cast<JourneyHistoryItem>().Sum(x => x.TotalKm);
        double convertedTotal = overallHistoryKm * ConversionFactor;
        lblHistoryTotal.Text = $"History Total: {convertedTotal:F1} {ShortUnit}";
    }

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
            "Enter postcodes separated by commas.\n" +
            "• Example: SW1A 1AA, M1 1AE, EH1 1YZ\n\n" +
            "2. MULTIPLE JOURNEYS AT ONCE:\n" +
            "Separate individual journeys using a semicolon (;).\n" +
            "• Example: SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR ; CB2 1TN, NR1 1ER\n" +
            "• All journeys will render together in the Breakdown panel.\n\n" +
            "3. DATE FIELD:\n" +
            "• Optional. Leave blank or enter a custom date (defaults to showing today's date hint).\n\n" +
            "4. HISTORY MANAGEMENT:\n" +
            "• Clicking '+ Save to History' saves all active journeys as individual entries in History.\n" +
            "• Click any history row to restore it, or click the '✕' icon on an item to remove it.\n\n" +
            "5. QUICK COPY:\n" +
            "• Click either summary label directly or click its '📋 Copy' button to copy total distances.";

        MessageBox.Show(helpText, "How to Use", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    #region API Helpers
    private static async Task<(double Lat, double Lon)> GetCoordinatesAsync(string postcode)
    {
        string url = $"https://api.postcodes.io/postcodes/{Uri.EscapeDataString(postcode)}";

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error reaching postcodes.io: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception("The request to postcodes.io timed out.", ex);
        }

        // 404 is the only status code indicating the postcode actually doesn't exist
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)        
            throw new Exception($"'{postcode}' is not a recognized UK postcode.");        

        // Handle other server or API endpoint failures (e.g., 500 Internal Server Error, 503 Service Unavailable, 403 Forbidden)
        if (!response.IsSuccessStatusCode)        
            throw new Exception($"Postcode lookup service error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}). Please check the API connection.");        

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)            
                throw new Exception($"'{postcode}' returned no coordinate data.");            

            return (
                result.GetProperty("latitude").GetDouble(),
                result.GetProperty("longitude").GetDouble()
            );
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse response from postcodes.io API.", ex);
        }
    }

    private static async Task<double> GetDrivingDistanceMetersAsync((double Lat, double Lon) start, (double Lat, double Lon) end)
    {
        // CultureInfo.InvariantCulture prevents comma decimal separators (e.g. 51,500 instead of 51.500)
        string startLon = start.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string startLat = start.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string endLon = end.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string endLat = end.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);

        string url = $"http://router.project-osrm.org/route/v1/driving/{startLon},{startLat};{endLon},{endLat}?overview=false";

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WinFormsPostcodeDistanceTool");

            response = await client.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error reaching routing service (OSRM): {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception("The request to the routing service timed out.", ex);
        }

        if (!response.IsSuccessStatusCode)        
            throw new Exception($"Routing service error (HTTP {(int)response.StatusCode} {response.ReasonPhrase}). Please try again later.");        

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            // OSRM returns a "code" field (e.g. "Ok", "NoRoute", "InvalidQuery")
            if (root.TryGetProperty("code", out var codeElement))
            {
                string code = codeElement.GetString() ?? string.Empty;
                if (code == "NoRoute")                
                    throw new Exception("No drivable route could be found between these coordinates.");
                
                if (code != "Ok")                
                    throw new Exception($"Routing service returned error code: '{code}'.");                
            }

            if (!root.TryGetProperty("routes", out var routes) || routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)            
                throw new Exception("No drivable route found for the given postcodes.");            

            var firstRoute = routes[0];
            if (!firstRoute.TryGetProperty("distance", out var distanceElement))            
                throw new Exception("Route data received, but distance property was missing.");            

            return distanceElement.GetDouble();
        }
        catch (JsonException ex)
        {
            throw new Exception("Failed to parse response from OSRM routing service.", ex);
        }
    }
    #endregion
}

public class ProcessedJourney
{
    public int Index { get; set; }
    public string DateStr { get; set; } = string.Empty;
    public string RawInput { get; set; } = string.Empty;
    public string FirstCode { get; set; } = string.Empty;
    public List<LegData> Legs { get; set; } = [];
    public double TotalKm { get; set; }
}

public class LegData
{
    public int LegNum { get; set; }
    public string Start { get; set; } = string.Empty;
    public string End { get; set; } = string.Empty;
    public double Meters { get; set; }
}

public class JourneyHistoryItem
{
    public string DateStr { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public string RawInput { get; set; } = string.Empty;
    public List<LegData> Legs { get; set; } = [];
    public double TotalKm { get; set; }
}