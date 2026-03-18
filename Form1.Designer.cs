#nullable enable
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
        splitContainer       = new SplitContainer();
        treeView             = new TreeView();
        panelDetail          = new Panel();
        lblDetailTitle       = new Label();
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
        dgvMap               = new DataGridView();
        panelConstantValue   = new Panel();
        lblValueLabel        = new Label();
        txtConstValue        = new TextBox();
        lblConstEndian       = new Label();
        btnApplyValue        = new Button();
        statusStrip          = new StatusStrip();
        statusLabel          = new ToolStripStatusLabel();

        menuStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        panelDetail.SuspendLayout();
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

        menuItemOpenXdf.Text         = "&Open XDF…";
        menuItemOpenXdf.ShortcutKeys = Keys.Control | Keys.O;
        menuItemOpenXdf.Click       += MenuOpenXdf_Click;

        menuItemOpenBin.Text         = "Open &BIN…";
        menuItemOpenBin.ShortcutKeys = Keys.Control | Keys.B;
        menuItemOpenBin.Enabled      = false;
        menuItemOpenBin.Click       += MenuOpenBin_Click;

        menuItemSaveBin.Text         = "&Save BIN";
        menuItemSaveBin.ShortcutKeys = Keys.Control | Keys.S;
        menuItemSaveBin.Enabled      = false;
        menuItemSaveBin.Click       += MenuSaveBin_Click;

        menuItemSaveBinAs.Text       = "Save BIN &As…";
        menuItemSaveBinAs.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
        menuItemSaveBinAs.Enabled    = false;
        menuItemSaveBinAs.Click     += MenuSaveBinAs_Click;

        menuItemSettings.Text         = "&Settings…";
        menuItemSettings.Click       += MenuSettings_Click;

        menuItemExit.Text            = "E&xit";
        menuItemExit.Click          += MenuExit_Click;

        menuItemCalibrAI.Text = "&CalibrAI";
        menuItemCalibrAI.DropDownItems.Add(menuItemDetect);
        menuItemDetect.Text    = "&Detect Maps in BIN…";
        menuItemDetect.Enabled = false;
        menuItemDetect.Click  += MenuDetectMaps_Click;

        foreach (ToolStripItem item in menuItemFile.DropDownItems) { item.BackColor = bgDark; item.ForeColor = fgLight; }
        foreach (ToolStripItem item in menuItemCalibrAI.DropDownItems) { item.BackColor = bgDark; item.ForeColor = fgLight; }

        // ===== SplitContainer =====
        splitContainer.Dock = DockStyle.Fill;
        splitContainer.Size = new Size(1000, 526);
        splitContainer.Panel1MinSize = 160;
        splitContainer.Panel2MinSize = 400;
        splitContainer.SplitterDistance = 260;
        splitContainer.BackColor = bgPanel;

        treeView.Dock = DockStyle.Fill;
        treeView.HideSelection = false;
        treeView.BackColor = bgPanel;
        treeView.ForeColor = fgLight;
        treeView.BorderStyle = BorderStyle.None;
        treeView.LineColor = fgLight;
        treeView.AfterSelect += TreeView_AfterSelect;
        splitContainer.Panel1.Controls.Add(treeView);

        // Right: panelDetail
        panelDetail.Dock = DockStyle.Fill;
        panelDetail.Visible = false;
        panelDetail.BackColor = bgDark;
        splitContainer.Panel2.Controls.Add(panelDetail);

        // ===== panelDetail =====
        // Title strip at top
        lblDetailTitle.AutoSize = false;
        lblDetailTitle.Dock = DockStyle.Top;
        lblDetailTitle.Height = 36;
        lblDetailTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblDetailTitle.Padding = new Padding(10, 8, 0, 0);
        lblDetailTitle.BackColor = bgPanel;
        lblDetailTitle.ForeColor = accent;

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

        // TabControl for Map View (Text, 2D, 3D)
        tabControlView = new TabControl();
        tabText = new TabPage("Text");
        tab2D = new TabPage("2D");
        tab3D = new TabPage("3D");

        tabControlView.Dock = DockStyle.Fill;
        tabControlView.Visible = false; // Hide if no valid data
        tabControlView.Controls.AddRange(new Control[] { tabText, tab2D, tab3D });

        // Map DataGridView
        dgvMap.Dock = DockStyle.Fill;
        dgvMap.AllowUserToAddRows = false;
        dgvMap.AllowUserToDeleteRows = false;
        dgvMap.RowHeadersWidth = 60;
        dgvMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
        dgvMap.BackgroundColor = bgDark;
        dgvMap.BorderStyle = BorderStyle.None;
        dgvMap.GridColor = Color.FromArgb(60, 60, 60);
        dgvMap.EnableHeadersVisualStyles = false;
        dgvMap.ColumnHeadersDefaultCellStyle.BackColor = bgPanel;
        dgvMap.ColumnHeadersDefaultCellStyle.ForeColor = fgLight;
        dgvMap.RowHeadersDefaultCellStyle.BackColor = bgPanel;
        dgvMap.RowHeadersDefaultCellStyle.ForeColor = fgLight;
        dgvMap.DefaultCellStyle.BackColor = bgControl;
        dgvMap.DefaultCellStyle.ForeColor = fgLight;
        dgvMap.DefaultCellStyle.SelectionBackColor = accent;
        dgvMap.CellEndEdit += DgvMap_CellEndEdit;

        tabText.Controls.Add(dgvMap);
        tabText.BackColor = bgDark;

        // Placeholders for 2D / 3D
        var lbl2D = new Label { Text = "2D View (Rendering not implemented yet)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = fgLight };
        tab2D.Controls.Add(lbl2D);
        tab2D.BackColor = bgDark;

        var lbl3D = new Label { Text = "3D View (Rendering not implemented yet)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = fgLight };
        tab3D.Controls.Add(lbl3D);
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
        panelDetail.Controls.Add(lblDetailTitle);

        // ===== StatusStrip =====
        statusStrip.BackColor = bgDark;
        statusStrip.ForeColor = fgLight;
        statusStrip.Items.Add(statusLabel);
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.Text = "Ready. Use File > Open XDF… to load a definition file.";

        // ===== Form1 =====
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1050, 650);
        MinimumSize = new Size(800, 550);
        Text = "OpenTuningTool - Dark Mode";
        BackColor = bgDark;
        ForeColor = fgLight;
        MainMenuStrip = menuStrip;
        Controls.AddRange(new Control[] { splitContainer, statusStrip, menuStrip });

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        panelDetail.ResumeLayout(false);
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
    private SplitContainer splitContainer = null!;
    private TreeView treeView = null!;
    private Panel panelDetail = null!;
    private Label lblDetailTitle = null!;
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
    private TabControl tabControlView = null!;
    private TabPage tabText = null!;
    private TabPage tab2D = null!;
    private TabPage tab3D = null!;
    private DataGridView dgvMap = null!;
    private Panel panelConstantValue = null!;
    private Label lblValueLabel = null!;
    private TextBox txtConstValue = null!;
    private Label lblConstEndian = null!;
    private Button btnApplyValue = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
}
