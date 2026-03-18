#nullable enable
using OpenTuningTool.Controls;

namespace OpenTuningTool;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        // Colors for modern dark theme
        var bgDark = Color.FromArgb(30, 30, 30);
        var bgPanel = Color.FromArgb(37, 37, 38);
        var bgControl = Color.FromArgb(45, 45, 48);
        var fgLight = Color.FromArgb(220, 220, 220);
        var accent = Color.FromArgb(0, 122, 204);

        // ---- controls ----
        menuStrip            = new MenuStrip();
        menuItemFile         = new ToolStripMenuItem();
        menuItemOpenXdf      = new ToolStripMenuItem();
        menuItemOpenBin      = new ToolStripMenuItem();
        menuItemSep1         = new ToolStripSeparator();
        menuItemSaveBin      = new ToolStripMenuItem();
        menuItemSaveBinAs    = new ToolStripMenuItem();
        menuItemSettings     = new ToolStripMenuItem();
        menuItemSep2         = new ToolStripSeparator();
        menuItemExit         = new ToolStripMenuItem();
        menuItemCalibrAI     = new ToolStripMenuItem();
        menuItemDetect       = new ToolStripMenuItem();
        toolbarPanel         = new Panel();
        btnToolOpenXdf       = new Button();
        btnToolOpenBin       = new Button();
        btnToolSave          = new Button();
        splitContainer       = new SplitContainer();
        searchBox            = new ModernSearchBox();
        treeView             = new TreeView();
        panelDetachedPlaceholder = new Panel();
        lblDetachedPlaceholder = new Label();
        btnReattachDetail   = new Button();
        panelDetail          = new Panel();
        panelDetailHeader   = new Panel();
        lblDetailTitle       = new Label();
        btnDetachDetail     = new Button();
        panelMeta            = new Panel();
        tableLayout          = new TableLayoutPanel();
        lblIdLabel           = new Label();
        lblIdValue           = new Label();
        lblDescLabel         = new Label();
        lblDescValue         = new Label();
        lblXAxisLabel        = new Label();
        lblXAxisValue        = new Label();
        lblYAxisLabel        = new Label();
        lblYAxisValue        = new Label();
        lblDataLabel         = new Label();
        lblDataValue         = new Label();
        lblAddressLabel      = new Label();
        lblAddressValue      = new Label();
        panelValues          = new Panel();
        lblNoBin             = new Label();
        dgvMap               = new StyledDataGridView();
        tabControlView       = new FlatTabControl();
        tabText              = new TabPage("Text");
        tab2D                = new TabPage("2D");
        tab3D                = new TabPage("3D");
        heatmapView          = new HeatmapView();
        surfacePlotView      = new SurfacePlotView();
        btnResetView3D       = new Button();
        panelConstantValue   = new Panel();
        lblValueLabel        = new Label();
        txtConstValue        = new TextBox();
        lblConstEndian       = new Label();
        btnApplyValue        = new Button();
        statusStrip          = new StatusStrip();
        statusLabel          = new ToolStripStatusLabel();
        statusDimensions     = new ToolStripStatusLabel();
        statusBinInfo        = new ToolStripStatusLabel();

        menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        panelDetachedPlaceholder.SuspendLayout();
        panelDetail.SuspendLayout();
        panelDetailHeader.SuspendLayout();
        panelMeta.SuspendLayout();
        tableLayout.SuspendLayout();
        panelValues.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvMap).BeginInit();
        panelConstantValue.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // ===== MenuStrip =====
        menuStrip.BackColor = bgDark;
        menuStrip.ForeColor = fgLight;
        menuStrip.Items.AddRange(new ToolStripItem[] { menuItemFile, menuItemCalibrAI });
        menuStrip.Location = new Point(0, 0);
        menuStrip.Size = new Size(1000, 24);

        menuItemFile.Text = "&File";
        menuItemFile.DropDownItems.AddRange(new ToolStripItem[]
        {
            menuItemOpenXdf, menuItemOpenBin,
            menuItemSep1,
            menuItemSaveBin, menuItemSaveBinAs, menuItemSettings,
            menuItemSep2,
            menuItemExit
        });

        menuItemOpenXdf.Text         = "&Open XDF\u2026";
        menuItemOpenXdf.ShortcutKeys = Keys.Control | Keys.O;
        menuItemOpenXdf.Click       += MenuOpenXdf_Click;

        menuItemOpenBin.Text         = "Open &BIN\u2026";
        menuItemOpenBin.ShortcutKeys = Keys.Control | Keys.B;
        menuItemOpenBin.Enabled      = false;
        menuItemOpenBin.Click       += MenuOpenBin_Click;

        menuItemSaveBin.Text         = "&Save BIN";
        menuItemSaveBin.ShortcutKeys = Keys.Control | Keys.S;
        menuItemSaveBin.Enabled      = false;
        menuItemSaveBin.Click       += MenuSaveBin_Click;

        menuItemSaveBinAs.Text       = "Save BIN &As\u2026";
        menuItemSaveBinAs.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
        menuItemSaveBinAs.Enabled    = false;
        menuItemSaveBinAs.Click     += MenuSaveBinAs_Click;

        menuItemSettings.Text         = "&Settings\u2026";
        menuItemSettings.Click       += MenuSettings_Click;

        menuItemExit.Text            = "E&xit";
        menuItemExit.Click          += MenuExit_Click;

        menuItemCalibrAI.Text = "&CalibrAI";
        menuItemCalibrAI.DropDownItems.Add(menuItemDetect);
        menuItemDetect.Text    = "&Detect Maps in BIN\u2026";
        menuItemDetect.Enabled = false;
        menuItemDetect.Click  += MenuDetectMaps_Click;

        foreach (ToolStripItem item in menuItemFile.DropDownItems) { item.BackColor = bgDark; item.ForeColor = fgLight; }
        foreach (ToolStripItem item in menuItemCalibrAI.DropDownItems) { item.BackColor = bgDark; item.ForeColor = fgLight; }

        // ===== Toolbar Panel =====
        toolbarPanel.Dock = DockStyle.Top;
        toolbarPanel.Height = 36;
        toolbarPanel.BackColor = bgPanel;
        toolbarPanel.Padding = new Padding(6, 4, 6, 4);

        btnToolOpenXdf.Text = "\U0001F4C2 XDF";
        btnToolOpenXdf.FlatStyle = FlatStyle.Flat;
        btnToolOpenXdf.FlatAppearance.BorderSize = 0;
        btnToolOpenXdf.BackColor = bgControl;
        btnToolOpenXdf.ForeColor = fgLight;
        btnToolOpenXdf.Size = new Size(64, 28);
        btnToolOpenXdf.Location = new Point(6, 4);
        btnToolOpenXdf.Cursor = Cursors.Hand;
        btnToolOpenXdf.Font = new Font("Segoe UI", 8.5f);
        btnToolOpenXdf.Click += MenuOpenXdf_Click;

        btnToolOpenBin.Text = "\U0001F4C2 BIN";
        btnToolOpenBin.FlatStyle = FlatStyle.Flat;
        btnToolOpenBin.FlatAppearance.BorderSize = 0;
        btnToolOpenBin.BackColor = bgControl;
        btnToolOpenBin.ForeColor = fgLight;
        btnToolOpenBin.Size = new Size(64, 28);
        btnToolOpenBin.Location = new Point(74, 4);
        btnToolOpenBin.Cursor = Cursors.Hand;
        btnToolOpenBin.Font = new Font("Segoe UI", 8.5f);
        btnToolOpenBin.Enabled = false;
        btnToolOpenBin.Click += MenuOpenBin_Click;

        btnToolSave.Text = "\U0001F4BE Save";
        btnToolSave.FlatStyle = FlatStyle.Flat;
        btnToolSave.FlatAppearance.BorderSize = 0;
        btnToolSave.BackColor = bgControl;
        btnToolSave.ForeColor = fgLight;
        btnToolSave.Size = new Size(68, 28);
        btnToolSave.Location = new Point(142, 4);
        btnToolSave.Cursor = Cursors.Hand;
        btnToolSave.Font = new Font("Segoe UI", 8.5f);
        btnToolSave.Enabled = false;
        btnToolSave.Click += MenuSaveBin_Click;

        toolbarPanel.Controls.AddRange(new Control[] { btnToolOpenXdf, btnToolOpenBin, btnToolSave });

        // ===== SplitContainer =====
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.Size = new Size(1000, 526);
        splitContainer.Panel1MinSize = 160;
        splitContainer.Panel2MinSize = 400;
        splitContainer.SplitterDistance = 260;
        splitContainer.SplitterWidth = 2;
        splitContainer.BackColor = bgPanel;

        // Search box at top of left panel
        searchBox.Dock = DockStyle.Top;
        searchBox.Height = 28;
        searchBox.Placeholder = "Search tables...";
        searchBox.Margin = new Padding(6, 6, 6, 4);
        searchBox.ApplyThemeColors(bgControl, fgLight, Color.FromArgb(70, 70, 74), Color.FromArgb(150, 150, 150));
        searchBox.SearchTextChanged += SearchBox_TextChanged;

        // Padding panel for search box
        var searchPadding = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = bgPanel,
            Padding = new Padding(6, 5, 6, 3)
        };
        searchBox.Dock = DockStyle.Fill;
        searchPadding.Controls.Add(searchBox);

        treeView.Dock = DockStyle.Fill;
        treeView.HideSelection = false;
        treeView.BackColor = bgPanel;
        treeView.ForeColor = fgLight;
        treeView.BorderStyle = BorderStyle.None;
        treeView.LineColor = fgLight;
        treeView.Font = new Font("Segoe UI", 9.5f);
        treeView.ItemHeight = 24;
        treeView.AfterSelect += TreeView_AfterSelect;

        splitContainer.Panel1.Controls.Add(treeView);
        splitContainer.Panel1.Controls.Add(searchPadding);

        // Right: detached placeholder
        panelDetachedPlaceholder.Dock = DockStyle.Fill;
        panelDetachedPlaceholder.Visible = false;
        panelDetachedPlaceholder.BackColor = bgDark;
        panelDetachedPlaceholder.Padding = new Padding(24);

        lblDetachedPlaceholder.Dock = DockStyle.Fill;
        lblDetachedPlaceholder.TextAlign = ContentAlignment.MiddleCenter;
        lblDetachedPlaceholder.ForeColor = fgLight;
        lblDetachedPlaceholder.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
        lblDetachedPlaceholder.Padding = new Padding(0, 0, 0, 48);
        lblDetachedPlaceholder.Text = "The data view is detached into its own window.";

        btnReattachDetail.Dock = DockStyle.Bottom;
        btnReattachDetail.Height = 32;
        btnReattachDetail.Text = "Attach Back";
        btnReattachDetail.FlatStyle = FlatStyle.Flat;
        btnReattachDetail.FlatAppearance.BorderSize = 1;
        btnReattachDetail.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 74);
        btnReattachDetail.BackColor = bgControl;
        btnReattachDetail.ForeColor = fgLight;
        btnReattachDetail.Cursor = Cursors.Hand;
        btnReattachDetail.Click += BtnReattachDetail_Click;

        panelDetachedPlaceholder.Controls.Add(lblDetachedPlaceholder);
        panelDetachedPlaceholder.Controls.Add(btnReattachDetail);

        // Right: panelDetail
        panelDetail.Dock = DockStyle.Fill;
        panelDetail.Visible = false;
        panelDetail.BackColor = bgDark;
        splitContainer.Panel2.Controls.Add(panelDetachedPlaceholder);
        splitContainer.Panel2.Controls.Add(panelDetail);

        // ===== panelDetail =====
        // Title strip at top
        panelDetailHeader.Dock = DockStyle.Top;
        panelDetailHeader.Height = 40;
        panelDetailHeader.BackColor = bgPanel;
        panelDetailHeader.Padding = new Padding(0);

        lblDetailTitle.AutoSize = false;
        lblDetailTitle.Dock = DockStyle.Fill;
        lblDetailTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblDetailTitle.Padding = new Padding(12, 10, 0, 0);
        lblDetailTitle.BackColor = bgPanel;
        lblDetailTitle.ForeColor = accent;

        btnDetachDetail.Dock = DockStyle.Right;
        btnDetachDetail.Width = 92;
        btnDetachDetail.Text = "Detach";
        btnDetachDetail.FlatStyle = FlatStyle.Flat;
        btnDetachDetail.FlatAppearance.BorderSize = 0;
        btnDetachDetail.BackColor = bgPanel;
        btnDetachDetail.ForeColor = fgLight;
        btnDetachDetail.Cursor = Cursors.Hand;
        btnDetachDetail.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnDetachDetail.Click += BtnDetachDetail_Click;

        panelDetailHeader.Controls.Add(lblDetailTitle);
        panelDetailHeader.Controls.Add(btnDetachDetail);

        // Meta panel (fixed height) below title
        panelMeta.Dock = DockStyle.Top;
        panelMeta.Height = 150;
        panelMeta.BackColor = bgPanel;
        panelMeta.Controls.Add(tableLayout);

        tableLayout.Dock = DockStyle.Fill;
        tableLayout.ColumnCount = 2;
        tableLayout.RowCount = 6;
        tableLayout.Padding = new Padding(15, 8, 8, 8);
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 6; i++)
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));

        SetupMetaLabel(lblIdLabel,      "ID:",           0, 0);
        SetupMetaValue(lblIdValue,                       1, 0, new Font("Consolas", 9F));
        SetupMetaLabel(lblDescLabel,    "Description:",  0, 1);
        SetupMetaValue(lblDescValue,                     1, 1);
        SetupMetaLabel(lblXAxisLabel,   "X Axis:",       0, 2);
        SetupMetaValue(lblXAxisValue,                    1, 2, new Font("Consolas", 9F));
        SetupMetaLabel(lblYAxisLabel,   "Y Axis:",       0, 3);
        SetupMetaValue(lblYAxisValue,                    1, 3, new Font("Consolas", 9F));
        SetupMetaLabel(lblDataLabel,    "Data:",         0, 4);
        SetupMetaValue(lblDataValue,                     1, 4, new Font("Consolas", 9F));
        SetupMetaLabel(lblAddressLabel, "Address:",      0, 5);
        SetupMetaValue(lblAddressValue,                  1, 5, new Font("Consolas", 9F));

        tableLayout.Controls.Add(lblIdLabel,      0, 0); tableLayout.Controls.Add(lblIdValue,      1, 0);
        tableLayout.Controls.Add(lblDescLabel,    0, 1); tableLayout.Controls.Add(lblDescValue,    1, 1);
        tableLayout.Controls.Add(lblXAxisLabel,   0, 2); tableLayout.Controls.Add(lblXAxisValue,   1, 2);
        tableLayout.Controls.Add(lblYAxisLabel,   0, 3); tableLayout.Controls.Add(lblYAxisValue,   1, 3);
        tableLayout.Controls.Add(lblDataLabel,    0, 4); tableLayout.Controls.Add(lblDataValue,    1, 4);
        tableLayout.Controls.Add(lblAddressLabel, 0, 5); tableLayout.Controls.Add(lblAddressValue, 1, 5);

        // Values panel fills the rest
        panelValues.Dock = DockStyle.Fill;
        panelValues.BackColor = bgDark;
        panelValues.Padding = new Padding(10);

        // "No BIN" message
        lblNoBin.Dock = DockStyle.Fill;
        lblNoBin.TextAlign = ContentAlignment.MiddleCenter;
        lblNoBin.ForeColor = fgLight;
        lblNoBin.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
        lblNoBin.Text = "Open a BIN file to view and edit values.";

        // ===== FlatTabControl for Map View (Text, 2D, 3D) =====
        tabControlView.Dock = DockStyle.Fill;
        tabControlView.Visible = false;
        tabControlView.TabPages.AddRange(new TabPage[] { tabText, tab2D, tab3D });

        // Map DataGridView (styled)
        dgvMap.Dock = DockStyle.Fill;
        dgvMap.AllowUserToAddRows = false;
        dgvMap.AllowUserToDeleteRows = false;
        dgvMap.RowHeadersWidth = 60;
        dgvMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        dgvMap.ApplyThemeColors(bgDark, bgPanel, bgControl, fgLight, accent, Color.FromArgb(60, 60, 60));
        dgvMap.CellEndEdit += DgvMap_CellEndEdit;

        tabText.Controls.Add(dgvMap);
        tabText.BackColor = bgDark;

        // 2D Heatmap View
        heatmapView.Dock = DockStyle.Fill;
        heatmapView.ApplyThemeColors(bgDark, fgLight, Color.FromArgb(60, 60, 60), accent);
        tab2D.Controls.Add(heatmapView);
        tab2D.BackColor = bgDark;

        // 3D Surface View with Reset button
        surfacePlotView.Dock = DockStyle.Fill;
        surfacePlotView.ApplyThemeColors(bgDark, fgLight, Color.FromArgb(60, 60, 60), accent);

        btnResetView3D.Text = "Reset View";
        btnResetView3D.Size = new Size(80, 26);
        btnResetView3D.FlatStyle = FlatStyle.Flat;
        btnResetView3D.FlatAppearance.BorderSize = 1;
        btnResetView3D.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 74);
        btnResetView3D.BackColor = Color.FromArgb(50, bgPanel.R, bgPanel.G, bgPanel.B);
        btnResetView3D.ForeColor = fgLight;
        btnResetView3D.Font = new Font("Segoe UI", 7.5f);
        btnResetView3D.Cursor = Cursors.Hand;
        btnResetView3D.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnResetView3D.Click += BtnResetView3D_Click;

        tab3D.Controls.Add(btnResetView3D);
        tab3D.Controls.Add(surfacePlotView);
        btnResetView3D.BringToFront();
        btnResetView3D.Location = new Point(tab3D.Width - 90, 6);
        tab3D.BackColor = bgDark;

        // Constant value panel
        panelConstantValue.Dock = DockStyle.Top;
        panelConstantValue.Height = 44;
        panelConstantValue.Visible = false;
        panelConstantValue.BackColor = bgPanel;
        panelConstantValue.Padding = new Padding(10, 8, 10, 8);

        lblValueLabel.AutoSize = true;
        lblValueLabel.Text = "Value:";
        lblValueLabel.Location = new Point(10, 13);
        lblValueLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        txtConstValue.Location = new Point(60, 10);
        txtConstValue.Size = new Size(130, 23);
        txtConstValue.Font = new Font("Consolas", 10F);
        txtConstValue.BackColor = bgControl;
        txtConstValue.ForeColor = fgLight;
        txtConstValue.BorderStyle = BorderStyle.FixedSingle;

        btnApplyValue.Location = new Point(200, 9);
        btnApplyValue.Size = new Size(70, 26);
        btnApplyValue.Text = "Apply";
        btnApplyValue.FlatStyle = FlatStyle.Flat;
        btnApplyValue.FlatAppearance.BorderSize = 0;
        btnApplyValue.BackColor = accent;
        btnApplyValue.ForeColor = Color.White;
        btnApplyValue.Cursor = Cursors.Hand;
        btnApplyValue.Click += BtnApplyValue_Click;

        lblConstEndian.AutoSize = true;
        lblConstEndian.Location = new Point(280, 13);
        lblConstEndian.ForeColor = SystemColors.GrayText;
        lblConstEndian.Font = new Font("Segoe UI", 8F, FontStyle.Italic);

        panelConstantValue.Controls.AddRange(new Control[]
            { lblValueLabel, txtConstValue, btnApplyValue, lblConstEndian });

        panelValues.Controls.AddRange(new Control[] { tabControlView, panelConstantValue, lblNoBin });

        // Wire panelDetail together (Controls added bottom-to-top for DockStyle.Top)
        panelDetail.Controls.Add(panelValues);
        panelDetail.Controls.Add(panelMeta);
        panelDetail.Controls.Add(panelDetailHeader);

        // ===== StatusStrip =====
        statusStrip.BackColor = bgPanel;
        statusStrip.ForeColor = fgLight;
        statusStrip.SizingGrip = false;
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, statusDimensions, statusBinInfo });

        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Text = "Ready. Use File > Open XDF\u2026 to load a definition file.";
        statusLabel.Font = new Font("Segoe UI", 8.5f);

        statusDimensions.AutoSize = false;
        statusDimensions.Width = 80;
        statusDimensions.TextAlign = ContentAlignment.MiddleCenter;
        statusDimensions.BorderSides = ToolStripStatusLabelBorderSides.Left;
        statusDimensions.BorderStyle = Border3DStyle.Etched;
        statusDimensions.Font = new Font("Consolas", 8.5f);

        statusBinInfo.AutoSize = false;
        statusBinInfo.Width = 120;
        statusBinInfo.TextAlign = ContentAlignment.MiddleCenter;
        statusBinInfo.BorderSides = ToolStripStatusLabelBorderSides.Left;
        statusBinInfo.BorderStyle = Border3DStyle.Etched;
        statusBinInfo.Font = new Font("Segoe UI", 8.5f);

        // ===== Form1 =====
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(800, 550);
        Text = "OpenTuningTool";
        BackColor = bgDark;
        ForeColor = fgLight;
        MainMenuStrip = menuStrip;
        Controls.AddRange(new Control[] { splitContainer, toolbarPanel, statusStrip, menuStrip });

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        panelDetachedPlaceholder.ResumeLayout(false);
        panelDetail.ResumeLayout(false);
        panelDetailHeader.ResumeLayout(false);
        panelMeta.ResumeLayout(false);
        tableLayout.ResumeLayout(false);
        panelValues.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvMap).EndInit();
        panelConstantValue.ResumeLayout(false);
        panelConstantValue.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    // ---- helper to reduce repetition in InitializeComponent ----
    private static void SetupMetaLabel(Label lbl, string text, int col, int row)
    {
        lbl.Text = text;
        lbl.TextAlign = ContentAlignment.MiddleRight;
        lbl.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lbl.Dock = DockStyle.Fill;
    }

    private static void SetupMetaValue(Label lbl, int col, int row, Font? font = null)
    {
        lbl.AutoSize = false;
        lbl.Dock = DockStyle.Fill;
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        if (font != null) lbl.Font = font;
    }

    // ---- field declarations ----
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem menuItemFile = null!;
    private ToolStripMenuItem menuItemOpenXdf = null!;
    private ToolStripMenuItem menuItemOpenBin = null!;
    private ToolStripSeparator menuItemSep1 = null!;
    private ToolStripMenuItem menuItemSaveBin = null!;
    private ToolStripMenuItem menuItemSaveBinAs = null!;
    private ToolStripMenuItem menuItemSettings = null!;
    private ToolStripSeparator menuItemSep2 = null!;
    private ToolStripMenuItem menuItemExit = null!;
    private ToolStripMenuItem menuItemCalibrAI = null!;
    private ToolStripMenuItem menuItemDetect = null!;
    private Panel toolbarPanel = null!;
    private Button btnToolOpenXdf = null!;
    private Button btnToolOpenBin = null!;
    private Button btnToolSave = null!;
    private SplitContainer splitContainer = null!;
    private ModernSearchBox searchBox = null!;
    private TreeView treeView = null!;
    private Panel panelDetachedPlaceholder = null!;
    private Label lblDetachedPlaceholder = null!;
    private Button btnReattachDetail = null!;
    private Panel panelDetail = null!;
    private Panel panelDetailHeader = null!;
    private Label lblDetailTitle = null!;
    private Button btnDetachDetail = null!;
    private Panel panelMeta = null!;
    private TableLayoutPanel tableLayout = null!;
    private Label lblIdLabel = null!;
    private Label lblIdValue = null!;
    private Label lblDescLabel = null!;
    private Label lblDescValue = null!;
    private Label lblXAxisLabel = null!;
    private Label lblXAxisValue = null!;
    private Label lblYAxisLabel = null!;
    private Label lblYAxisValue = null!;
    private Label lblDataLabel = null!;
    private Label lblDataValue = null!;
    private Label lblAddressLabel = null!;
    private Label lblAddressValue = null!;
    private Panel panelValues = null!;
    private Label lblNoBin = null!;
    private FlatTabControl tabControlView = null!;
    private TabPage tabText = null!;
    private TabPage tab2D = null!;
    private TabPage tab3D = null!;
    private StyledDataGridView dgvMap = null!;
    private HeatmapView heatmapView = null!;
    private SurfacePlotView surfacePlotView = null!;
    private Button btnResetView3D = null!;
    private Panel panelConstantValue = null!;
    private Label lblValueLabel = null!;
    private TextBox txtConstValue = null!;
    private Label lblConstEndian = null!;
    private Button btnApplyValue = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private ToolStripStatusLabel statusDimensions = null!;
    private ToolStripStatusLabel statusBinInfo = null!;
}
