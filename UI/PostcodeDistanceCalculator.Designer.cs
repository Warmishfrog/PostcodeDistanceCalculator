namespace PostcodeDistanceCalculator.UI;

partial class PostcodeDistanceCalculator
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        panelHeader = new Panel();
        lblAppTitle = new Label();
        btnHelp = new Button();
        splitContainer = new SplitContainer();
        splitContainerLeft = new SplitContainer();
        lblInputHeader = new Label();
        lblInputInfo = new Label();
        lnkWarnings = new LinkLabel();
        txtPostcodes = new WarningTextBox();
        lblDateHeader = new Label();
        txtDate = new TextBox();
        grpUnits = new GroupBox();
        rdoKm = new RadioButton();
        rdoMiles = new RadioButton();
        btnCalculate = new Button();
        btnAddNew = new Button();
        lblBreakdownHeader = new Label();
        flowBreakdownOptions = new FlowLayoutPanel();
        chkCompact = new CheckBox();
        chkLegDistances = new CheckBox();
        chkShowEstimate = new CheckBox();
        txtResults = new WarningTextBox();
        flowJourneyTotal = new FlowLayoutPanel();
        lblJourneyTotal = new Label();
        btnCopyJourneyTotal = new Button();
        btnVerifyJourney = new Button();
        lblStatus = new Label();
        btnReopenHistory = new Button();
        lblHistoryHeader = new Label();
        lstHistory = new ListBox();
        lblHistoryTotal = new Label();
        btnCopyHistoryTotal = new Button();
        btnImportHistory = new Button();
        btnExportHistory = new Button();
        btnClearHistory = new Button();
        btnToggleHistory = new Button();
        toolTipInput = new ToolTip(components);
        mnuHistory = new ContextMenuStrip(components);
        mnuVerify = new ToolStripMenuItem();
        mnuOpenInMaps = new ToolStripMenuItem();
        mnuUseEstimate = new ToolStripMenuItem();
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
        flowJourneyTotal.SuspendLayout();
        flowBreakdownOptions.SuspendLayout();
        mnuHistory.SuspendLayout();
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
        panelHeader.TabIndex = 1;
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
        btnHelp.ImageAlign = ContentAlignment.MiddleLeft;
        btnHelp.Location = new Point(860, 11);
        btnHelp.Name = "btnHelp";
        btnHelp.Size = new Size(75, 28);
        btnHelp.TabIndex = 1;
        btnHelp.Text = "Help";
        btnHelp.TextImageRelation = TextImageRelation.ImageBeforeText;
        btnHelp.UseVisualStyleBackColor = false;
        btnHelp.Click += BtnHelp_Click;
        //
        // splitContainer
        //
        splitContainer.BackColor = Color.FromArgb(203, 213, 225);
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.Location = new Point(0, 50);
        splitContainer.Name = "splitContainer";
        //
        // splitContainer.Panel1
        //
        splitContainer.Panel1.Controls.Add(splitContainerLeft);
        splitContainer.Panel1.Controls.Add(btnReopenHistory);
        //
        // splitContainer.Panel2
        //
        splitContainer.Panel2.BackColor = Color.FromArgb(241, 245, 249);
        splitContainer.Panel2.Controls.Add(lblHistoryHeader);
        splitContainer.Panel2.Controls.Add(lstHistory);
        splitContainer.Panel2.Controls.Add(lblHistoryTotal);
        splitContainer.Panel2.Controls.Add(btnCopyHistoryTotal);
        splitContainer.Panel2.Controls.Add(btnImportHistory);
        splitContainer.Panel2.Controls.Add(btnExportHistory);
        splitContainer.Panel2.Controls.Add(btnClearHistory);
        splitContainer.Panel2.Controls.Add(btnToggleHistory);
        splitContainer.Panel1MinSize = 400;
        splitContainer.Panel2MinSize = 300;
        splitContainer.Size = new Size(950, 530);
        splitContainer.SplitterDistance = 620;
        splitContainer.SplitterWidth = 8;
        splitContainer.TabIndex = 0;
        //
        // splitContainerLeft
        //
        splitContainerLeft.BackColor = Color.FromArgb(203, 213, 225);
        splitContainerLeft.Dock = DockStyle.Fill;
        splitContainerLeft.Location = new Point(0, 0);
        splitContainerLeft.Name = "splitContainerLeft";
        splitContainerLeft.Orientation = Orientation.Horizontal;
        //
        // splitContainerLeft.Panel1
        //
        splitContainerLeft.Panel1.BackColor = Color.FromArgb(248, 250, 252);
        splitContainerLeft.Panel1.Controls.Add(lblInputHeader);
        splitContainerLeft.Panel1.Controls.Add(lblInputInfo);
        splitContainerLeft.Panel1.Controls.Add(lnkWarnings);
        splitContainerLeft.Panel1.Controls.Add(txtPostcodes);
        splitContainerLeft.Panel1.Controls.Add(lblDateHeader);
        splitContainerLeft.Panel1.Controls.Add(txtDate);
        splitContainerLeft.Panel1.Controls.Add(grpUnits);
        splitContainerLeft.Panel1.Controls.Add(btnCalculate);
        splitContainerLeft.Panel1.Controls.Add(btnAddNew);
        splitContainerLeft.Panel1MinSize = 120;
        //
        // splitContainerLeft.Panel2
        //
        splitContainerLeft.Panel2.BackColor = Color.FromArgb(248, 250, 252);
        splitContainerLeft.Panel2.Controls.Add(lblBreakdownHeader);
        splitContainerLeft.Panel2.Controls.Add(flowBreakdownOptions);
        splitContainerLeft.Panel2.Controls.Add(txtResults);
        splitContainerLeft.Panel2.Controls.Add(flowJourneyTotal);
        splitContainerLeft.Panel2.Controls.Add(lblStatus);
        splitContainerLeft.Panel2MinSize = 150;
        splitContainerLeft.Size = new Size(620, 530);
        splitContainerLeft.SplitterDistance = 185;
        splitContainerLeft.SplitterWidth = 8;
        splitContainerLeft.TabIndex = 0;
        //
        // lblInputHeader
        //
        lblInputHeader.AutoSize = true;
        lblInputHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblInputHeader.ForeColor = Color.FromArgb(217, 119, 6);
        lblInputHeader.Location = new Point(15, 10);
        lblInputHeader.Name = "lblInputHeader";
        lblInputHeader.Size = new Size(113, 17);
        lblInputHeader.TabIndex = 0;
        lblInputHeader.Text = "Enter Postcodes:";
        toolTipInput.SetToolTip(lblInputHeader, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;), or one per line starting with a date (21st: AB12 3CD, EF45 6GH).");
        //
        // lblInputInfo
        //
        lblInputInfo.AccessibleName = "Help";
        lblInputInfo.Cursor = Cursors.Hand;
        lblInputInfo.ForeColor = Color.FromArgb(30, 64, 175);
        lblInputInfo.ImageAlign = ContentAlignment.MiddleCenter;
        lblInputInfo.Location = new Point(130, 9);
        lblInputInfo.Name = "lblInputInfo";
        lblInputInfo.Size = new Size(20, 20);
        lblInputInfo.TabIndex = 1;
        toolTipInput.SetToolTip(lblInputInfo, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;), or one per line starting with a date (21st: AB12 3CD, EF45 6GH).\nClick for full help.");
        lblInputInfo.Click += BtnHelp_Click;
        //
        // lnkWarnings
        //
        lnkWarnings.ActiveLinkColor = Color.FromArgb(180, 83, 9);
        lnkWarnings.AutoSize = true;
        lnkWarnings.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lnkWarnings.LinkColor = Color.FromArgb(217, 119, 6);
        lnkWarnings.Location = new Point(156, 11);
        lnkWarnings.Name = "lnkWarnings";
        lnkWarnings.Size = new Size(130, 15);
        lnkWarnings.TabIndex = 9;
        lnkWarnings.TabStop = true;
        lnkWarnings.Text = "1 postcode to check";
        toolTipInput.SetToolTip(lnkWarnings, "Some postcodes may have typos. Click for details.");
        lnkWarnings.Visible = false;
        lnkWarnings.LinkClicked += LnkWarnings_LinkClicked;
        //
        // txtPostcodes
        //
        txtPostcodes.AcceptsReturn = true;
        txtPostcodes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtPostcodes.BorderStyle = BorderStyle.FixedSingle;
        txtPostcodes.Font = new Font("Segoe UI", 10F);
        txtPostcodes.Location = new Point(15, 31);
        txtPostcodes.Multiline = true;
        txtPostcodes.Name = "txtPostcodes";
        txtPostcodes.PlaceholderText = "One journey:  SW1A 1AA, M1 1AE, EH1 1YZ\r\nSeveral journeys:  SW1A 1AA, M1 1AE ; B1 1AA, LS1 1UR\r\nDated journeys, one per line:  21st: BS15 8DB, BS5 8NG\r\nPostcodes can also go one per line.";
        txtPostcodes.ScrollBars = ScrollBars.Vertical;
        txtPostcodes.Size = new Size(385, 105);
        txtPostcodes.TabIndex = 2;
        toolTipInput.SetToolTip(txtPostcodes, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;), or one per line starting with a date (21st: AB12 3CD, EF45 6GH).\nPress Ctrl+Enter to calculate.");
        txtPostcodes.TextChanged += TxtPostcodes_TextChanged;
        txtPostcodes.KeyDown += TxtPostcodes_KeyDown;
        //
        // lblDateHeader
        //
        lblDateHeader.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblDateHeader.AutoSize = true;
        lblDateHeader.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblDateHeader.ForeColor = Color.FromArgb(51, 65, 85);
        lblDateHeader.Location = new Point(412, 31);
        lblDateHeader.Name = "lblDateHeader";
        lblDateHeader.Size = new Size(36, 15);
        lblDateHeader.TabIndex = 3;
        lblDateHeader.Text = "Date:";
        //
        // txtDate
        //
        txtDate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        txtDate.BorderStyle = BorderStyle.FixedSingle;
        txtDate.Font = new Font("Segoe UI", 9.5F);
        txtDate.Location = new Point(412, 50);
        txtDate.Name = "txtDate";
        txtDate.Size = new Size(75, 24);
        txtDate.TabIndex = 4;
        txtDate.KeyDown += TxtDate_KeyDown;
        //
        // grpUnits
        //
        grpUnits.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        grpUnits.Controls.Add(rdoKm);
        grpUnits.Controls.Add(rdoMiles);
        grpUnits.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        grpUnits.ForeColor = Color.FromArgb(51, 65, 85);
        grpUnits.Location = new Point(497, 31);
        grpUnits.Name = "grpUnits";
        grpUnits.Size = new Size(105, 72);
        grpUnits.TabIndex = 5;
        grpUnits.TabStop = false;
        grpUnits.Text = "Unit";
        //
        // rdoKm
        //
        rdoKm.AutoSize = true;
        rdoKm.Font = new Font("Segoe UI", 8.5F);
        rdoKm.Location = new Point(10, 42);
        rdoKm.Name = "rdoKm";
        rdoKm.Size = new Size(40, 19);
        rdoKm.TabIndex = 1;
        rdoKm.Text = "km";
        rdoKm.CheckedChanged += UnitToggle_CheckedChanged;
        //
        // rdoMiles
        //
        rdoMiles.AutoSize = true;
        rdoMiles.Checked = true;
        rdoMiles.Font = new Font("Segoe UI", 8.5F);
        rdoMiles.Location = new Point(10, 18);
        rdoMiles.Name = "rdoMiles";
        rdoMiles.Size = new Size(52, 19);
        rdoMiles.TabIndex = 0;
        rdoMiles.TabStop = true;
        rdoMiles.Text = "Miles";
        rdoMiles.CheckedChanged += UnitToggle_CheckedChanged;
        //
        // btnCalculate
        //
        btnCalculate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnCalculate.BackColor = Color.FromArgb(217, 119, 6);
        btnCalculate.FlatAppearance.BorderSize = 0;
        btnCalculate.FlatStyle = FlatStyle.Flat;
        btnCalculate.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnCalculate.ForeColor = Color.White;
        btnCalculate.Location = new Point(15, 144);
        btnCalculate.Name = "btnCalculate";
        btnCalculate.Size = new Size(190, 32);
        btnCalculate.TabIndex = 6;
        btnCalculate.Text = "Calculate Route";
        toolTipInput.SetToolTip(btnCalculate, "Calculate (Ctrl+Enter in the postcode box)");
        btnCalculate.UseVisualStyleBackColor = false;
        btnCalculate.Click += BtnCalculate_Click;
        //
        // btnAddNew
        //
        btnAddNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnAddNew.BackColor = Color.FromArgb(226, 232, 240);
        btnAddNew.FlatAppearance.BorderSize = 0;
        btnAddNew.FlatStyle = FlatStyle.Flat;
        btnAddNew.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnAddNew.ForeColor = Color.FromArgb(51, 65, 85);
        btnAddNew.Location = new Point(215, 144);
        btnAddNew.Name = "btnAddNew";
        btnAddNew.Size = new Size(165, 32);
        btnAddNew.TabIndex = 7;
        btnAddNew.Text = "+ Save to History";
        btnAddNew.UseVisualStyleBackColor = false;
        btnAddNew.EnabledChanged += BtnAddNew_EnabledChanged;
        btnAddNew.Click += BtnAddNew_Click;
        //
        // lblBreakdownHeader
        //
        lblBreakdownHeader.AutoSize = true;
        lblBreakdownHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblBreakdownHeader.ForeColor = Color.FromArgb(217, 119, 6);
        lblBreakdownHeader.Location = new Point(15, 8);
        lblBreakdownHeader.Name = "lblBreakdownHeader";
        lblBreakdownHeader.Size = new Size(130, 17);
        lblBreakdownHeader.TabIndex = 0;
        lblBreakdownHeader.Text = "Journey Breakdown:";
        //
        // flowBreakdownOptions
        //
        flowBreakdownOptions.AutoSize = true;
        flowBreakdownOptions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowBreakdownOptions.Controls.Add(chkCompact);
        flowBreakdownOptions.Controls.Add(chkLegDistances);
        flowBreakdownOptions.Controls.Add(chkShowEstimate);
        flowBreakdownOptions.Location = new Point(160, 6);
        flowBreakdownOptions.Name = "flowBreakdownOptions";
        flowBreakdownOptions.Size = new Size(330, 21);
        flowBreakdownOptions.TabIndex = 4;
        flowBreakdownOptions.WrapContents = false;
        //
        // chkCompact
        //
        chkCompact.AutoSize = true;
        chkCompact.Font = new Font("Segoe UI", 8.5F);
        chkCompact.ForeColor = Color.FromArgb(51, 65, 85);
        chkCompact.Margin = new Padding(0, 0, 10, 0);
        chkCompact.Name = "chkCompact";
        chkCompact.TabIndex = 0;
        chkCompact.Text = "Compact";
        toolTipInput.SetToolTip(chkCompact, "Show each journey's legs on one line instead of the full breakdown");
        chkCompact.CheckedChanged += BreakdownOption_CheckedChanged;
        //
        // chkLegDistances
        //
        chkLegDistances.AutoSize = true;
        chkLegDistances.Checked = true;
        chkLegDistances.CheckState = CheckState.Checked;
        chkLegDistances.Font = new Font("Segoe UI", 8.5F);
        chkLegDistances.ForeColor = Color.FromArgb(51, 65, 85);
        chkLegDistances.Margin = new Padding(0, 0, 10, 0);
        chkLegDistances.Name = "chkLegDistances";
        chkLegDistances.TabIndex = 1;
        chkLegDistances.Text = "Leg distances";
        toolTipInput.SetToolTip(chkLegDistances, "Show the distance of each individual leg");
        chkLegDistances.CheckedChanged += BreakdownOption_CheckedChanged;
        //
        // chkShowEstimate
        //
        chkShowEstimate.AutoSize = true;
        chkShowEstimate.Checked = true;
        chkShowEstimate.CheckState = CheckState.Checked;
        chkShowEstimate.Font = new Font("Segoe UI", 8.5F);
        chkShowEstimate.ForeColor = Color.FromArgb(51, 65, 85);
        chkShowEstimate.Margin = new Padding(0);
        chkShowEstimate.Name = "chkShowEstimate";
        chkShowEstimate.TabIndex = 2;
        chkShowEstimate.Text = "Estimate vs Google";
        toolTipInput.SetToolTip(chkShowEstimate, "For journeys verified with Google Maps, also show this app's own estimate");
        chkShowEstimate.CheckedChanged += BreakdownOption_CheckedChanged;
        //
        // txtResults
        //
        txtResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtResults.BackColor = Color.White;
        txtResults.BorderStyle = BorderStyle.FixedSingle;
        txtResults.Font = new Font("Consolas", 10F);
        txtResults.ForeColor = Color.FromArgb(15, 23, 42);
        txtResults.Location = new Point(15, 30);
        txtResults.Multiline = true;
        txtResults.Name = "txtResults";
        txtResults.ReadOnly = true;
        txtResults.ScrollBars = ScrollBars.Vertical;
        txtResults.Size = new Size(587, 240);
        txtResults.TabIndex = 1;
        //
        // flowJourneyTotal
        //
        flowJourneyTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        flowJourneyTotal.AutoSize = true;
        flowJourneyTotal.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flowJourneyTotal.Controls.Add(lblJourneyTotal);
        flowJourneyTotal.Controls.Add(btnCopyJourneyTotal);
        flowJourneyTotal.Controls.Add(btnVerifyJourney);
        flowJourneyTotal.Location = new Point(15, 277);
        flowJourneyTotal.Name = "flowJourneyTotal";
        flowJourneyTotal.Size = new Size(319, 26);
        flowJourneyTotal.TabIndex = 2;
        flowJourneyTotal.WrapContents = false;
        //
        // lblJourneyTotal
        //
        lblJourneyTotal.AutoSize = true;
        lblJourneyTotal.Cursor = Cursors.Hand;
        lblJourneyTotal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblJourneyTotal.ForeColor = Color.FromArgb(30, 41, 59);
        lblJourneyTotal.Location = new Point(0, 3);
        lblJourneyTotal.Margin = new Padding(0, 3, 8, 0);
        lblJourneyTotal.Name = "lblJourneyTotal";
        lblJourneyTotal.Size = new Size(160, 20);
        lblJourneyTotal.TabIndex = 0;
        lblJourneyTotal.Text = "Journey Total: 0.0 mi";
        lblJourneyTotal.Click += LblJourneyTotal_Click;
        //
        // btnCopyJourneyTotal
        //
        btnCopyJourneyTotal.BackColor = Color.FromArgb(226, 232, 240);
        btnCopyJourneyTotal.FlatAppearance.BorderSize = 0;
        btnCopyJourneyTotal.FlatStyle = FlatStyle.Flat;
        btnCopyJourneyTotal.Font = new Font("Segoe UI", 9F);
        btnCopyJourneyTotal.ImageAlign = ContentAlignment.MiddleLeft;
        btnCopyJourneyTotal.Location = new Point(168, 0);
        btnCopyJourneyTotal.Margin = new Padding(0);
        btnCopyJourneyTotal.Name = "btnCopyJourneyTotal";
        btnCopyJourneyTotal.Size = new Size(70, 26);
        btnCopyJourneyTotal.TabIndex = 1;
        btnCopyJourneyTotal.Text = "Copy";
        btnCopyJourneyTotal.TextImageRelation = TextImageRelation.ImageBeforeText;
        toolTipInput.SetToolTip(btnCopyJourneyTotal, "Copy Journey Total");
        btnCopyJourneyTotal.UseVisualStyleBackColor = false;
        btnCopyJourneyTotal.Click += BtnCopyJourneyTotal_Click;
        // 
        // btnVerifyJourney
        // 
        btnVerifyJourney.BackColor = Color.FromArgb(226, 232, 240);
        btnVerifyJourney.Enabled = false;
        btnVerifyJourney.FlatAppearance.BorderSize = 0;
        btnVerifyJourney.FlatStyle = FlatStyle.Flat;
        btnVerifyJourney.Font = new Font("Segoe UI", 9F);
        btnVerifyJourney.ImageAlign = ContentAlignment.MiddleLeft;
        btnVerifyJourney.Location = new Point(244, 0);
        btnVerifyJourney.Margin = new Padding(6, 0, 0, 0);
        btnVerifyJourney.Name = "btnVerifyJourney";
        btnVerifyJourney.AutoSize = true;
        btnVerifyJourney.Size = new Size(75, 26);
        btnVerifyJourney.TabIndex = 2;
        btnVerifyJourney.Text = "Verify in Google Maps";
        btnVerifyJourney.TextImageRelation = TextImageRelation.ImageBeforeText;
        toolTipInput.SetToolTip(btnVerifyJourney, "Opens each journey in Google Maps in your web browser, so you can type in Google's distance to replace this app's estimate");
        btnVerifyJourney.UseVisualStyleBackColor = false;
        btnVerifyJourney.Click += BtnVerifyJourney_Click;
        //
        // lblStatus
        //
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lblStatus.AutoEllipsis = true;
        lblStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblStatus.ForeColor = Color.FromArgb(100, 116, 139);
        lblStatus.Location = new Point(15, 312);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(587, 17);
        lblStatus.TabIndex = 3;
        lblStatus.Text = "Ready for postcodes.";
        //
        // btnReopenHistory
        //
        btnReopenHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnReopenHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnReopenHistory.FlatAppearance.BorderSize = 0;
        btnReopenHistory.FlatStyle = FlatStyle.Flat;
        btnReopenHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnReopenHistory.ForeColor = Color.FromArgb(51, 65, 85);
        btnReopenHistory.ImageAlign = ContentAlignment.MiddleLeft;
        btnReopenHistory.Location = new Point(515, 6);
        btnReopenHistory.Name = "btnReopenHistory";
        btnReopenHistory.Size = new Size(100, 24);
        btnReopenHistory.TabIndex = 1;
        btnReopenHistory.Text = "History";
        btnReopenHistory.TextImageRelation = TextImageRelation.ImageBeforeText;
        toolTipInput.SetToolTip(btnReopenHistory, "Open Saved Journeys panel");
        btnReopenHistory.UseVisualStyleBackColor = false;
        btnReopenHistory.Visible = false;
        btnReopenHistory.Click += BtnToggleHistory_Click;
        //
        // lblHistoryHeader
        //
        lblHistoryHeader.AutoSize = true;
        lblHistoryHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblHistoryHeader.ForeColor = Color.FromArgb(30, 41, 59);
        lblHistoryHeader.Location = new Point(10, 10);
        lblHistoryHeader.Name = "lblHistoryHeader";
        lblHistoryHeader.Size = new Size(55, 17);
        lblHistoryHeader.TabIndex = 0;
        lblHistoryHeader.Text = "History:";
        //
        // lstHistory
        //
        lstHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstHistory.BorderStyle = BorderStyle.FixedSingle;
        lstHistory.ContextMenuStrip = mnuHistory;
        lstHistory.DrawMode = DrawMode.OwnerDrawFixed;
        lstHistory.Font = new Font("Segoe UI", 9F);
        lstHistory.FormattingEnabled = true;
        lstHistory.IntegralHeight = false;
        lstHistory.ItemHeight = 26;
        lstHistory.Location = new Point(10, 35);
        lstHistory.Name = "lstHistory";
        lstHistory.SelectionMode = SelectionMode.MultiExtended;
        lstHistory.Size = new Size(305, 431);
        lstHistory.TabIndex = 1;
        lstHistory.DrawItem += LstHistory_DrawItem;
        lstHistory.Format += LstHistory_Format;
        lstHistory.SelectedIndexChanged += LstHistory_SelectedIndexChanged;
        lstHistory.KeyDown += LstHistory_KeyDown;
        lstHistory.MouseDown += LstHistory_MouseDown;
        //
        // lblHistoryTotal
        //
        lblHistoryTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblHistoryTotal.AutoSize = true;
        lblHistoryTotal.Cursor = Cursors.Hand;
        lblHistoryTotal.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        lblHistoryTotal.ForeColor = Color.FromArgb(30, 41, 59);
        lblHistoryTotal.Location = new Point(10, 480);
        lblHistoryTotal.Name = "lblHistoryTotal";
        lblHistoryTotal.Size = new Size(150, 19);
        lblHistoryTotal.TabIndex = 2;
        lblHistoryTotal.Text = "History Total: 0.0 mi";
        lblHistoryTotal.Click += LblHistoryTotal_Click;
        //
        // btnCopyHistoryTotal
        //
        btnCopyHistoryTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnCopyHistoryTotal.BackColor = Color.FromArgb(226, 232, 240);
        btnCopyHistoryTotal.FlatAppearance.BorderSize = 0;
        btnCopyHistoryTotal.FlatStyle = FlatStyle.Flat;
        btnCopyHistoryTotal.Font = new Font("Segoe UI", 9F);
        btnCopyHistoryTotal.ImageAlign = ContentAlignment.MiddleLeft;
        btnCopyHistoryTotal.Location = new Point(245, 477);
        btnCopyHistoryTotal.Name = "btnCopyHistoryTotal";
        btnCopyHistoryTotal.Size = new Size(70, 26);
        btnCopyHistoryTotal.TabIndex = 3;
        btnCopyHistoryTotal.Text = "Copy";
        btnCopyHistoryTotal.TextImageRelation = TextImageRelation.ImageBeforeText;
        toolTipInput.SetToolTip(btnCopyHistoryTotal, "Copy History Total");
        btnCopyHistoryTotal.UseVisualStyleBackColor = false;
        btnCopyHistoryTotal.Click += BtnCopyHistoryTotal_Click;
        //
        // btnImportHistory
        //
        btnImportHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnImportHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnImportHistory.FlatAppearance.BorderSize = 0;
        btnImportHistory.FlatStyle = FlatStyle.Flat;
        btnImportHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnImportHistory.ForeColor = Color.FromArgb(30, 41, 59);
        btnImportHistory.Location = new Point(100, 6);
        btnImportHistory.Name = "btnImportHistory";
        btnImportHistory.Size = new Size(55, 24);
        btnImportHistory.TabIndex = 4;
        btnImportHistory.Text = "Import";
        toolTipInput.SetToolTip(btnImportHistory, "Add saved journeys from a .txt file made with Export");
        btnImportHistory.UseVisualStyleBackColor = false;
        btnImportHistory.Click += BtnImportHistory_Click;
        //
        // btnExportHistory
        //
        btnExportHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExportHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnExportHistory.FlatAppearance.BorderSize = 0;
        btnExportHistory.FlatStyle = FlatStyle.Flat;
        btnExportHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnExportHistory.ForeColor = Color.FromArgb(30, 41, 59);
        btnExportHistory.Location = new Point(160, 6);
        btnExportHistory.Name = "btnExportHistory";
        btnExportHistory.Size = new Size(55, 24);
        btnExportHistory.TabIndex = 5;
        btnExportHistory.Text = "Export";
        toolTipInput.SetToolTip(btnExportHistory, "Save all saved journeys to a .txt file");
        btnExportHistory.UseVisualStyleBackColor = false;
        btnExportHistory.Click += BtnExportHistory_Click;
        //
        // btnClearHistory
        //
        btnClearHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnClearHistory.FlatAppearance.BorderSize = 0;
        btnClearHistory.FlatStyle = FlatStyle.Flat;
        btnClearHistory.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        btnClearHistory.ForeColor = Color.FromArgb(185, 28, 28);
        btnClearHistory.Location = new Point(220, 6);
        btnClearHistory.Name = "btnClearHistory";
        btnClearHistory.Size = new Size(55, 24);
        btnClearHistory.TabIndex = 6;
        btnClearHistory.Text = "Clear";
        toolTipInput.SetToolTip(btnClearHistory, "Delete all saved journeys");
        btnClearHistory.UseVisualStyleBackColor = false;
        btnClearHistory.Click += BtnClearHistory_Click;
        //
        // btnToggleHistory
        //
        btnToggleHistory.AccessibleName = "Close Saved Journeys panel";
        btnToggleHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggleHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnToggleHistory.FlatAppearance.BorderSize = 0;
        btnToggleHistory.FlatStyle = FlatStyle.Flat;
        btnToggleHistory.ForeColor = Color.FromArgb(51, 65, 85);
        btnToggleHistory.Location = new Point(280, 6);
        btnToggleHistory.Name = "btnToggleHistory";
        btnToggleHistory.Size = new Size(35, 24);
        btnToggleHistory.TabIndex = 7;
        toolTipInput.SetToolTip(btnToggleHistory, "Close Saved Journeys panel");
        btnToggleHistory.UseVisualStyleBackColor = false;
        btnToggleHistory.Click += BtnToggleHistory_Click;
        // 
        // mnuHistory
        // 
        mnuHistory.Items.AddRange(new ToolStripItem[] { mnuVerify, mnuOpenInMaps, mnuUseEstimate });
        mnuHistory.Name = "mnuHistory";
        mnuHistory.Size = new Size(260, 70);
        mnuHistory.Opening += MnuHistory_Opening;
        // 
        // mnuVerify
        // 
        mnuVerify.Name = "mnuVerify";
        mnuVerify.Size = new Size(259, 22);
        mnuVerify.Text = "Verify with Google Maps...";
        mnuVerify.Click += MnuVerify_Click;
        // 
        // mnuOpenInMaps
        // 
        mnuOpenInMaps.Name = "mnuOpenInMaps";
        mnuOpenInMaps.Size = new Size(259, 22);
        mnuOpenInMaps.Text = "Open in Google Maps";
        mnuOpenInMaps.Click += MnuOpenInMaps_Click;
        // 
        // mnuUseEstimate
        // 
        mnuUseEstimate.Name = "mnuUseEstimate";
        mnuUseEstimate.Size = new Size(259, 22);
        mnuUseEstimate.Text = "Use This App's Estimate";
        mnuUseEstimate.Click += MnuUseEstimate_Click;
        //
        // PostcodeDistanceCalculator
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
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        splitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        splitContainerLeft.Panel1.ResumeLayout(false);
        splitContainerLeft.Panel1.PerformLayout();
        flowJourneyTotal.ResumeLayout(false);
        flowBreakdownOptions.ResumeLayout(false);
        flowBreakdownOptions.PerformLayout();
        flowJourneyTotal.PerformLayout();
        mnuHistory.ResumeLayout(false);
        splitContainerLeft.Panel2.ResumeLayout(false);
        splitContainerLeft.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
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
    private LinkLabel lnkWarnings;
    private WarningTextBox txtPostcodes;
    private Label lblDateHeader;
    private TextBox txtDate;
    private GroupBox grpUnits;
    private RadioButton rdoMiles;
    private RadioButton rdoKm;
    private Button btnCalculate;
    private Button btnAddNew;
    private Label lblBreakdownHeader;
    private FlowLayoutPanel flowBreakdownOptions;
    private CheckBox chkCompact;
    private CheckBox chkLegDistances;
    private CheckBox chkShowEstimate;
    private WarningTextBox txtResults;
    private FlowLayoutPanel flowJourneyTotal;
    private Label lblJourneyTotal;
    private Button btnCopyJourneyTotal;
    private Button btnVerifyJourney;
    private ContextMenuStrip mnuHistory;
    private ToolStripMenuItem mnuVerify;
    private ToolStripMenuItem mnuOpenInMaps;
    private ToolStripMenuItem mnuUseEstimate;
    private Label lblStatus;
    private Label lblHistoryHeader;
    private Button btnToggleHistory;
    private Button btnImportHistory;
    private Button btnExportHistory;
    private Button btnClearHistory;
    private ListBox lstHistory;
    private Label lblHistoryTotal;
    private Button btnCopyHistoryTotal;
}
