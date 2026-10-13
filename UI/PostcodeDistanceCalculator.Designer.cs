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
        txtPostcodes = new TextBox();
        lblDateHeader = new Label();
        txtDate = new TextBox();
        grpUnits = new GroupBox();
        rdoKm = new RadioButton();
        rdoMiles = new RadioButton();
        btnCalculate = new Button();
        btnAddNew = new Button();
        lblBreakdownHeader = new Label();
        txtResults = new TextBox();
        lblJourneyTotal = new Label();
        btnCopyJourneyTotal = new Button();
        lblStatus = new Label();
        btnReopenHistory = new Button();
        lblHistoryHeader = new Label();
        lstHistory = new ListBox();
        lblHistoryTotal = new Label();
        btnCopyHistoryTotal = new Button();
        btnClearHistory = new Button();
        btnToggleHistory = new Button();
        toolTipInput = new ToolTip(components);
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
        splitContainer.Panel2.Controls.Add(btnClearHistory);
        splitContainer.Panel2.Controls.Add(btnToggleHistory);
        splitContainer.Panel2MinSize = 220;
        splitContainer.Size = new Size(950, 530);
        splitContainer.SplitterDistance = 620;
        splitContainer.SplitterWidth = 6;
        splitContainer.TabIndex = 0;
        //
        // splitContainerLeft
        //
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
        toolTipInput.SetToolTip(lblInputHeader, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;).");
        //
        // lblInputInfo
        //
        lblInputInfo.Cursor = Cursors.Hand;
        lblInputInfo.ForeColor = Color.FromArgb(30, 64, 175);
        lblInputInfo.ImageAlign = ContentAlignment.MiddleCenter;
        lblInputInfo.Location = new Point(130, 9);
        lblInputInfo.Name = "lblInputInfo";
        lblInputInfo.Size = new Size(20, 20);
        lblInputInfo.TabIndex = 1;
        toolTipInput.SetToolTip(lblInputInfo, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;).\nClick for full help.");
        lblInputInfo.Click += BtnHelp_Click;
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
        txtPostcodes.ScrollBars = ScrollBars.Vertical;
        txtPostcodes.Size = new Size(385, 105);
        txtPostcodes.TabIndex = 2;
        toolTipInput.SetToolTip(txtPostcodes, "Separate individual postcodes with commas or new lines (e.g. AB12 3CD, EF45 6GH).\nSeparate multiple distinct journeys with semicolons (;).\nPress Ctrl+Enter to calculate.");
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
        // lblJourneyTotal
        //
        lblJourneyTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblJourneyTotal.AutoSize = true;
        lblJourneyTotal.Cursor = Cursors.Hand;
        lblJourneyTotal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblJourneyTotal.ForeColor = Color.FromArgb(30, 41, 59);
        lblJourneyTotal.Location = new Point(15, 280);
        lblJourneyTotal.Name = "lblJourneyTotal";
        lblJourneyTotal.Size = new Size(160, 20);
        lblJourneyTotal.TabIndex = 2;
        lblJourneyTotal.Text = "Journey Total: 0.0 mi";
        lblJourneyTotal.SizeChanged += LblJourneyTotal_SizeChanged;
        lblJourneyTotal.Click += LblJourneyTotal_Click;
        //
        // btnCopyJourneyTotal
        //
        btnCopyJourneyTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        btnCopyJourneyTotal.BackColor = Color.FromArgb(226, 232, 240);
        btnCopyJourneyTotal.FlatAppearance.BorderSize = 0;
        btnCopyJourneyTotal.FlatStyle = FlatStyle.Flat;
        btnCopyJourneyTotal.Font = new Font("Segoe UI", 9F);
        btnCopyJourneyTotal.ImageAlign = ContentAlignment.MiddleLeft;
        btnCopyJourneyTotal.Location = new Point(185, 277);
        btnCopyJourneyTotal.Name = "btnCopyJourneyTotal";
        btnCopyJourneyTotal.Size = new Size(70, 26);
        btnCopyJourneyTotal.TabIndex = 3;
        btnCopyJourneyTotal.Text = "Copy";
        btnCopyJourneyTotal.TextImageRelation = TextImageRelation.ImageBeforeText;
        toolTipInput.SetToolTip(btnCopyJourneyTotal, "Copy Journey Total");
        btnCopyJourneyTotal.UseVisualStyleBackColor = false;
        btnCopyJourneyTotal.Click += BtnCopyJourneyTotal_Click;
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
        lblStatus.TabIndex = 4;
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
        lblHistoryHeader.Size = new Size(105, 17);
        lblHistoryHeader.TabIndex = 0;
        lblHistoryHeader.Text = "Saved Journeys:";
        //
        // lstHistory
        //
        lstHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lstHistory.BorderStyle = BorderStyle.FixedSingle;
        lstHistory.DrawMode = DrawMode.OwnerDrawFixed;
        lstHistory.Font = new Font("Segoe UI", 9F);
        lstHistory.IntegralHeight = false;
        lstHistory.ItemHeight = 26;
        lstHistory.Location = new Point(10, 35);
        lstHistory.Name = "lstHistory";
        lstHistory.Size = new Size(305, 431);
        lstHistory.TabIndex = 1;
        lstHistory.DrawItem += LstHistory_DrawItem;
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
        btnClearHistory.TabIndex = 4;
        btnClearHistory.Text = "Clear";
        toolTipInput.SetToolTip(btnClearHistory, "Delete all saved journeys");
        btnClearHistory.UseVisualStyleBackColor = false;
        btnClearHistory.Click += BtnClearHistory_Click;
        //
        // btnToggleHistory
        //
        btnToggleHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggleHistory.BackColor = Color.FromArgb(226, 232, 240);
        btnToggleHistory.FlatAppearance.BorderSize = 0;
        btnToggleHistory.FlatStyle = FlatStyle.Flat;
        btnToggleHistory.ForeColor = Color.FromArgb(51, 65, 85);
        btnToggleHistory.Location = new Point(280, 6);
        btnToggleHistory.Name = "btnToggleHistory";
        btnToggleHistory.Size = new Size(35, 24);
        btnToggleHistory.TabIndex = 5;
        toolTipInput.SetToolTip(btnToggleHistory, "Close Saved Journeys panel");
        btnToggleHistory.UseVisualStyleBackColor = false;
        btnToggleHistory.Click += BtnToggleHistory_Click;
        //
        // PostcodeDistanceCalculator
        //
        AcceptButton = btnCalculate;
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
    private Button btnClearHistory;
    private ListBox lstHistory;
    private Label lblHistoryTotal;
    private Button btnCopyHistoryTotal;
}
