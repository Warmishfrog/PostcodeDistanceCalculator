namespace PostcodeDistanceCalculator;

partial class PostcodeDistanceCalculator
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        panelHeader = new Panel();
        lblAppTitle = new Label();
        btnHelp = new Button();

        // Outer Splitter (Main Area vs History)
        splitContainer = new SplitContainer();

        // Inner Splitter (Input Area vs Results Breakdown)
        splitContainerLeft = new SplitContainer();

        // Reopen History Button (Visible when History Panel is collapsed)
        btnReopenHistory = new Button();

        // ToolTip
        toolTipInput = new ToolTip(components);

        // Top-Left Controls (Input)
        lblInputHeader = new Label();
        lblInputInfo = new Label();
        txtPostcodes = new TextBox();
        lblDateHeader = new Label();
        txtDate = new TextBox();
        grpUnits = new GroupBox();
        rdoMiles = new RadioButton();
        rdoKm = new RadioButton();
        btnCalculate = new Button();
        btnAddNew = new Button();

        // Bottom-Left Controls (Results Breakdown)
        lblBreakdownHeader = new Label();
        txtResults = new TextBox();
        lblJourneyTotal = new Label();
        btnCopyJourneyTotal = new Button();
        lblStatus = new Label();

        // Right Controls (History Sidebar)
        lblHistoryHeader = new Label();
        btnToggleHistory = new Button();
        lstHistory = new ListBox();
        lblHistoryTotal = new Label();
        btnCopyHistoryTotal = new Button();

        panelHeader.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();

        ((System.ComponentModel.ISupportInitialize)splitContainerLeft).BeginInit();
        splitContainerLeft.Panel1.SuspendLayout();
        splitContainerLeft.Panel2.SuspendLayout();
        splitContainerLeft.SuspendLayout();

        grpUnits.SuspendLayout();
        SuspendLayout();

        // 
        // panelHeader
        // 
        panelHeader.BackColor = Color.FromArgb(30, 41, 59);
        panelHeader.Controls.Add(lblAppTitle);
        panelHeader.Controls.Add(btnHelp);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Location = new Point(0, 0);
        panelHeader.Name = "panelHeader";
        panelHeader.Size = new Size(950, 50);
        panelHeader.TabIndex = 0;

        // 
        // lblAppTitle
        // 
        lblAppTitle.AutoSize = true;
        lblAppTitle.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);
        lblAppTitle.ForeColor = Color.White;
        lblAppTitle.Location = new Point(15, 13);
        lblAppTitle.Name = "lblAppTitle";
        lblAppTitle.Size = new Size(290, 23);
        lblAppTitle.TabIndex = 0;
        lblAppTitle.Text = "UK Postcode Distance Calculator";

        // 
        // btnHelp
        // 
        btnHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnHelp.BackColor = Color.FromArgb(51, 65, 85);
        btnHelp.FlatAppearance.BorderSize = 0;
        btnHelp.FlatStyle = FlatStyle.Flat;
        btnHelp.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnHelp.ForeColor = Color.White;
        btnHelp.Location = new Point(860, 11);
        btnHelp.Name = "btnHelp";
        btnHelp.Size = new Size(75, 28);
        btnHelp.TabIndex = 1;
        btnHelp.Text = "❓ Help";
        btnHelp.UseVisualStyleBackColor = false;
        btnHelp.Click += BtnHelp_Click;

        // 
        // Outer SplitContainer
        // 
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.Location = new Point(0, 50);
        splitContainer.Name = "splitContainer";
        splitContainer.Panel1.Controls.Add(splitContainerLeft);
        splitContainer.Panel1.Controls.Add(btnReopenHistory);
        splitContainer.Panel2.BackColor = Color.FromArgb(241, 245, 249);
        splitContainer.Panel2.Controls.Add(lblHistoryHeader);
        splitContainer.Panel2.Controls.Add(btnToggleHistory);
        splitContainer.Panel2.Controls.Add(lstHistory);
        splitContainer.Panel2.Controls.Add(lblHistoryTotal);
        splitContainer.Panel2.Controls.Add(btnCopyHistoryTotal);
        splitContainer.Panel2MinSize = 220;
        splitContainer.Size = new Size(950, 530);
        splitContainer.SplitterDistance = 620;
        splitContainer.SplitterWidth = 6;
        splitContainer.TabIndex = 1;

        // 
        // Reopen History Button
        // 
        btnReopenHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnReopenHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnReopenHistory.FlatAppearance.BorderSize = 0;
        btnReopenHistory.FlatStyle = FlatStyle.Flat;
        btnReopenHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnReopenHistory.ForeColor = Color.FromArgb(51, 65, 85);
        btnReopenHistory.Location = new Point(515, 6);
        btnReopenHistory.Name = "btnReopenHistory";
        btnReopenHistory.Size = new Size(100, 24);
        btnReopenHistory.TabIndex = 1;
        btnReopenHistory.Text = "📜 History ◀";
        btnReopenHistory.UseVisualStyleBackColor = false;
        btnReopenHistory.Visible = false;
        btnReopenHistory.Click += BtnToggleHistory_Click;

        // 
        // Inner SplitContainer
        // 
        splitContainerLeft.Dock = DockStyle.Fill;
        splitContainerLeft.Orientation = Orientation.Horizontal;
        splitContainerLeft.Location = new Point(0, 0);
        splitContainerLeft.Name = "splitContainerLeft";

        // Top Panel: Postcode Input
        splitContainerLeft.Panel1.BackColor = Color.FromArgb(248, 250, 252);
        splitContainerLeft.Panel1.Controls.Add(lblInputHeader);
        splitContainerLeft.Panel1.Controls.Add(lblInputInfo);
        splitContainerLeft.Panel1.Controls.Add(txtPostcodes);
        splitContainerLeft.Panel1.Controls.Add(lblDateHeader);
        splitContainerLeft.Panel1.Controls.Add(txtDate);
        splitContainerLeft.Panel1.Controls.Add(grpUnits);
        splitContainerLeft.Panel1.Controls.Add(btnCalculate);
        splitContainerLeft.Panel1.Controls.Add(btnAddNew);
        splitContainerLeft.Panel1MinSize = 120;

        // Bottom Panel: Journey Breakdown
        splitContainerLeft.Panel2.BackColor = Color.FromArgb(248, 250, 252);
        splitContainerLeft.Panel2.Controls.Add(lblBreakdownHeader);
        splitContainerLeft.Panel2.Controls.Add(txtResults);
        splitContainerLeft.Panel2.Controls.Add(lblJourneyTotal);
        splitContainerLeft.Panel2.Controls.Add(btnCopyJourneyTotal);
        splitContainerLeft.Panel2.Controls.Add(lblStatus);
        splitContainerLeft.Panel2MinSize = 0;

        splitContainerLeft.Size = new Size(620, 530);
        splitContainerLeft.SplitterDistance = 185;
        splitContainerLeft.SplitterWidth = 6;
        splitContainerLeft.TabIndex = 0;

        // 
        // TOP-LEFT: Input Controls
        // 
        lblInputHeader.AutoSize = true;
        lblInputHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblInputHeader.ForeColor = Color.FromArgb(217, 119, 6);
        lblInputHeader.Location = new Point(15, 10);
        lblInputHeader.Text = "Enter Postcodes:";

        // Info Icon next to Enter Postcodes header
        lblInputInfo.AutoSize = true;
        lblInputInfo.Cursor = Cursors.Hand;
        lblInputInfo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblInputInfo.ForeColor = Color.FromArgb(30, 64, 175);
        lblInputInfo.Location = new Point(130, 9);
        lblInputInfo.Text = "ℹ️";
        lblInputInfo.Click += BtnHelp_Click;

        txtPostcodes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtPostcodes.BorderStyle = BorderStyle.FixedSingle;
        txtPostcodes.Font = new Font("Segoe UI", 10F);
        txtPostcodes.Location = new Point(15, 31);
        txtPostcodes.Multiline = true;
        txtPostcodes.ScrollBars = ScrollBars.Vertical;
        txtPostcodes.Size = new Size(385, 105);

        // ToolTip Configurations
        string inputTip = "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;).";
        toolTipInput.SetToolTip(txtPostcodes, inputTip);
        toolTipInput.SetToolTip(lblInputHeader, inputTip);
        toolTipInput.SetToolTip(lblInputInfo, inputTip + "\nClick for full help.");
        toolTipInput.SetToolTip(btnReopenHistory, "Open Saved Journeys panel");

        // Date Controls
        lblDateHeader.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblDateHeader.AutoSize = true;
        lblDateHeader.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblDateHeader.ForeColor = Color.FromArgb(51, 65, 85);
        lblDateHeader.Location = new Point(412, 31);
        lblDateHeader.Text = "Date:";

        txtDate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        txtDate.BorderStyle = BorderStyle.FixedSingle;
        txtDate.Font = new Font("Segoe UI", 9.5F);
        txtDate.Location = new Point(412, 50);
        txtDate.Size = new Size(75, 24);

        // Compact Unit Group Box
        grpUnits.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        grpUnits.Controls.Add(rdoKm);
        grpUnits.Controls.Add(rdoMiles);
        grpUnits.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        grpUnits.ForeColor = Color.FromArgb(51, 65, 85);
        grpUnits.Location = new Point(497, 31);
        grpUnits.Size = new Size(105, 72);
        grpUnits.Text = "Unit";

        rdoMiles.AutoSize = true;
        rdoMiles.Checked = true;
        rdoMiles.Font = new Font("Segoe UI", 8.5F);
        rdoMiles.Location = new Point(10, 18);
        rdoMiles.Text = "Miles";
        rdoMiles.CheckedChanged += UnitToggle_CheckedChanged;

        rdoKm.AutoSize = true;
        rdoKm.Font = new Font("Segoe UI", 8.5F);
        rdoKm.Location = new Point(10, 42);
        rdoKm.Text = "km";
        rdoKm.CheckedChanged += UnitToggle_CheckedChanged;

        btnCalculate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnCalculate.BackColor = Color.FromArgb(217, 119, 6);
        btnCalculate.FlatAppearance.BorderSize = 0;
        btnCalculate.FlatStyle = FlatStyle.Flat;
        btnCalculate.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnCalculate.ForeColor = Color.White;
        btnCalculate.Location = new Point(15, 144);
        btnCalculate.Size = new Size(190, 32);
        btnCalculate.Text = "Calculate Route";
        btnCalculate.UseVisualStyleBackColor = false;
        btnCalculate.Click += BtnCalculate_Click;

        btnAddNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnAddNew.BackColor = Color.FromArgb(226, 232, 240);
        btnAddNew.FlatAppearance.BorderSize = 0;
        btnAddNew.FlatStyle = FlatStyle.Flat;
        btnAddNew.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnAddNew.ForeColor = Color.FromArgb(51, 65, 85);
        btnAddNew.Location = new Point(215, 144);
        btnAddNew.Size = new Size(165, 32);
        btnAddNew.Text = "+ Save to History";
        btnAddNew.UseVisualStyleBackColor = false;
        btnAddNew.Click += BtnAddNew_Click;
        btnAddNew.EnabledChanged += BtnAddNew_EnabledChanged;

        // 
        // BOTTOM-LEFT: Breakdown Controls
        // 
        lblBreakdownHeader.AutoSize = true;
        lblBreakdownHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblBreakdownHeader.ForeColor = Color.FromArgb(217, 119, 6);
        lblBreakdownHeader.Location = new Point(15, 8);
        lblBreakdownHeader.Text = "Journey Breakdown:";

        txtResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtResults.BackColor = Color.White;
        txtResults.BorderStyle = BorderStyle.FixedSingle;
        txtResults.Font = new Font("Consolas", 10F);
        txtResults.ForeColor = Color.FromArgb(15, 23, 42);
        txtResults.Location = new Point(15, 30);
        txtResults.Multiline = true;
        txtResults.ReadOnly = true;
        txtResults.ScrollBars = ScrollBars.Vertical;
        txtResults.Size = new Size(587, 240);

        lblJourneyTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblJourneyTotal.AutoSize = true;
        lblJourneyTotal.Cursor = Cursors.Hand;
        lblJourneyTotal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblJourneyTotal.ForeColor = Color.FromArgb(30, 41, 59);
        lblJourneyTotal.Location = new Point(15, 280);
        lblJourneyTotal.Text = "Journey Total: 0.0 mi";

        btnCopyJourneyTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnCopyJourneyTotal.BackColor = Color.FromArgb(226, 232, 240);
        btnCopyJourneyTotal.FlatAppearance.BorderSize = 0;
        btnCopyJourneyTotal.FlatStyle = FlatStyle.Flat;
        btnCopyJourneyTotal.Font = new Font("Segoe UI", 9F);
        btnCopyJourneyTotal.Location = new Point(210, 277);
        btnCopyJourneyTotal.Size = new Size(70, 26);
        btnCopyJourneyTotal.Text = "📋 Copy";
        btnCopyJourneyTotal.UseVisualStyleBackColor = false;
        btnCopyJourneyTotal.Click += BtnCopyJourneyTotal_Click;
        toolTipInput.SetToolTip(btnCopyJourneyTotal, "Copy Journey Total");

        // Fixed width + ellipsis so long error messages don't run off the panel (full text shows on hover)
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.AutoSize = false;
        lblStatus.AutoEllipsis = true;
        lblStatus.Size = new Size(587, 17);
        lblStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Location = new Point(15, 312);
        lblStatus.Text = "Ready for postcodes.";

        // 
        // RIGHT: History Controls
        // 
        lblHistoryHeader.AutoSize = true;
        lblHistoryHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblHistoryHeader.ForeColor = Color.FromArgb(30, 41, 59);
        lblHistoryHeader.Location = new Point(10, 10);
        lblHistoryHeader.Text = "Saved Journeys:";

        btnToggleHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggleHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnToggleHistory.FlatAppearance.BorderSize = 0;
        btnToggleHistory.FlatStyle = FlatStyle.Flat;
        btnToggleHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnToggleHistory.ForeColor = Color.FromArgb(51, 65, 85);
        btnToggleHistory.Location = new Point(280, 6);
        btnToggleHistory.Name = "btnToggleHistory";
        btnToggleHistory.Size = new Size(35, 24);
        btnToggleHistory.TabIndex = 0;
        btnToggleHistory.Text = "▶";
        btnToggleHistory.UseVisualStyleBackColor = false;
        btnToggleHistory.Click += BtnToggleHistory_Click;
        toolTipInput.SetToolTip(btnToggleHistory, "Close Saved Journeys panel");

        lstHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstHistory.BorderStyle = BorderStyle.FixedSingle;
        lstHistory.DrawMode = DrawMode.OwnerDrawFixed;
        lstHistory.Font = new Font("Segoe UI", 9F);
        lstHistory.ItemHeight = 26;
        lstHistory.Location = new Point(10, 35);
        lstHistory.Size = new Size(305, 431);
        lstHistory.IntegralHeight = false;
        lstHistory.DrawItem += LstHistory_DrawItem;
        lstHistory.MouseDown += LstHistory_MouseDown;
        lstHistory.SelectedIndexChanged += LstHistory_SelectedIndexChanged;
        //lstHistory.SizeChanged += (s, e) => lstHistory.Invalidate();

        lblHistoryTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblHistoryTotal.AutoSize = true;
        lblHistoryTotal.Cursor = Cursors.Hand;
        lblHistoryTotal.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblHistoryTotal.ForeColor = Color.FromArgb(30, 41, 59);
        lblHistoryTotal.Location = new Point(10, 480);
        lblHistoryTotal.Text = "History Total: 0.0 mi";

        btnCopyHistoryTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCopyHistoryTotal.BackColor = Color.FromArgb(226, 232, 240);
        btnCopyHistoryTotal.FlatAppearance.BorderSize = 0;
        btnCopyHistoryTotal.FlatStyle = FlatStyle.Flat;
        btnCopyHistoryTotal.Font = new Font("Segoe UI", 9F);
        btnCopyHistoryTotal.Location = new Point(245, 477);
        btnCopyHistoryTotal.Size = new Size(70, 26);
        btnCopyHistoryTotal.Text = "📋 Copy";
        btnCopyHistoryTotal.UseVisualStyleBackColor = false;
        btnCopyHistoryTotal.Click += BtnCopyHistoryTotal_Click;
        toolTipInput.SetToolTip(btnCopyHistoryTotal, "Copy History Total");

        // 
        // Form Configuration
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(950, 580);
        Controls.Add(splitContainer);
        Controls.Add(panelHeader);
        MinimumSize = new Size(720, 520);
        Name = "PostcodeDistanceCalculator";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "UK Postcode Distance Calculator";

        panelHeader.ResumeLayout(false);
        panelHeader.PerformLayout();

        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        splitContainer.Panel2.PerformLayout();
        splitContainer.ResumeLayout(false);

        ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
        splitContainerLeft.Panel1.ResumeLayout(false);
        splitContainerLeft.Panel1.PerformLayout();
        splitContainerLeft.Panel2.ResumeLayout(false);
        splitContainerLeft.Panel2.PerformLayout();
        splitContainerLeft.ResumeLayout(false);

        grpUnits.ResumeLayout(false);
        grpUnits.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private Panel panelHeader;
    private Label lblAppTitle;
    private Button btnHelp;

    private SplitContainer splitContainer;
    private SplitContainer splitContainerLeft;
    private Button btnReopenHistory;
    private ToolTip toolTipInput;

    private Label lblInputHeader;
    private Label lblInputInfo;
    private TextBox txtPostcodes;
    private Label lblDateHeader;
    private TextBox txtDate;
    private GroupBox grpUnits;
    private RadioButton rdoMiles;
    private RadioButton rdoKm;
    private Button btnCalculate;
    private Button btnAddNew;

    private Label lblBreakdownHeader;
    private TextBox txtResults;
    private Label lblJourneyTotal;
    private Button btnCopyJourneyTotal;
    private Label lblStatus;

    private Label lblHistoryHeader;
    private Button btnToggleHistory;
    private ListBox lstHistory;
    private Label lblHistoryTotal;
    private Button btnCopyHistoryTotal;
}