using OpenTuningTool.Controls;
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
    private readonly Dictionary<int, TableEditorForm> _tableEditorForms = [];
    private readonly BinEditHistory _editHistory = new();
    private readonly CalibrAiClient _calibrAi = new();

    public Form1()
    {
        _settings = AppSettingsStore.Load();
        InitializeComponent();
        heatmapView.CellSelected    += HeatmapView_CellSelected;
        heatmapView.CellActivated   += HeatmapView_CellActivated;
        heatmapView.SelectionChanged += HeatmapView_SelectionChanged;
        surfacePlotView.PointSelected    += SurfacePlotView_PointSelected;
        surfacePlotView.PointActivated   += SurfacePlotView_PointActivated;
        surfacePlotView.SelectionChanged += SurfacePlotView_SelectionChanged;
        surfacePlotView.PointsValueChanged += SurfacePlotView_PointsValueChanged;
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

        foreach (KeyValuePair<int, TableEditorForm> entry in _tableEditorForms.ToArray())
        {
            TableEditorForm editor = entry.Value;
            if (editor.IsDisposed)
                continue;

            editor.ApplyAppearance(_settings.Theme, _settings.UiDensity);
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
            _editHistory.Clear();
            CloseAllTableEditorWindows();
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
            _editHistory.Clear();
            _settings.LastBinPath = dlg.FileName;
            SaveSettingsQuietly();
            menuItemSaveBin.Enabled = true;
            menuItemSaveBinAs.Enabled = true;
            btnToolSave.Enabled = true;
            UpdateTitleBar();
            statusBinInfo.Text = $"{_bin.Length:N0} bytes";
            SetStatus($"BIN loaded: {Path.GetFileName(dlg.FileName)}  ({_bin.Length:N0} bytes)");

            RefreshOpenTableEditorWindows();

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

    private void BtnDetachDetail_Click(object? sender, EventArgs e)
    {
        OpenSelectedTableEditorWindow();
    }

    private void BtnReattachDetail_Click(object? sender, EventArgs e) => OpenSelectedTableEditorWindow();

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
        bool canOpenTableWindow = _selectedTable?.ZAxis != null && _document != null && _bin != null;
        btnDetachDetail.Text = "New Window";
        btnDetachDetail.Enabled = canOpenTableWindow;
        btnReattachDetail.Visible = false;
        panelDetachedPlaceholder.Visible = false;
    }

    private void UpdateDetachedDetailWindowTitle()
    {
        if (_detachedDetailForm == null || _detachedDetailForm.IsDisposed)
            return;

        string detailName = _selectedTable?.Title ?? _selectedConstant?.Title ?? "Data View";
        _detachedDetailForm.Text = $"Data View — {detailName}";
    }

    private void OpenSelectedTableEditorWindow()
    {
        if (_selectedTable?.ZAxis == null || _document == null || _bin == null)
            return;

        if (_tableEditorForms.TryGetValue(_selectedTable.UniqueId, out TableEditorForm? existingEditor))
        {
            if (!existingEditor.IsDisposed)
            {
                existingEditor.UpdateDataSource(_document, _bin);
                existingEditor.RefreshData();
                if (!existingEditor.Visible)
                    existingEditor.Show(this);

                existingEditor.BringToFront();
                existingEditor.Activate();
                return;
            }

            _tableEditorForms.Remove(_selectedTable.UniqueId);
        }

        var editor = new TableEditorForm(
            _document,
            _bin,
            _selectedTable,
            _settings.Theme,
            _settings.UiDensity,
            GetDefaultTableViewIndex(),
            HandleTableDataChanged);

        PositionTableEditorWindow(editor);
        editor.FormClosed += TableEditorForm_FormClosed;
        _tableEditorForms[_selectedTable.UniqueId] = editor;
        editor.Show(this);
        editor.BringToFront();
        editor.Activate();
    }

    private void PositionTableEditorWindow(Form editor)
    {
        Rectangle ownerBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        Rectangle workingArea = Screen.FromRectangle(ownerBounds).WorkingArea;
        int cascadeOffset = 28 * (_tableEditorForms.Count % 8);
        editor.StartPosition = FormStartPosition.Manual;

        int desiredX = ownerBounds.Left + 40 + cascadeOffset;
        int desiredY = ownerBounds.Top + 40 + cascadeOffset;
        int maxX = Math.Max(workingArea.Left, workingArea.Right - editor.Width);
        int maxY = Math.Max(workingArea.Top, workingArea.Bottom - editor.Height);

        editor.Location = new Point(
            Math.Clamp(desiredX, workingArea.Left, maxX),
            Math.Clamp(desiredY, workingArea.Top, maxY));
    }

    private void TableEditorForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is not TableEditorForm editor)
            return;

        _tableEditorForms.Remove(editor.Table.UniqueId);
    }

    private void RefreshOpenTableEditorWindows()
    {
        if (_document == null)
            return;

        foreach (KeyValuePair<int, TableEditorForm> entry in _tableEditorForms.ToArray())
        {
            int uniqueId = entry.Key;
            TableEditorForm editor = entry.Value;
            if (editor.IsDisposed)
            {
                _tableEditorForms.Remove(uniqueId);
                continue;
            }

            editor.UpdateDataSource(_document, _bin);
            editor.RefreshData();
        }
    }

    private void CloseAllTableEditorWindows()
    {
        foreach (KeyValuePair<int, TableEditorForm> entry in _tableEditorForms.ToArray())
        {
            int uniqueId = entry.Key;
            TableEditorForm editor = entry.Value;
            editor.FormClosed -= TableEditorForm_FormClosed;
            if (!editor.IsDisposed)
                editor.Close();

            _tableEditorForms.Remove(uniqueId);
        }
    }

    private void HandleTableDataChanged(XdfTable? table)
    {
        if (table == null)
            return;

        UpdateTitleBar();
        if (_selectedTable != null || _selectedConstant != null)
            RefreshDetailPanel();

        RefreshOpenTableEditorWindows();
        SetStatus($"Updated {table.Title}");
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
        UpdateDetailDetachUi();

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
        UpdateDetailDetachUi();

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
        UpdateDetailDetachUi();
    }

    // -----------------------------------------------------------------------
    // Map DataGridView helpers
    // -----------------------------------------------------------------------

    private void LoadMapIntoGrid(XdfTable table, double[] values)
    {
        TableEditorSupport.PopulateMapGrid(dgvMap, _document, _bin, table, values);
    }

    private void DgvMap_Resize(object? sender, EventArgs e)
    {
        if (dgvMap.Columns.Count > 0)
            TableEditorSupport.FitMapGridToViewport(dgvMap);
    }

    private void HeatmapView_CellSelected(object? sender, HeatmapCellEventArgs e) => SelectMapCell(e.Row, e.Col);

    private void HeatmapView_CellActivated(object? sender, HeatmapCellEventArgs e) => EditSelectedTableCell(e.Row, e.Col, e.DisplayValue);

    private void SurfacePlotView_PointSelected(object? sender, SurfacePointEventArgs e) => SelectMapCell(e.Row, e.Col);

    private void SurfacePlotView_PointActivated(object? sender, SurfacePointEventArgs e) =>
        EditSelectedTableCell(e.Row, e.Col, GetGridCellDisplayValue(e.Row, e.Col));

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
        if (_bin == null || _selectedTable == null || e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var cell = dgvMap.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!TableEditorSupport.TryWriteTableCellValue(
                _document!,
                _bin,
                _selectedTable,
                e.RowIndex,
                e.ColumnIndex,
                cell.Value?.ToString(),
                out BinCellEdit? appliedEdit,
                out string errorMessage))
        {
            MessageBox.Show(errorMessage, "Update Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshDetailPanel();
            return;
        }

        if (appliedEdit is { } edit)
            _editHistory.Record(edit);

        HandleTableDataChanged(_selectedTable);
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
        double previousRawValue = _bin.ReadMap(
            absAddr,
            1,
            1,
            _selectedConstant.ElementSizeBits,
            _selectedConstant.Format.TypeFlags)[0];
        _bin.WriteCell(absAddr, _selectedConstant.ElementSizeBits, _selectedConstant.Format, rawValue);
        _editHistory.Record(new BinCellEdit(
            absAddr,
            _selectedConstant.ElementSizeBits,
            _selectedConstant.Format.TypeFlags,
            previousRawValue,
            rawValue,
            $"Constant {_selectedConstant.Title}"));
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
    // Cross-view selection sync
    // -----------------------------------------------------------------------

    private bool _syncingSelection;

    private void HeatmapView_SelectionChanged(IReadOnlyCollection<(int row, int col)> cells)
    {
        if (_syncingSelection || _selectedTable?.ZAxis == null) return;
        _syncingSelection = true;
        try
        {
            int cols = _selectedTable.ZAxis.ColCount;
            surfacePlotView.SetSelectedIndices(cells.Select(c => c.row * cols + c.col));

            dgvMap.ClearSelection();
            foreach (var (r, c) in cells)
            {
                if (r < dgvMap.Rows.Count && c < dgvMap.Columns.Count)
                    dgvMap.Rows[r].Cells[c].Selected = true;
            }
        }
        finally { _syncingSelection = false; }
    }

    private void SurfacePlotView_SelectionChanged(IReadOnlyCollection<int> indices)
    {
        if (_syncingSelection || _selectedTable?.ZAxis == null) return;
        _syncingSelection = true;
        try
        {
            int cols = _selectedTable.ZAxis.ColCount;
            heatmapView.SetSelectedCells(indices.Select(i => (row: i / cols, col: i % cols)));

            dgvMap.ClearSelection();
            foreach (int i in indices)
            {
                int r = i / cols, c = i % cols;
                if (r < dgvMap.Rows.Count && c < dgvMap.Columns.Count)
                    dgvMap.Rows[r].Cells[c].Selected = true;
            }
        }
        finally { _syncingSelection = false; }
    }

    // -----------------------------------------------------------------------
    // 3D drag value editing — write back to BIN
    // -----------------------------------------------------------------------

    private void SurfacePlotView_PointsValueChanged(int[] indices, double[] newValues)
    {
        if (_bin == null || _selectedTable?.ZAxis == null || _document == null) return;

        var z         = _selectedTable.ZAxis;
        int absAddr   = _document.BaseOffset + z.Address;
        int elemBytes = z.ElementSizeBits / 8;

        // Read old values before writing (for undo history)
        double[] oldValues = new double[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            int offset = absAddr + indices[i] * elemBytes;
            oldValues[i] = _bin.ReadMap(offset, 1, 1, z.ElementSizeBits, z.Format)[0];
        }

        // Write new values and record in undo history
        for (int i = 0; i < indices.Length; i++)
        {
            int offset = absAddr + indices[i] * elemBytes;
            _bin.WriteCell(offset, z.ElementSizeBits, z.Format, newValues[i]);
            double committed = _bin.ReadMap(offset, 1, 1, z.ElementSizeBits, z.Format)[0];
            _editHistory.Record(new BinCellEdit(
                offset, z.ElementSizeBits, z.Format.TypeFlags,
                PreviousRawValue: oldValues[i],
                NewRawValue:      committed,
                Label: $"3D-drag [{indices[i] / z.ColCount},{indices[i] % z.ColCount}]"));
        }

        // Re-read full map to update views
        double[] allValues = _bin.ReadMap(absAddr, z.RowCount, z.ColCount, z.ElementSizeBits, z.Format);
        double[]? xVals = TryReadAxisValues(_selectedTable.XAxis);
        double[]? yVals = TryReadAxisValues(_selectedTable.YAxis);

        string[] displayValues = new string[allValues.Length];
        for (int i = 0; i < allValues.Length; i++)
            displayValues[i] = FormatDisplayValue(allValues[i], z.Format);

        var savedHeatmapSel = heatmapView.SelectedCells.ToHashSet();
        heatmapView.LoadData(allValues, z.RowCount, z.ColCount, xVals, yVals, displayValues);
        heatmapView.SetSelectedCells(savedHeatmapSel);

        // Update DGV cells
        for (int i = 0; i < indices.Length; i++)
        {
            int r = indices[i] / z.ColCount, c = indices[i] % z.ColCount;
            if (r < dgvMap.Rows.Count && c < dgvMap.Columns.Count)
                dgvMap.Rows[r].Cells[c].Value = displayValues[indices[i]];
        }
        dgvMap.Invalidate();

        UpdateTitleBar();
    }

    private void SelectMapCell(int row, int col)
    {
        if (row < 0 || col < 0 || row >= dgvMap.Rows.Count || col >= dgvMap.Columns.Count)
            return;

        dgvMap.ClearSelection();
        dgvMap.CurrentCell = dgvMap.Rows[row].Cells[col];
        dgvMap.Rows[row].Cells[col].Selected = true;
    }

    private void EditSelectedTableCell(int row, int col, string initialValue)
    {
        if (_bin == null || _selectedTable == null || _document == null)
            return;

        SelectMapCell(row, col);

        using var dialog = new ValueEditDialog(
            "Edit Table Value",
            $"Cell [{row}, {col}]",
            string.IsNullOrWhiteSpace(initialValue) ? GetGridCellDisplayValue(row, col) : initialValue,
            _settings.Theme,
            _settings.UiDensity);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (!TableEditorSupport.TryWriteTableCellValue(
                _document,
                _bin,
                _selectedTable,
                row,
                col,
                dialog.ValueText,
                out BinCellEdit? appliedEdit,
                out string errorMessage))
        {
            MessageBox.Show(errorMessage, "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshDetailPanel();
            return;
        }

        if (appliedEdit is { } edit)
            _editHistory.Record(edit);

        HandleTableDataChanged(_selectedTable);
    }

    private string GetGridCellDisplayValue(int row, int col)
    {
        if (row < 0 || col < 0 || row >= dgvMap.Rows.Count || col >= dgvMap.Columns.Count)
            return string.Empty;

        return Convert.ToString(dgvMap.Rows[row].Cells[col].FormattedValue ?? dgvMap.Rows[row].Cells[col].Value) ?? string.Empty;
    }

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

    private bool TryUndoBinEdit()
    {
        if (!_editHistory.TryUndo(_bin, out BinCellEdit edit))
            return false;

        UpdateTitleBar();
        if (_selectedTable != null || _selectedConstant != null)
            RefreshDetailPanel();

        RefreshOpenTableEditorWindows();
        SetStatus($"Undo: {edit.Label}");
        return true;
    }

    private bool TryRedoBinEdit()
    {
        if (!_editHistory.TryRedo(_bin, out BinCellEdit edit))
            return false;

        UpdateTitleBar();
        if (_selectedTable != null || _selectedConstant != null)
            RefreshDetailPanel();

        RefreshOpenTableEditorWindows();
        SetStatus($"Redo: {edit.Label}");
        return true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            OpenTableSearchWindow();
            return true;
        }

        if (keyData == (Keys.Control | Keys.Z))
        {
            if (!KeyboardShortcutSupport.IsTextInputControlFocused(this) && TryUndoBinEdit())
                return true;
        }

        if (keyData == (Keys.Control | Keys.Y))
        {
            if (!KeyboardShortcutSupport.IsTextInputControlFocused(this) && TryRedoBinEdit())
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
        CloseAllTableEditorWindows();
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
