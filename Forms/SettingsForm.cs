using OpenTuningTool.Models;

namespace OpenTuningTool.Forms;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _numMinConfidence;
    private readonly TextBox _txtCalibrAiUrl;
    private readonly ComboBox _cmbTheme;
    private readonly ComboBox _cmbDefaultView;
    private readonly ComboBox _cmbUiDensity;
    private readonly CheckBox _chkAutoExpandTree;
    private readonly CheckBox _chkPromptUnsaved;
    private readonly CheckBox _chkAutoLoadLastFiles;
    private readonly Button _btnSave;
    private readonly Button _btnCancel;

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(AppSettings currentSettings)
    {
        ResultSettings = currentSettings.Clone();

        _numMinConfidence = new NumericUpDown();
        _txtCalibrAiUrl = new TextBox();
        _cmbTheme = new ComboBox();
        _cmbDefaultView = new ComboBox();
        _cmbUiDensity = new ComboBox();
        _chkAutoExpandTree = new CheckBox();
        _chkPromptUnsaved = new CheckBox();
        _chkAutoLoadLastFiles = new CheckBox();
        _btnSave = new Button();
        _btnCancel = new Button();

        InitializeComponent();
        LoadSettingsIntoControls(ResultSettings);
        _cmbTheme.SelectedIndexChanged += CmbTheme_SelectedIndexChanged;

        ApplyThemePreview(ResultSettings.Theme);
        ThemeUtility.ApplyUiDensity(this, currentSettings.UiDensity);
    }

    private void InitializeComponent()
    {
        var bgDark = Color.FromArgb(30, 30, 30);
        var bgPanel = Color.FromArgb(37, 37, 38);
        var bgControl = Color.FromArgb(45, 45, 48);
        var fgLight = Color.FromArgb(220, 220, 220);
        var accent = Color.FromArgb(0, 122, 204);

        var lblHeader = new Label
        {
            AutoSize = true,
            Text = "Settings",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = accent,
            Location = new Point(16, 12),
        };

        var lblHint = new Label
        {
            AutoSize = true,
            Text = "Tune runtime defaults, startup behavior, service endpoints, and theme.",
            ForeColor = Color.Silver,
            Location = new Point(16, 42),
        };

        var panel = new Panel
        {
            Location = new Point(16, 70),
            Size = new Size(588, 312),
            BackColor = bgPanel,
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12),
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 8; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

        var lblMinConfidence = MakeLabel("CalibrAI min confidence:", fgLight);
        _numMinConfidence.DecimalPlaces = 2;
        _numMinConfidence.Increment = 0.01M;
        _numMinConfidence.Minimum = 0.00M;
        _numMinConfidence.Maximum = 1.00M;
        _numMinConfidence.BackColor = bgControl;
        _numMinConfidence.ForeColor = fgLight;
        _numMinConfidence.Font = new Font("Segoe UI", 9.5F);
        _numMinConfidence.Dock = DockStyle.Fill;

        var lblCalibrAiUrl = MakeLabel("CalibrAI server URL:", fgLight);
        _txtCalibrAiUrl.Dock = DockStyle.Fill;
        _txtCalibrAiUrl.BackColor = bgControl;
        _txtCalibrAiUrl.ForeColor = fgLight;
        _txtCalibrAiUrl.BorderStyle = BorderStyle.FixedSingle;
        _txtCalibrAiUrl.Font = new Font("Segoe UI", 9.5F);
        _txtCalibrAiUrl.PlaceholderText = "http://localhost:8721";

        var lblTheme = MakeLabel("Theme:", fgLight);
        _cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTheme.Items.AddRange(new object[] { "Dark", "Light" });
        _cmbTheme.BackColor = bgControl;
        _cmbTheme.ForeColor = fgLight;
        _cmbTheme.Font = new Font("Segoe UI", 9.5F);
        _cmbTheme.Dock = DockStyle.Fill;

        var lblDefaultView = MakeLabel("Default table view:", fgLight);
        _cmbDefaultView.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbDefaultView.Items.AddRange(new object[] { "Text", "2D", "3D" });
        _cmbDefaultView.BackColor = bgControl;
        _cmbDefaultView.ForeColor = fgLight;
        _cmbDefaultView.Font = new Font("Segoe UI", 9.5F);
        _cmbDefaultView.Dock = DockStyle.Fill;

        var lblUiDensity = MakeLabel("UI density:", fgLight);
        _cmbUiDensity.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbUiDensity.Items.AddRange(new object[] { "Compact", "Comfortable", "Spacious" });
        _cmbUiDensity.BackColor = bgControl;
        _cmbUiDensity.ForeColor = fgLight;
        _cmbUiDensity.Font = new Font("Segoe UI", 9.5F);
        _cmbUiDensity.Dock = DockStyle.Fill;

        _chkAutoExpandTree.Text = "Auto-expand table/constant tree";
        _chkAutoExpandTree.ForeColor = fgLight;
        _chkAutoExpandTree.AutoSize = true;
        _chkAutoExpandTree.Dock = DockStyle.Left;

        _chkPromptUnsaved.Text = "Prompt before discarding unsaved BIN changes";
        _chkPromptUnsaved.ForeColor = fgLight;
        _chkPromptUnsaved.AutoSize = true;
        _chkPromptUnsaved.Dock = DockStyle.Left;

        _chkAutoLoadLastFiles.Text = "Auto-load last XDF/BIN on startup";
        _chkAutoLoadLastFiles.ForeColor = fgLight;
        _chkAutoLoadLastFiles.AutoSize = true;
        _chkAutoLoadLastFiles.Dock = DockStyle.Left;

        table.Controls.Add(lblMinConfidence, 0, 0);
        table.Controls.Add(_numMinConfidence, 1, 0);
        table.Controls.Add(lblCalibrAiUrl, 0, 1);
        table.Controls.Add(_txtCalibrAiUrl, 1, 1);
        table.Controls.Add(lblTheme, 0, 2);
        table.Controls.Add(_cmbTheme, 1, 2);
        table.Controls.Add(lblDefaultView, 0, 3);
        table.Controls.Add(_cmbDefaultView, 1, 3);
        table.Controls.Add(lblUiDensity, 0, 4);
        table.Controls.Add(_cmbUiDensity, 1, 4);
        table.Controls.Add(_chkAutoExpandTree, 1, 5);
        table.Controls.Add(_chkPromptUnsaved, 1, 6);
        table.Controls.Add(_chkAutoLoadLastFiles, 1, 7);

        panel.Controls.Add(table);

        _btnSave.Text = "Save";
        _btnSave.Size = new Size(94, 32);
        _btnSave.Location = new Point(412, 392);
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.BackColor = accent;
        _btnSave.ForeColor = Color.White;
        _btnSave.Cursor = Cursors.Hand;
        _btnSave.Click += BtnSave_Click;

        _btnCancel.Text = "Cancel";
        _btnCancel.Size = new Size(94, 32);
        _btnCancel.Location = new Point(510, 392);
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.BackColor = bgControl;
        _btnCancel.ForeColor = fgLight;
        _btnCancel.Cursor = Cursors.Hand;
        _btnCancel.Click += BtnCancel_Click;

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 436);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Settings";
        BackColor = bgDark;
        ForeColor = fgLight;

        Controls.Add(lblHeader);
        Controls.Add(lblHint);
        Controls.Add(panel);
        Controls.Add(_btnSave);
        Controls.Add(_btnCancel);
    }

    private static Label MakeLabel(string text, Color foreColor)
    {
        return new Label
        {
            Text = text,
            ForeColor = foreColor,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
        };
    }

    private void LoadSettingsIntoControls(AppSettings settings)
    {
        _numMinConfidence.Value = (decimal)Math.Clamp(settings.CalibrAiMinConfidence, 0.0f, 1.0f);
        _txtCalibrAiUrl.Text = settings.CalibrAiBaseUrl;
        _cmbTheme.SelectedIndex = settings.Theme switch
        {
            AppTheme.Light => 1,
            _ => 0,
        };
        _cmbDefaultView.SelectedIndex = settings.DefaultTableViewMode switch
        {
            TableViewMode.TwoD => 1,
            TableViewMode.ThreeD => 2,
            _ => 0,
        };
        _cmbUiDensity.SelectedIndex = settings.UiDensity switch
        {
            UiDensity.Compact => 0,
            UiDensity.Spacious => 2,
            _ => 1,
        };
        _chkAutoExpandTree.Checked = settings.AutoExpandTreeNodes;
        _chkPromptUnsaved.Checked = settings.PromptBeforeDiscardingBinChanges;
        _chkAutoLoadLastFiles.Checked = settings.AutoLoadLastFilesOnStartup;
    }

    private void CmbTheme_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ApplyThemePreview(GetSelectedTheme());
    }

    private void ApplyThemePreview(AppTheme theme)
    {
        ThemeUtility.ApplyTheme(this, theme);
    }

    private AppTheme GetSelectedTheme()
    {
        return _cmbTheme.SelectedIndex == 1 ? AppTheme.Light : AppTheme.Dark;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!TryNormalizeHttpUrl(_txtCalibrAiUrl.Text, out string normalizedUrl))
        {
            MessageBox.Show(
                this,
                "Enter a valid HTTP or HTTPS URL for CalibrAI (example: http://localhost:8721).",
                "Invalid URL",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            _txtCalibrAiUrl.Focus();
            return;
        }

        AppSettings updated = ResultSettings.Clone();
        updated.CalibrAiMinConfidence = (float)_numMinConfidence.Value;
        updated.CalibrAiBaseUrl = normalizedUrl;
        updated.Theme = GetSelectedTheme();
        updated.DefaultTableViewMode = _cmbDefaultView.SelectedIndex switch
        {
            1 => TableViewMode.TwoD,
            2 => TableViewMode.ThreeD,
            _ => TableViewMode.Text,
        };
        updated.UiDensity = _cmbUiDensity.SelectedIndex switch
        {
            0 => UiDensity.Compact,
            2 => UiDensity.Spacious,
            _ => UiDensity.Comfortable,
        };
        updated.AutoExpandTreeNodes = _chkAutoExpandTree.Checked;
        updated.PromptBeforeDiscardingBinChanges = _chkPromptUnsaved.Checked;
        updated.AutoLoadLastFilesOnStartup = _chkAutoLoadLastFiles.Checked;

        updated.Normalize();
        ResultSettings = updated;

        DialogResult = DialogResult.OK;
        Close();
    }

    private static bool TryNormalizeHttpUrl(string raw, out string normalized)
    {
        normalized = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "http://localhost:8721";

        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"http://{normalized}";
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        normalized = uri.ToString().TrimEnd('/');
        return true;
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
