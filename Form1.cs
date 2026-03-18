using OpenTuningTool.Forms;
using OpenTuningTool.Models;
using OpenTuningTool.Parsing;
using OpenTuningTool.Services;

namespace OpenTuningTool;

public partial class Form1 : Form
{
    private AppSettings _settings;
    private bool _startupRestoreAttempted;
    private XdfDocument? _document;
    private BinBuffer? _bin;
    private string? _binPath;
    private XdfTable? _selectedTable;
    private XdfConstant? _selectedConstant;
    private TableSearchForm? _tableSearchForm;
    private readonly CalibrAiClient _calibrAi = new();

    public Form1()
    {
        _settings = AppSettingsStore.Load();
        InitializeComponent();
        ApplySettingsToUi();
    }

    // -----------------------------------------------------------------------
    // Menu handlers
    // -----------------------------------------------------------------------

    private async void MenuOpenXdf_Click(object? sender, EventArgs e) => await OpenXdfAsync();

    private void MenuOpenBin_Click(object? sender, EventArgs e) => OpenBin();

    private void MenuSaveBin_Click(object? sender, EventArgs e) => SaveBin();

    private void MenuSaveBinAs_Click(object? sender, EventArgs e) => SaveBinAs();

    private void MenuSettings_Click(object? sender, EventArgs e) => OpenSettings();

    private void MenuExit_Click(object? sender, EventArgs e) => Application.Exit();

    private async void MenuDetectMaps_Click(object? sender, EventArgs e) => await DetectMapsAsync();

    // -----------------------------------------------------------------------
    // Settings
    // -----------------------------------------------------------------------

    private void OpenSettings()
    {
        using var settingsForm = new SettingsForm(_settings);
        if (settingsForm.ShowDialog(this) != DialogResult.OK) return;

        _settings = settingsForm.ResultSettings;
        _settings.Normalize();
        SaveSettingsQuietly();
        ApplySettingsToUi();
        SetStatus(
            $"Settings saved. CalibrAI min confidence: {_settings.CalibrAiMinConfidence:F2}, URL: {_settings.CalibrAiBaseUrl}");
    }

    private void ApplySettingsToUi()
    {
        _settings.Normalize();
        _calibrAi.SetBaseUrl(_settings.CalibrAiBaseUrl);
        ThemeUtility.ApplyTheme(this, _settings.Theme);
        ThemeUtility.ApplyUiDensity(this, _settings.UiDensity);

        if (tabControlView.TabPages.Count >= 3)
            tabControlView.SelectedIndex = GetDefaultTableViewIndex();

        if (_tableSearchForm != null && !_tableSearchForm.IsDisposed)
        {
            ThemeUtility.ApplyTheme(_tableSearchForm, _settings.Theme);
            ThemeUtility.ApplyUiDensity(_tableSearchForm, _settings.UiDensity);
        }
    }

    private int GetDefaultTableViewIndex()
    {
        return _settings.DefaultTableViewMode switch
        {
            TableViewMode.TwoD => 1,
            TableViewMode.ThreeD => 2,
            _ => 0,
        };
    }

    // -----------------------------------------------------------------------
    // Open XDF (async to avoid UI freeze)
    // -----------------------------------------------------------------------

    private async Task OpenXdfAsync()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open XDF File",
            Filter = "XDF Files (*.xdf)|*.xdf|All Files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        string path = dlg.FileName;
        SetStatus($"Loading {Path.GetFileName(path)}…");
        menuItemOpenXdf.Enabled = false;

