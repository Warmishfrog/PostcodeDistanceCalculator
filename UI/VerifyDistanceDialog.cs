using PostcodeDistanceCalculator.Helpers;

namespace PostcodeDistanceCalculator.UI;

/// <summary>
/// Asks for the distance Google Maps shows for one journey, while Google Maps is open in the browser.
/// Returns <see cref="DialogResult.OK"/> with <see cref="Distance"/> set, <see cref="DialogResult.Ignore"/> to skip
/// this journey, <see cref="DialogResult.Abort"/> to stop and keep the figures entered so far, or
/// <see cref="DialogResult.Cancel"/> to stop and discard every figure entered in this run.
/// </summary>
internal sealed class VerifyDistanceDialog : Form
{
    private readonly TextBox txtDistance;
    private readonly Button btnSave;

    /// <summary>The distance entered, in the unit passed to the constructor. Set when the result is OK.</summary>
    public double Distance { get; private set; }

    /// <param name="heading">Which journey this is, e.g. "Journey 2 of 3: SW1A 1AA, M1 1AE".</param>
    /// <param name="estimate">The app's estimate, already formatted in <paramref name="unitName"/>.</param>
    /// <param name="unitName">Unit the user should enter, e.g. "miles".</param>
    /// <param name="currentGoogleFigure">A previously entered Google Maps figure to pre-fill, if any.</param>
    /// <param name="reopenMaps">Opens the journey in Google Maps again.</param>
    /// <param name="moreJourneys">True when verifying several journeys, which adds the Stop button.</param>
    public VerifyDistanceDialog(string heading, string estimate, string unitName, string? currentGoogleFigure, Action reopenMaps, bool moreJourneys)
    {
        Text = "Verify with Google Maps";
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        var wrapWidth = new Size(LogicalToDeviceUnits(400), 0);

        layout.Controls.Add(new Label { AutoSize = true, MaximumSize = wrapWidth, Text = heading, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) });
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = wrapWidth,
            Text = "Google Maps has opened in your browser with this route. Enter the distance it shows for the route you would drive.",
        });

        var reopen = new LinkLabel { AutoSize = true, Text = "Open in Google Maps again", Margin = new Padding(0, 4, 0, 8) };
        reopen.LinkClicked += (_, _) => reopenMaps();
        layout.Controls.Add(reopen);

        layout.Controls.Add(new Label { AutoSize = true, Text = $"This app's estimate: {estimate} {unitName}", Margin = new Padding(0, 0, 0, 8) });

        var inputRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        inputRow.Controls.Add(new Label { AutoSize = true, Text = "Google Maps distance:", Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 0) });
        txtDistance = new TextBox { Width = LogicalToDeviceUnits(90), Text = currentGoogleFigure ?? "", AccessibleName = $"Google Maps distance in {unitName}" };
        txtDistance.TextChanged += (_, _) => btnSave!.Enabled = DistanceInput.TryParse(txtDistance.Text, out _);
        inputRow.Controls.Add(txtDistance);
        inputRow.Controls.Add(new Label { AutoSize = true, Text = unitName, Margin = new Padding(6, 6, 0, 0) });
        layout.Controls.Add(inputRow);
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = wrapWidth,
            ForeColor = SystemColors.GrayText,
            Text = BreakdownFormatter.GoogleMapsCaveat,
            Margin = new Padding(0, 6, 0, 0),
        });

        btnSave = new Button { AutoSize = true, Text = "Save", Enabled = DistanceInput.TryParse(txtDistance.Text, out _) };
        btnSave.Click += (_, _) =>
        {
            if (!DistanceInput.TryParse(txtDistance.Text, out double distance)) return;
            Distance = distance;
            DialogResult = DialogResult.OK;
        };
        var btnSkip = new Button { AutoSize = true, Text = "Skip", DialogResult = DialogResult.Ignore };
        var btnStop = new Button { AutoSize = true, Text = "Stop", DialogResult = DialogResult.Abort, Visible = moreJourneys };
        var btnCancel = new Button { AutoSize = true, Text = "Cancel", DialogResult = DialogResult.Cancel };

        if (moreJourneys)
        {
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = wrapWidth,
                ForeColor = SystemColors.GrayText,
                Text = "Stop keeps the figures saved so far. Cancel discards every figure from this run.",
                Margin = new Padding(0, 8, 0, 0),
            });
        }

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
        buttons.Controls.AddRange([btnCancel, btnStop, btnSkip, btnSave]);
        layout.Controls.Add(buttons);

        Controls.Add(layout);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
        ActiveControl = txtDistance;
    }
}
