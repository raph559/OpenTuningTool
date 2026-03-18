namespace OpenTuningTool.Forms;

partial class DetectResultsForm
{
    private System.ComponentModel.IContainer components = null;

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

        dataGridView = new DataGridView();
        lblCount = new Label();
        btnSelectAll = new Button();
        btnSelectNone = new Button();
        btnAcceptSelected = new Button();
        btnAcceptAll = new Button();
        btnCancel = new Button();
        panelButtons = new Panel();

        ((System.ComponentModel.ISupportInitialize)dataGridView).BeginInit();
        panelButtons.SuspendLayout();
        SuspendLayout();

        // --- dataGridView ---
        dataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dataGridView.Location = new Point(15, 40);
        dataGridView.Size = new Size(755, 350);
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridView.RowHeadersVisible = false;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.ReadOnly = false;
        
        // Theme the datagridview
        dataGridView.BackgroundColor = bgDark;
        dataGridView.BorderStyle = BorderStyle.None;
        dataGridView.GridColor = Color.FromArgb(60, 60, 60);
        dataGridView.EnableHeadersVisualStyles = false;
        dataGridView.ColumnHeadersDefaultCellStyle.BackColor = bgPanel;
        dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = fgLight;
        dataGridView.DefaultCellStyle.BackColor = bgControl;
        dataGridView.DefaultCellStyle.ForeColor = fgLight;
        dataGridView.DefaultCellStyle.SelectionBackColor = accent;

        var colCheck = new DataGridViewCheckBoxColumn
        {
            HeaderText = "✓",
            Width = 35,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = false,
        };
        var colAddr    = new DataGridViewTextBoxColumn { HeaderText = "Address",    ReadOnly = true };
        var colSize    = new DataGridViewTextBoxColumn { HeaderText = "Size (B)",   ReadOnly = true };
        var colRows    = new DataGridViewTextBoxColumn { HeaderText = "Rows",       ReadOnly = true };
        var colCols    = new DataGridViewTextBoxColumn { HeaderText = "Cols",       ReadOnly = true };
        var colBits    = new DataGridViewTextBoxColumn { HeaderText = "Bits",       ReadOnly = true };
        var colEndian  = new DataGridViewTextBoxColumn { HeaderText = "Endian",     ReadOnly = true };
        var colConf    = new DataGridViewTextBoxColumn { HeaderText = "Confidence", ReadOnly = true };

        dataGridView.Columns.AddRange(colCheck, colAddr, colSize, colRows, colCols, colBits, colEndian, colConf);

        // --- lblCount ---
        lblCount.AutoSize = true;
        lblCount.Location = new Point(15, 12);
        lblCount.Text = "0 candidate(s) detected.";
        lblCount.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblCount.ForeColor = accent;

        // --- panelButtons ---
        panelButtons.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelButtons.Location = new Point(0, 406);
        panelButtons.Size = new Size(784, 55);
        panelButtons.BackColor = bgPanel;
        panelButtons.Controls.AddRange(new Control[] {
            btnSelectAll, btnSelectNone, btnAcceptSelected, btnAcceptAll, btnCancel
        });

        // Helper action for themes mapping
        void ThemeButton(Button btn, bool primary = false) 
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = fgLight;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
            if (primary) {
                btn.BackColor = accent;
                btn.ForeColor = Color.White;
            } else {
                btn.BackColor = bgControl;
            }
        }

        // Select All
        btnSelectAll.Text = "Select All";
        btnSelectAll.Size = new Size(90, 30);
        btnSelectAll.Location = new Point(15, 12);
        btnSelectAll.Click += BtnSelectAll_Click;
        ThemeButton(btnSelectAll);

        // Select None
        btnSelectNone.Text = "Select None";
        btnSelectNone.Size = new Size(95, 30);
        btnSelectNone.Location = new Point(115, 12);
        btnSelectNone.Click += BtnSelectNone_Click;
        ThemeButton(btnSelectNone);

        // Accept Selected
        btnAcceptSelected.Text = "Accept Selected";
        btnAcceptSelected.Size = new Size(120, 30);
        btnAcceptSelected.Location = new Point(445, 12);
        btnAcceptSelected.Click += BtnAcceptSelected_Click;
        ThemeButton(btnAcceptSelected, true);

        // Accept All
        btnAcceptAll.Text = "Accept All";
        btnAcceptAll.Size = new Size(95, 30);
        btnAcceptAll.Location = new Point(575, 12);
        btnAcceptAll.Click += BtnAcceptAll_Click;
        ThemeButton(btnAcceptAll);

        // Cancel
        btnCancel.Text = "Cancel";
        btnCancel.Size = new Size(85, 30);
        btnCancel.Location = new Point(682, 12);
        btnCancel.Click += BtnCancel_Click;
        ThemeButton(btnCancel);

        // --- Form ---
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 461);
        MinimumSize = new Size(650, 420);
        Text = "CalibrAI — Map Discovery";
        StartPosition = FormStartPosition.CenterParent;
        BackColor = bgDark;
        ForeColor = fgLight;
        Controls.AddRange(new Control[] { lblCount, dataGridView, panelButtons });

        ((System.ComponentModel.ISupportInitialize)dataGridView).EndInit();
        panelButtons.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private DataGridView dataGridView = null!;
    private Label lblCount = null!;
    private Button btnSelectAll = null!;
    private Button btnSelectNone = null!;
    private Button btnAcceptSelected = null!;
    private Button btnAcceptAll = null!;
    private Button btnCancel = null!;
    private Panel panelButtons = null!;
}