        try
        {
            _document = await Task.Run(() => new XdfParser().Parse(path));
            _settings.LastXdfPath = path;
            SaveSettingsQuietly();
            ClearDetailPanel();
            RefreshTree();
            RefreshTableSearchWindow();
            menuItemDetect.Enabled = true;
            menuItemOpenBin.Enabled = true;
            SetStatus(
                $"Loaded: {Path.GetFileName(path)} " +
                $"— {_document.Tables.Count} tables, {_document.Constants.Count} constants" +
                $"  (base offset: 0x{_document.BaseOffset:X})");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open XDF file:\n\n{ex.Message}",
                "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Ready.");
        }
        finally
        {
            menuItemOpenXdf.Enabled = true;
        }
    }

    // -----------------------------------------------------------------------
    // Open BIN
    // -----------------------------------------------------------------------

    private void OpenBin()
    {
        if (ConfirmDiscardBinChanges() == false) return;

        using var dlg = new OpenFileDialog
        {
            Title = "Open ECU BIN File",
            Filter = "BIN Files (*.bin)|*.bin|All Files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            _bin = BinBuffer.Load(dlg.FileName);
            _binPath = dlg.FileName;
            _settings.LastBinPath = dlg.FileName;
            SaveSettingsQuietly();
            menuItemSaveBin.Enabled = true;
            menuItemSaveBinAs.Enabled = true;
            UpdateTitleBar();
            SetStatus($"BIN loaded: {Path.GetFileName(dlg.FileName)}  ({_bin.Length:N0} bytes)");

            // Refresh detail panel to show values
            RefreshDetailPanel();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load BIN file:\n\n{ex.Message}",
                "BIN Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // -----------------------------------------------------------------------
    // Save BIN
    // -----------------------------------------------------------------------

    private void SaveBin()
    {
        if (_bin == null) return;
        if (_binPath == null) { SaveBinAs(); return; }

        try
        {
            _bin.Save(_binPath);
            UpdateTitleBar();
            SetStatus($"Saved: {Path.GetFileName(_binPath)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "Save Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveBinAs()
    {
        if (_bin == null) return;

        using var dlg = new SaveFileDialog
        {
            Title = "Save BIN File As",
            Filter = "BIN Files (*.bin)|*.bin|All Files (*.*)|*.*",
            FileName = _binPath != null ? Path.GetFileName(_binPath) : "output.bin",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            _bin.Save(dlg.FileName);
            _binPath = dlg.FileName;
            _settings.LastBinPath = dlg.FileName;
            SaveSettingsQuietly();
            UpdateTitleBar();
            SetStatus($"Saved: {Path.GetFileName(_binPath)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed:\n\n{ex.Message}", "Save Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // -----------------------------------------------------------------------
    // Populate TreeView
    // -----------------------------------------------------------------------

    private void RefreshTree()
    {
        if (_document == null)
        {
            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            treeView.EndUpdate();
            return;
        }

        object? selectedTag = treeView.SelectedNode?.Tag;
        TreeNode? selectedNode = null;

        treeView.BeginUpdate();
        treeView.Nodes.Clear();

        var tablesNode = new TreeNode($"Tables ({_document.Tables.Count})")
        {
            NodeFont = new Font(treeView.Font, FontStyle.Bold)
        };
        foreach (var table in _document.Tables)
        {
            var node = new TreeNode(table.Title) { Tag = table };
            tablesNode.Nodes.Add(node);
            if (ReferenceEquals(table, selectedTag))
                selectedNode = node;
        }

        treeView.Nodes.Add(tablesNode);
        var constantsNode = new TreeNode($"Constants ({_document.Constants.Count})")
        {
            NodeFont = new Font(treeView.Font, FontStyle.Bold)
        };
        foreach (var constant in _document.Constants)
        {
            var node = new TreeNode(constant.Title) { Tag = constant };
            constantsNode.Nodes.Add(node);
            if (ReferenceEquals(constant, selectedTag))
                selectedNode = node;
        }

        treeView.Nodes.Add(constantsNode);
        if (_settings.AutoExpandTreeNodes)
        {
            tablesNode.Expand();
            constantsNode.Expand();
        }
        treeView.EndUpdate();

        if (selectedNode != null) treeView.SelectedNode = selectedNode;
        else if (selectedTag is XdfObject) ClearDetailPanel();
    }

    private static bool TableMatchesSearch(XdfTable table, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return true;

        string[] terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string term in terms)
        {
            bool titleMatch = table.Title.Contains(term, StringComparison.OrdinalIgnoreCase);
            bool descriptionMatch = table.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
            if (!titleMatch && !descriptionMatch)
                return false;
        }

        return true;
    }

    private IReadOnlyList<XdfTable> FindMatchingTables(string searchText)
    {
        if (_document == null) return Array.Empty<XdfTable>();
        if (string.IsNullOrWhiteSpace(searchText)) return Array.Empty<XdfTable>();
        return _document.Tables.Where(table => TableMatchesSearch(table, searchText)).ToList();
    }

    private void OpenTableSearchWindow()
    {
        if (_document == null)
        {
            MessageBox.Show(
                "Load an XDF file before searching for tables.",
                "No XDF Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_tableSearchForm == null || _tableSearchForm.IsDisposed)
        {
            _tableSearchForm = new TableSearchForm(
                FindMatchingTables,
                SelectTableFromSearch,
                _settings.UiDensity,
                _settings.Theme);
            _tableSearchForm.FormClosed += TableSearchForm_FormClosed;
            _tableSearchForm.Show(this);
        }
        else
        {
            _tableSearchForm.RefreshResults();
            if (_tableSearchForm.WindowState == FormWindowState.Minimized)
                _tableSearchForm.WindowState = FormWindowState.Normal;

            _tableSearchForm.BringToFront();
        }

        _tableSearchForm.FocusSearchBox();
    }

    private void RefreshTableSearchWindow()
    {
        if (_tableSearchForm == null || _tableSearchForm.IsDisposed) return;
        _tableSearchForm.RefreshResults();
    }

    private void TableSearchForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _tableSearchForm))
            _tableSearchForm = null;
    }

    private void SelectTableFromSearch(XdfTable table)
    {
        SelectObjectInTree(table);
        SetStatus($"Selected table: {table.Title}");
    }

    private void SelectObjectInTree(XdfObject target)
    {
        foreach (TreeNode categoryNode in treeView.Nodes)
        {
            foreach (TreeNode objectNode in categoryNode.Nodes)
            {
                if (!ReferenceEquals(objectNode.Tag, target)) continue;

                categoryNode.Expand();
                treeView.SelectedNode = objectNode;
                objectNode.EnsureVisible();
                treeView.Focus();
                return;
            }
        }
    }

    // -----------------------------------------------------------------------
    // TreeView selection → detail panel
    // -----------------------------------------------------------------------

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is XdfTable table)       ShowTableDetail(table);
        else if (e.Node?.Tag is XdfConstant con) ShowConstantDetail(con);
        else                                     ClearDetailPanel();
    }

    private void RefreshDetailPanel()
    {
        if (_selectedTable != null)    ShowTableDetail(_selectedTable);
        else if (_selectedConstant != null) ShowConstantDetail(_selectedConstant);
    }

    // -----------------------------------------------------------------------
    // Show Table detail
    // -----------------------------------------------------------------------

    private void ShowTableDetail(XdfTable table)
    {
        _selectedTable   = table;
        _selectedConstant = null;

        lblDetailTitle.Text = table.Title;
        lblIdValue.Text     = $"0x{table.UniqueId:X}";
        lblDescValue.Text   = table.Description ?? "(none)";

        lblXAxisLabel.Visible = true; lblXAxisValue.Visible = true;
        lblYAxisLabel.Visible = true; lblYAxisValue.Visible = true;
        lblDataLabel.Visible  = true; lblDataValue.Visible  = true;
        lblAddressLabel.Visible = false; lblAddressValue.Visible = false;

        lblDataLabel.Text = "Data:";

        lblXAxisValue.Text = table.XAxis != null
            ? $"{table.XAxis.IndexCount} elements" +
              (table.XAxis.Address.HasValue ? $" @ 0x{_document!.BaseOffset + table.XAxis.Address.Value:X}" : " (inline)") +
              $"  [{table.XAxis.ElementSizeBits}-bit]"
            : "(none)";

        lblYAxisValue.Text = table.YAxis != null
            ? $"{table.YAxis.IndexCount} elements" +
              (table.YAxis.Address.HasValue ? $" @ 0x{_document!.BaseOffset + table.YAxis.Address.Value:X}" : " (inline)") +
              $"  [{table.YAxis.ElementSizeBits}-bit]"
            : "(none)";

        if (table.ZAxis != null)
        {
            int absAddr = _document!.BaseOffset + table.ZAxis.Address;
            lblDataValue.Text =
                $"{table.ZAxis.RowCount}×{table.ZAxis.ColCount}" +
                $" @ 0x{absAddr:X}" +
                $"  [{table.ZAxis.ElementSizeBits}-bit, " +
                (table.ZAxis.MajorStrideBits < 0 ? "big-endian" : "little-endian") + "]";
        }
        else lblDataValue.Text = "(none)";

        panelDetail.Visible = true;

        // Values area
        if (_bin != null && table.ZAxis != null)
        {
            int absAddr   = _document!.BaseOffset + table.ZAxis.Address;
            bool bigEndian = table.ZAxis.MajorStrideBits < 0;
            int byteCount = table.ZAxis.RowCount * table.ZAxis.ColCount * (table.ZAxis.ElementSizeBits / 8);

            if (_bin.IsAddressValid(absAddr, byteCount))
            {
                double[] values = _bin.ReadMap(absAddr, table.ZAxis.RowCount, table.ZAxis.ColCount,
                                               table.ZAxis.ElementSizeBits, bigEndian);
                LoadMapIntoGrid(table, values);
                tabControlView.SelectedIndex = GetDefaultTableViewIndex();
                tabControlView.Visible  = true;
                panelConstantValue.Visible = false;
                lblNoBin.Visible        = false;
            }
            else
            {
                lblNoBin.Text    = $"Address 0x{absAddr:X} is outside the BIN ({_bin.Length:N0} bytes).";
                lblNoBin.Visible = true; tabControlView.Visible = false; panelConstantValue.Visible = false;
            }
        }
        else
        {
            lblNoBin.Text    = _bin == null
                ? "Open a BIN file (File → Open BIN…) to view and edit values."
                : "No data address for this table.";
            lblNoBin.Visible = true; tabControlView.Visible = false; panelConstantValue.Visible = false;
        }
    }

    // -----------------------------------------------------------------------
    // Show Constant detail
    // -----------------------------------------------------------------------

    private void ShowConstantDetail(XdfConstant constant)
    {
        _selectedTable    = null;
        _selectedConstant = constant;

        lblDetailTitle.Text  = constant.Title;
        lblIdValue.Text      = $"0x{constant.UniqueId:X}";
        lblDescValue.Text    = constant.Description ?? "(none)";

        int absAddr = _document!.BaseOffset + constant.Address;
        lblAddressValue.Text = $"0x{absAddr:X}";

        lblXAxisLabel.Visible = false; lblXAxisValue.Visible = false;
        lblYAxisLabel.Visible = false; lblYAxisValue.Visible = false;
        lblAddressLabel.Visible = true; lblAddressValue.Visible = true;

        lblDataLabel.Text  = "Element Size:";
        lblDataValue.Text  = $"{constant.ElementSizeBits} bits";
        lblDataLabel.Visible = true; lblDataValue.Visible = true;

        panelDetail.Visible = true;

        // Values area
        if (_bin != null)
        {
            int byteCount = constant.ElementSizeBits / 8;
            if (_bin.IsAddressValid(absAddr, byteCount))
            {
                // Determine endianness: negative stride not available for constants; use 8-bit as LE,
                // 16/32-bit default to big-endian (typical Bosch/Siemens).
                bool bigEndian = constant.ElementSizeBits > 8;
                double[] vals = _bin.ReadMap(absAddr, 1, 1, constant.ElementSizeBits, bigEndian);
                txtConstValue.Text = ((long)vals[0]).ToString();
                lblConstEndian.Text = bigEndian ? "(big-endian)" : "(little-endian)";
                tabControlView.Visible = false; panelConstantValue.Visible = true; lblNoBin.Visible = false;
            }
            else
            {
                lblNoBin.Text    = $"Address 0x{absAddr:X} is outside the BIN ({_bin.Length:N0} bytes).";
                lblNoBin.Visible = true; tabControlView.Visible = false; panelConstantValue.Visible = false;
            }
        }
        else
        {
            lblNoBin.Text    = "Open a BIN file (File → Open BIN…) to view and edit values.";
            lblNoBin.Visible = true; tabControlView.Visible = false; panelConstantValue.Visible = false;
        }
    }

    private void ClearDetailPanel()
    {
        _selectedTable    = null;
        _selectedConstant = null;
        panelDetail.Visible = false;
    }

    // -----------------------------------------------------------------------
    // Map DataGridView helpers
    // -----------------------------------------------------------------------

    private void LoadMapIntoGrid(XdfTable table, double[] values)
    {
        int rows = table.ZAxis!.RowCount;
        int cols = table.ZAxis.ColCount;

        dgvMap.SuspendLayout();
        dgvMap.Rows.Clear();
        dgvMap.Columns.Clear();

        // Column headers from X-axis values if readable, else indices
        double[]? xVals = TryReadAxisValues(table.XAxis);
        for (int c = 0; c < cols; c++)
        {
            string header = xVals != null ? ((long)xVals[c]).ToString() : c.ToString();
            dgvMap.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }

        // Row headers from Y-axis values if readable, else indices
        double[]? yVals = TryReadAxisValues(table.YAxis);
        for (int r = 0; r < rows; r++)
        {
            string rowHeader = yVals != null ? ((long)yVals[r]).ToString() : r.ToString();
            int idx = dgvMap.Rows.Add();
            dgvMap.Rows[idx].HeaderCell.Value = rowHeader;
            for (int c = 0; c < cols; c++)
                dgvMap.Rows[idx].Cells[c].Value = (long)values[r * cols + c];
        }

        dgvMap.RowHeadersWidth = 60;
        dgvMap.ResumeLayout();
    }

    private double[]? TryReadAxisValues(XdfAxis? axis)
    {
        if (_bin == null || axis == null || !axis.Address.HasValue) return null;
        int absAddr  = _document!.BaseOffset + axis.Address.Value;
        bool bigEndian = axis.MajorStrideBits < 0;
        int byteCount = axis.IndexCount * (axis.ElementSizeBits / 8);
        if (!_bin.IsAddressValid(absAddr, byteCount)) return null;
        return _bin.ReadMap(absAddr, 1, axis.IndexCount, axis.ElementSizeBits, bigEndian);
    }

    // -----------------------------------------------------------------------
    // Map cell editing — write back to BIN
    // -----------------------------------------------------------------------

    private void DgvMap_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_bin == null || _selectedTable?.ZAxis == null) return;
        var cell = dgvMap.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!double.TryParse(cell.Value?.ToString(), out double newValue)) return;

        var z = _selectedTable.ZAxis;
        int absAddr   = _document!.BaseOffset + z.Address;
        bool bigEndian = z.MajorStrideBits < 0;
        int elemBytes  = z.ElementSizeBits / 8;
        int cellIndex  = e.RowIndex * z.ColCount + e.ColumnIndex;

        _bin.WriteCell(absAddr + cellIndex * elemBytes, z.ElementSizeBits, bigEndian, newValue);
        UpdateTitleBar();
    }

    // -----------------------------------------------------------------------
    // Constant value apply
    // -----------------------------------------------------------------------

    private void BtnApplyValue_Click(object? sender, EventArgs e)
    {
        if (_bin == null || _selectedConstant == null) return;
        if (!double.TryParse(txtConstValue.Text, out double value))
        {
            MessageBox.Show("Enter a valid number.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int absAddr   = _document!.BaseOffset + _selectedConstant.Address;
        bool bigEndian = _selectedConstant.ElementSizeBits > 8;
        _bin.WriteCell(absAddr, _selectedConstant.ElementSizeBits, bigEndian, value);
        UpdateTitleBar();
        SetStatus($"Updated {_selectedConstant.Title} → {(long)value}");
    }

    // -----------------------------------------------------------------------
    // CalibrAI detection
    // -----------------------------------------------------------------------

    private async Task DetectMapsAsync()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select ECU BIN File for CalibrAI Detection",
            Filter = "BIN Files (*.bin)|*.bin|All Files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        SetStatus($"Detecting maps in {Path.GetFileName(dlg.FileName)}…");
        menuItemDetect.Enabled = false;

        List<MapCandidateResult> candidates;
        try
        {
            candidates = await _calibrAi.DetectAsync(dlg.FileName, _settings.CalibrAiMinConfidence);
        }
        catch (HttpRequestException)
        {
            MessageBox.Show(
                "Could not connect to CalibrAI at localhost:8721.\n\nStart it with:  calibrai serve",
                "CalibrAI Not Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus("CalibrAI: connection failed.");
            menuItemDetect.Enabled = true;
            return;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SetStatus("CalibrAI: no model loaded.");
            menuItemDetect.Enabled = true;
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error:\n\n{ex.Message}", "Detection Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("CalibrAI: error.");
            menuItemDetect.Enabled = true;
            return;
        }
        finally
        {
            menuItemDetect.Enabled = true;
        }

        if (candidates.Count == 0)
        {
            MessageBox.Show("No map candidates found above the confidence threshold.",
                "No Candidates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetStatus("CalibrAI: no candidates.");
            return;
        }

        using var resultsForm = new DetectResultsForm(candidates, _settings.UiDensity, _settings.Theme);
        if (resultsForm.ShowDialog(this) != DialogResult.OK)
        {
            SetStatus("CalibrAI detection cancelled.");
            return;
        }

        if (_document == null) _document = new XdfDocument();

        int imported = 0;
        foreach (var c in resultsForm.SelectedCandidates)
        {
            ImportCandidate(c);
            imported++;
        }

        RefreshTree();
        RefreshTableSearchWindow();
        SetStatus($"Imported {imported} map(s) from CalibrAI. Document: {_document.Tables.Count} tables.");
    }

    // -----------------------------------------------------------------------
    // Import a CalibrAI candidate
    // -----------------------------------------------------------------------

    private void ImportCandidate(MapCandidateResult c)
    {
        int uniqueId    = _document!.Objects.Keys.Any() ? _document.Objects.Keys.Max() + 1 : 1;
        int baseOffset  = _document.BaseOffset;

        // CalibrAI returns absolute file offsets; convert back to raw XDF addresses
        int rawZAddr = c.Address - baseOffset;
        int? rawXAddr = c.XAxisAddress.HasValue ? c.XAxisAddress.Value - baseOffset : null;
        int? rawYAddr = c.YAxisAddress.HasValue ? c.YAxisAddress.Value - baseOffset : null;

        var xAxis = new XdfAxis('x', c.Cols, rawXAddr, c.ElementSizeBits, c.ElementSizeBits, 0);
        var yAxis = new XdfAxis('y', c.Rows, rawYAddr, c.ElementSizeBits, c.ElementSizeBits, 0);
        var zData = new XdfTableData(rawZAddr, c.Rows, c.Cols, c.ElementSizeBits, 0, 0);

        _document.AddTable(new XdfTable(
            uniqueId,
            $"AI_MAP_{c.AddressHex}",
            $"Auto-detected by CalibrAI (confidence: {c.Confidence:F2})",
            xAxis, yAxis, zData));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void UpdateTitleBar()
    {
        string dirty = (_bin?.IsDirty == true) ? " *" : "";
        string binName = _binPath != null ? $" — {Path.GetFileName(_binPath)}{dirty}" : "";
        Text = $"OpenTuningTool{binName}";
    }

    private void SetStatus(string message) => statusLabel.Text = message;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            OpenTableSearchWindow();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Returns false if the user chose to cancel (keep editing).</summary>
    private bool ConfirmDiscardBinChanges()
    {
        if (!_settings.PromptBeforeDiscardingBinChanges) return true;
        if (_bin == null || !_bin.IsDirty) return true;
        var result = MessageBox.Show(
            "The current BIN has unsaved changes. Discard them?",
            "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return result == DialogResult.Yes;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmDiscardBinChanges())
            e.Cancel = true;
        base.OnFormClosing(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (_startupRestoreAttempted) return;
        _startupRestoreAttempted = true;
        TryAutoLoadLastFilesOnStartup();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _calibrAi.Dispose();
        base.OnFormClosed(e);
    }

    private void TryAutoLoadLastFilesOnStartup()
    {
        if (!_settings.AutoLoadLastFilesOnStartup) return;

        bool loadedXdf = false;
        bool loadedBin = false;
        var warnings = new List<string>();

        if (!string.IsNullOrWhiteSpace(_settings.LastXdfPath))
        {
            if (File.Exists(_settings.LastXdfPath))
            {
                try
                {
                    _document = new XdfParser().Parse(_settings.LastXdfPath);
                    loadedXdf = true;
                    menuItemDetect.Enabled = true;
                    menuItemOpenBin.Enabled = true;
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not auto-load XDF: {ex.Message}");
                }
            }
            else
            {
                warnings.Add($"Last XDF file not found: {_settings.LastXdfPath}");
            }
        }

        if (!string.IsNullOrWhiteSpace(_settings.LastBinPath))
        {
            if (File.Exists(_settings.LastBinPath))
            {
                try
                {
                    _bin = BinBuffer.Load(_settings.LastBinPath);
                    _binPath = _settings.LastBinPath;
                    loadedBin = true;
                    menuItemSaveBin.Enabled = true;
                    menuItemSaveBinAs.Enabled = true;
                    UpdateTitleBar();
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not auto-load BIN: {ex.Message}");
                }
            }
            else
            {
                warnings.Add($"Last BIN file not found: {_settings.LastBinPath}");
            }
        }

        if (loadedXdf)
        {
            ClearDetailPanel();
            RefreshTree();
            RefreshTableSearchWindow();
        }

        if (loadedBin)
            RefreshDetailPanel();

        if (loadedXdf || loadedBin)
        {
            string loadedParts =
                (loadedXdf ? "XDF" : string.Empty) +
                (loadedXdf && loadedBin ? " + " : string.Empty) +
                (loadedBin ? "BIN" : string.Empty);

            SetStatus($"Auto-loaded previous session: {loadedParts}.");
        }

        if (warnings.Count > 0)
        {
            MessageBox.Show(
                this,
                string.Join("\n", warnings),
                "Startup Restore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void SaveSettingsQuietly()
    {
        try
        {
            AppSettingsStore.Save(_settings);
        }
        catch
        {
            // Ignore settings write errors to avoid interrupting core workflows.
        }
    }
}
