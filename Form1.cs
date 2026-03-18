using OpenTuningTool.Forms;
using OpenTuningTool.Models;
using OpenTuningTool.Parsing;
using OpenTuningTool.Services;
using System.Globalization;

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
    private Form? _detachedDetailForm;
    private bool _isMainFormClosing;
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

        if (_detachedDetailForm != null && !_detachedDetailForm.IsDisposed)
        {
            ThemeUtility.ApplyTheme(_detachedDetailForm, _settings.Theme);
            ThemeUtility.ApplyUiDensity(_detachedDetailForm, _settings.UiDensity);
        }

        UpdateDetailDetachUi();
        UpdateDetachedDetailWindowTitle();

        if (_selectedTable != null || _selectedConstant != null)
            RefreshDetailPanel();
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
            btnToolOpenBin.Enabled = true;
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
            btnToolSave.Enabled = true;
            UpdateTitleBar();
            statusBinInfo.Text = $"{_bin.Length:N0} bytes";
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

    private static string[] SplitSearchTerms(string searchText)
    {
        return string.IsNullOrWhiteSpace(searchText)
            ? Array.Empty<string>()
            : searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TableMatchesSearch(XdfTable table, string searchText)
    {
        return TableMatchesSearch(table, SplitSearchTerms(searchText));
    }

    private static bool TableMatchesSearch(XdfTable table, IReadOnlyList<string> searchTerms)
    {
        if (searchTerms.Count == 0) return true;

        foreach (string term in searchTerms)
        {
            bool titleMatch = table.Title.Contains(term, StringComparison.OrdinalIgnoreCase);
            bool descriptionMatch = table.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;
            if (!titleMatch && !descriptionMatch)
                return false;
        }

        return true;
    }

    private static bool ConstantMatchesSearch(XdfConstant constant, string searchText)
    {
        return constant.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               (constant.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true);
    }

    private IReadOnlyList<XdfTable> FindMatchingTables(string searchText)
    {
        if (_document == null) return Array.Empty<XdfTable>();
        string[] searchTerms = SplitSearchTerms(searchText);
        if (searchTerms.Length == 0) return Array.Empty<XdfTable>();
        return _document.Tables.Where(table => TableMatchesSearch(table, searchTerms)).ToList();
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

    private bool IsDetailDetached => panelDetail.Parent != splitContainer.Panel2;

    private void BtnDetachDetail_Click(object? sender, EventArgs e)
    {
        if (IsDetailDetached) AttachDetailPanel();
        else DetachDetailPanel();
    }

    private void BtnReattachDetail_Click(object? sender, EventArgs e) => AttachDetailPanel();

    private void DetachDetailPanel()
    {
        EnsureDetachedDetailForm();
        if (_detachedDetailForm == null)
            return;

        panelDetail.Parent?.Controls.Remove(panelDetail);
        _detachedDetailForm.Controls.Add(panelDetail);
        panelDetail.Dock = DockStyle.Fill;
        panelDetail.BringToFront();

        panelDetachedPlaceholder.Visible = true;
        panelDetachedPlaceholder.BringToFront();
        UpdateDetailDetachUi();
        UpdateDetachedDetailWindowTitle();

        if (!_detachedDetailForm.Visible)
            _detachedDetailForm.Show(this);
        else
            _detachedDetailForm.BringToFront();
    }

    private void AttachDetailPanel()
    {
        if (panelDetail.Parent == splitContainer.Panel2)
        {
            panelDetachedPlaceholder.Visible = false;
            UpdateDetailDetachUi();
            return;
        }

        panelDetail.Parent?.Controls.Remove(panelDetail);
        splitContainer.Panel2.Controls.Add(panelDetail);
        panelDetail.Dock = DockStyle.Fill;
        panelDetail.BringToFront();
        panelDetachedPlaceholder.Visible = false;
        UpdateDetailDetachUi();

        if (_detachedDetailForm != null && !_detachedDetailForm.IsDisposed)
        {
            Form detached = _detachedDetailForm;
            _detachedDetailForm = null;
            detached.Close();
        }
    }

    private void EnsureDetachedDetailForm()
    {
        if (_detachedDetailForm != null && !_detachedDetailForm.IsDisposed)
            return;

        _detachedDetailForm = new Form
        {
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            Size = new Size(Math.Max(540, splitContainer.Panel2.Width), Math.Max(420, splitContainer.Panel2.Height + 70)),
            MinimumSize = new Size(480, 360),
            FormBorderStyle = FormBorderStyle.Sizable,
        };

        Rectangle sourceBounds = RectangleToScreen(splitContainer.Panel2.Bounds);
        _detachedDetailForm.Location = new Point(
            Math.Max(0, sourceBounds.Left + 40),
            Math.Max(0, sourceBounds.Top + 40));
        _detachedDetailForm.FormClosed += DetachedDetailForm_FormClosed;

        ThemeUtility.ApplyTheme(_detachedDetailForm, _settings.Theme);
        ThemeUtility.ApplyUiDensity(_detachedDetailForm, _settings.UiDensity);
        UpdateDetachedDetailWindowTitle();
    }

    private void DetachedDetailForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!ReferenceEquals(sender, _detachedDetailForm) && sender is not Form)
            return;

        if (ReferenceEquals(sender, _detachedDetailForm))
            _detachedDetailForm = null;

        if (_isMainFormClosing || panelDetail.IsDisposed)
            return;

        AttachDetailPanel();
    }

    private void UpdateDetailDetachUi()
    {
        bool detached = IsDetailDetached;
        btnDetachDetail.Text = detached ? "Dock" : "Detach";
        btnReattachDetail.Visible = detached;
        panelDetachedPlaceholder.Visible = detached;
    }

    private void UpdateDetachedDetailWindowTitle()
    {
        if (_detachedDetailForm == null || _detachedDetailForm.IsDisposed)
            return;

        string detailName = _selectedTable?.Title ?? _selectedConstant?.Title ?? "Data View";
        _detachedDetailForm.Text = $"Data View — {detailName}";
    }

    // -----------------------------------------------------------------------
    // TreeView selection → detail panel
    // -----------------------------------------------------------------------

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is XdfTable table)
        {
            if (ReferenceEquals(_selectedTable, table) && _selectedConstant == null) return;
            ShowTableDetail(table);
        }
        else if (e.Node?.Tag is XdfConstant con)
        {
            if (ReferenceEquals(_selectedConstant, con) && _selectedTable == null) return;
            ShowConstantDetail(con);
        }
        else
        {
            ClearDetailPanel();
        }
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
                GetEndianLabel(table.ZAxis.Format) + "]";
        }
        else lblDataValue.Text = "(none)";

        panelDetail.Visible = true;
        UpdateDetachedDetailWindowTitle();

        // Values area
        if (_bin != null && table.ZAxis != null)
        {
            int absAddr   = _document!.BaseOffset + table.ZAxis.Address;
            int byteCount = table.ZAxis.RowCount * table.ZAxis.ColCount * (table.ZAxis.ElementSizeBits / 8);

            if (_bin.IsAddressValid(absAddr, byteCount))
            {
                double[] values = _bin.ReadMap(
                    absAddr,
                    table.ZAxis.RowCount,
                    table.ZAxis.ColCount,
                    table.ZAxis.ElementSizeBits,
                    table.ZAxis.Format);
                LoadMapIntoGrid(table, values);
                LoadVisualizationViews(table, values);
                tabControlView.SelectedIndex = GetDefaultTableViewIndex();
                tabControlView.Visible  = true;
                panelConstantValue.Visible = false;
                lblNoBin.Visible        = false;
                statusDimensions.Text = $"{table.ZAxis.RowCount}\u00D7{table.ZAxis.ColCount}";
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
        UpdateDetachedDetailWindowTitle();

        // Values area
        if (_bin != null)
        {
            int byteCount = constant.ElementSizeBits / 8;
            if (_bin.IsAddressValid(absAddr, byteCount))
            {
                double[] vals = _bin.ReadMap(absAddr, 1, 1, constant.ElementSizeBits, constant.Format);
                bool canEdit = CanEditValue(constant.Format, constant.ElementSizeBits);
                txtConstValue.Text = FormatDisplayValue(vals[0], constant.Format);
                txtConstValue.ReadOnly = !canEdit;
                btnApplyValue.Enabled = canEdit;
                lblConstEndian.Text = canEdit
                    ? $"({GetEndianLabel(constant.Format)})"
                    : $"({GetEndianLabel(constant.Format)}, read-only)";
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
        UpdateDetachedDetailWindowTitle();
    }

    // -----------------------------------------------------------------------
    // Map DataGridView helpers
    // -----------------------------------------------------------------------

    private void LoadMapIntoGrid(XdfTable table, double[] values)
    {
        int rows = table.ZAxis!.RowCount;
        int cols = table.ZAxis.ColCount;
        XdfValueFormat zFormat = table.ZAxis.Format;

        dgvMap.SuspendLayout();
        dgvMap.Rows.Clear();
        dgvMap.Columns.Clear();
        dgvMap.ReadOnly = !CanEditValue(zFormat, table.ZAxis.ElementSizeBits);

        // Column headers from X-axis values if readable, else indices
        double[]? xVals = TryReadAxisValues(table.XAxis);
        for (int c = 0; c < cols; c++)
        {
            string header = GetAxisDisplayLabel(table.XAxis, c, xVals);
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
            string rowHeader = GetAxisDisplayLabel(table.YAxis, r, yVals);
            int idx = dgvMap.Rows.Add();
            dgvMap.Rows[idx].HeaderCell.Value = rowHeader;
            for (int c = 0; c < cols; c++)
                dgvMap.Rows[idx].Cells[c].Value = FormatDisplayValue(values[r * cols + c], zFormat);
        }

        ResizeMapGridColumns();
        dgvMap.ResumeLayout();
    }

    private void ResizeMapGridColumns()
    {
        if (dgvMap.Columns.Count == 0)
        {
            dgvMap.RowHeadersWidth = 60;
            return;
        }

        const int cellPadding = 20;
        const int headerPadding = 24;
        TextFormatFlags measureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        Font headerFont = dgvMap.ColumnHeadersDefaultCellStyle.Font ?? dgvMap.Font;
        Font cellFont = dgvMap.DefaultCellStyle.Font ?? dgvMap.Font;
        Font rowHeaderFont = dgvMap.RowHeadersDefaultCellStyle.Font ?? dgvMap.Font;

        foreach (DataGridViewColumn column in dgvMap.Columns)
        {
            int width = MeasureGridText(column.HeaderText, headerFont, measureFlags) + headerPadding;

            foreach (DataGridViewRow row in dgvMap.Rows)
            {
                if (row.IsNewRow) continue;

                DataGridViewCell cell = row.Cells[column.Index];
                string text = Convert.ToString(cell.FormattedValue ?? cell.Value, CultureInfo.CurrentCulture) ?? string.Empty;
                width = Math.Max(width, MeasureGridText(text, cellFont, measureFlags) + cellPadding);
            }

            column.Width = Math.Max(width, 56);
        }

        int rowHeaderWidth = 60;
        foreach (DataGridViewRow row in dgvMap.Rows)
        {
            if (row.IsNewRow) continue;

            string text = Convert.ToString(row.HeaderCell.FormattedValue ?? row.HeaderCell.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            rowHeaderWidth = Math.Max(rowHeaderWidth, MeasureGridText(text, rowHeaderFont, measureFlags) + cellPadding);
        }

        dgvMap.RowHeadersWidth = rowHeaderWidth;
    }

    private static int MeasureGridText(string? text, Font font, TextFormatFlags flags)
    {
        string measureText = string.IsNullOrEmpty(text) ? " " : text;
        return TextRenderer.MeasureText(measureText, font, Size.Empty, flags).Width;
    }

    private double[]? TryReadAxisValues(XdfAxis? axis)
    {
        if (_bin == null || _document == null || axis == null)
            return null;

        XdfTable? breakpointTable = TryResolveAxisBreakpointTable(axis);
        if (breakpointTable?.ZAxis != null)
        {
            XdfTableData zAxis = breakpointTable.ZAxis;
            int absAddr = _document.BaseOffset + zAxis.Address;
            int valueCount = zAxis.RowCount * zAxis.ColCount;
            int byteCount = valueCount * (zAxis.ElementSizeBits / 8);
            if (!_bin.IsAddressValid(absAddr, byteCount))
                return null;

            double[] values = _bin.ReadMap(absAddr, zAxis.RowCount, zAxis.ColCount, zAxis.ElementSizeBits, zAxis.Format);
            if (values.Length == axis.IndexCount)
                return values;

            if (values.Length > axis.IndexCount)
                return values.Take(axis.IndexCount).ToArray();

            return null;
        }

        if (!axis.Address.HasValue)
            return null;

        int axisAbsAddr = _document.BaseOffset + axis.Address.Value;
        int axisByteCount = axis.IndexCount * (axis.ElementSizeBits / 8);
        if (!_bin.IsAddressValid(axisAbsAddr, axisByteCount))
            return null;

        return _bin.ReadMap(axisAbsAddr, 1, axis.IndexCount, axis.ElementSizeBits, axis.Format);
    }

    private XdfTable? TryResolveAxisBreakpointTable(XdfAxis axis)
    {
        if (_document == null || !axis.Address.HasValue)
            return null;

        int rawAddress = axis.Address.Value;

        XdfTable? exactMatch = _document.Tables.FirstOrDefault(table =>
            !ReferenceEquals(table, _selectedTable) &&
            table.ZAxis != null &&
            table.ZAxis.Address == rawAddress &&
            table.ZAxis.RowCount * table.ZAxis.ColCount == axis.IndexCount);

        if (exactMatch != null)
            return exactMatch;

        return _document.Tables.FirstOrDefault(table =>
            !ReferenceEquals(table, _selectedTable) &&
            table.ZAxis != null &&
            table.ZAxis.Address == rawAddress);
    }

    private string GetAxisDisplayLabel(XdfAxis? axis, int index, double[]? values)
    {
        XdfValueFormat displayFormat = axis != null
            ? TryResolveAxisBreakpointTable(axis)?.ZAxis?.Format ?? axis.Format
            : XdfValueFormat.Identity;

        if (axis != null && values != null && index < values.Length)
            return FormatDisplayValue(values[index], displayFormat);

        if (axis != null && axis.Labels.TryGetValue(index, out string? label))
            return label;

        return index.ToString(CultureInfo.CurrentCulture);
    }

    // -----------------------------------------------------------------------
    // Map cell editing — write back to BIN
    // -----------------------------------------------------------------------

    private void DgvMap_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_bin == null || _selectedTable?.ZAxis == null) return;
        var z = _selectedTable.ZAxis;

        if (!CanEditValue(z.Format, z.ElementSizeBits))
        {
            RefreshDetailPanel();
            return;
        }

        var cell = dgvMap.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!TryParseDisplayValue(cell.Value?.ToString(), z.Format, out double displayValue))
        {
            MessageBox.Show("Enter a valid value.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshDetailPanel();
            return;
        }

        int absAddr   = _document!.BaseOffset + z.Address;
        int elemBytes  = z.ElementSizeBits / 8;
        int cellIndex  = e.RowIndex * z.ColCount + e.ColumnIndex;
        if (!TryConvertDisplayToRaw(displayValue, z.Format, z.ElementSizeBits, out double rawValue))
        {
            MessageBox.Show(
                "This table uses a conversion formula that can't be written back yet.",
                "Write Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshDetailPanel();
            return;
        }

        _bin.WriteCell(absAddr + cellIndex * elemBytes, z.ElementSizeBits, z.Format, rawValue);
        UpdateTitleBar();
        RefreshDetailPanel();
    }

    // -----------------------------------------------------------------------
    // Constant value apply
    // -----------------------------------------------------------------------

    private void BtnApplyValue_Click(object? sender, EventArgs e)
    {
        if (_bin == null || _selectedConstant == null) return;
        if (!CanEditValue(_selectedConstant.Format, _selectedConstant.ElementSizeBits))
        {
            MessageBox.Show(
                "This value uses a conversion formula that can't be written back yet.",
                "Write Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!TryParseDisplayValue(txtConstValue.Text, _selectedConstant.Format, out double value))
        {
            MessageBox.Show("Enter a valid number.", "Invalid Input",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!TryConvertDisplayToRaw(value, _selectedConstant.Format, _selectedConstant.ElementSizeBits, out double rawValue))
        {
            MessageBox.Show(
                "This value uses a conversion formula that can't be written back yet.",
                "Write Not Supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshDetailPanel();
            return;
        }

        int absAddr   = _document!.BaseOffset + _selectedConstant.Address;
        _bin.WriteCell(absAddr, _selectedConstant.ElementSizeBits, _selectedConstant.Format, rawValue);
        UpdateTitleBar();
        RefreshDetailPanel();
        SetStatus($"Updated {_selectedConstant.Title} → {FormatDisplayValue(value, _selectedConstant.Format)}");
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
    // 2D / 3D visualization
    // -----------------------------------------------------------------------

    private void LoadVisualizationViews(XdfTable table, double[] values)
    {
        int rows = table.ZAxis!.RowCount;
        int cols = table.ZAxis.ColCount;
        double[]? xVals = TryReadAxisValues(table.XAxis);
        double[]? yVals = TryReadAxisValues(table.YAxis);
        string[] displayValues = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
            displayValues[i] = FormatDisplayValue(values[i], table.ZAxis.Format);

        heatmapView.LoadData(values, rows, cols, xVals, yVals, displayValues);
        surfacePlotView.LoadData(values, rows, cols, xVals, yVals);
    }

    private void BtnResetView3D_Click(object? sender, EventArgs e) => surfacePlotView.ResetView();

    // -----------------------------------------------------------------------
    // Search box — filter tree nodes
    // -----------------------------------------------------------------------

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        if (_document == null) return;

        string search = searchBox.Text.Trim();
        if (search.Length < 2)
        {
            RefreshTree();
            return;
        }

        ApplyTreeSearchFilter(search);
    }

    private void ApplyTreeSearchFilter(string search)
    {
        if (_document == null)
            return;

        object? selectedTag = treeView.SelectedNode?.Tag;
        TreeNode? selectedNode = null;

        treeView.BeginUpdate();
        treeView.Nodes.Clear();

        var tablesNode = new TreeNode("Tables")
        {
            NodeFont = new Font(treeView.Font, FontStyle.Bold)
        };
        foreach (var table in _document.Tables)
        {
            if (!TableMatchesSearch(table, search)) continue;
            var node = new TreeNode(table.Title) { Tag = table };
            tablesNode.Nodes.Add(node);
            if (ReferenceEquals(table, selectedTag))
                selectedNode = node;
        }

        var constantsNode = new TreeNode("Constants")
        {
            NodeFont = new Font(treeView.Font, FontStyle.Bold)
        };
        foreach (var constant in _document.Constants)
        {
            bool match = constant.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                         (constant.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
            if (!match) continue;
            var node = new TreeNode(constant.Title) { Tag = constant };
            constantsNode.Nodes.Add(node);
            if (ReferenceEquals(constant, selectedTag))
                selectedNode = node;
        }

        if (tablesNode.Nodes.Count > 0)
        {
            treeView.Nodes.Add(tablesNode);
            tablesNode.Expand();
        }

        if (constantsNode.Nodes.Count > 0)
        {
            treeView.Nodes.Add(constantsNode);
            constantsNode.Expand();
        }

        treeView.EndUpdate();

        if (selectedNode != null) treeView.SelectedNode = selectedNode;
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

    private static string GetEndianLabel(XdfValueFormat format) =>
        format.IsLittleEndian ? "little-endian" : "big-endian";

    private static bool CanEditValue(XdfValueFormat format, int elementSizeBits)
    {
        if ((format.OutputType ?? 0) == 4)
            return false;

        if (!XdfEquationEvaluator.IsSupported(format.MathEquation))
            return false;

        if (XdfEquationEvaluator.IsIdentity(format.MathEquation))
            return true;

        if (format.IsFloatingPoint)
            return false;

        return elementSizeBits is 8 or 16;
    }

    private static bool TryConvertDisplayToRaw(double displayValue, XdfValueFormat format, int elementSizeBits, out double rawValue)
    {
        if (XdfEquationEvaluator.IsIdentity(format.MathEquation))
        {
            rawValue = displayValue;
            return true;
        }

        if (format.IsFloatingPoint)
        {
            rawValue = 0;
            return false;
        }

        return XdfEquationEvaluator.TryInvertDiscrete(
            format.MathEquation,
            displayValue,
            elementSizeBits,
            format.IsSigned,
            out rawValue);
    }

    private static bool TryParseDisplayValue(string? text, XdfValueFormat format, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();
        if ((format.OutputType ?? 0) == 3)
        {
            string hexText = trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? trimmed[2..]
                : trimmed;

            if (long.TryParse(hexText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue))
            {
                value = hexValue;
                return true;
            }
        }

        return double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands,
                   CultureInfo.CurrentCulture, out value) ||
               double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands,
                   CultureInfo.InvariantCulture, out value);
    }

    private static string FormatDisplayValue(double value, XdfValueFormat format)
    {
        int outputType = format.OutputType ?? 2;
        if (outputType == 3 && Math.Abs(value) <= long.MaxValue)
            return $"0x{(long)Math.Round(value):X}";

        int? decimalPlaces = format.DecimalPlaces;
        if (decimalPlaces.HasValue)
        {
            int places = Math.Clamp(decimalPlaces.Value, 0, 10);
            double rounded = Math.Round(value, places, MidpointRounding.AwayFromZero);
            return rounded.ToString($"F{places}", CultureInfo.CurrentCulture);
        }

        if (outputType == 1)
            return value.ToString("G6", CultureInfo.CurrentCulture);

        if (Math.Abs(value - Math.Round(value)) < 0.000001 && Math.Abs(value) <= long.MaxValue)
            return ((long)Math.Round(value)).ToString(CultureInfo.CurrentCulture);

        return value.ToString("G6", CultureInfo.CurrentCulture);
    }

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
        {
            e.Cancel = true;
            return;
        }

        _isMainFormClosing = true;
        if (_detachedDetailForm != null && !_detachedDetailForm.IsDisposed)
            _detachedDetailForm.Close();

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
        _detachedDetailForm = null;
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
                    btnToolOpenBin.Enabled = true;
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
                    btnToolSave.Enabled = true;
                    statusBinInfo.Text = $"{_bin.Length:N0} bytes";
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
