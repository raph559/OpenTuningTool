using OpenTuningTool.Controls;
using OpenTuningTool.Models;
using OpenTuningTool.Services;

namespace OpenTuningTool.Forms;

public sealed class TableEditorForm : Form
{
    private const int HeaderHeight = 88;
    private const int BodyPadding = 10;
    private const int MinimumEditorWidth = 560;
    private const int MinimumEditorHeight = 380;
    private const int MaximumScreenMargin = 80;
    private const int HeatmapLeftMargin = 60;
    private const int HeatmapTopMargin = 30;
    private const int HeatmapRightMargin = 70;
    private const int HeatmapBottomMargin = 24;
    private const int HeatmapLegendWidth = 20;
    private const int HeatmapLegendGap = 12;
    private const int HeatmapCellWidth = 36;
    private const int HeatmapCellHeight = 20;

    private readonly XdfTable _table;
    private readonly Action<XdfTable> _notifyTableChanged;
    private readonly Label _lblTitle;
    private readonly Label _lblSummary;
    private readonly Label _lblAxes;
    private readonly Label _lblNoBin;
    private readonly FlatTabControl _tabControlView;
    private readonly TabPage _tabText;
    private readonly TabPage _tab2D;
    private readonly TabPage _tab3D;
    private readonly StyledDataGridView _dgvMap;
    private readonly HeatmapView _heatmapView;
    private readonly SurfacePlotView _surfacePlotView;
    private readonly Button _btnResetView3D;

    private XdfDocument _document;
    private BinBuffer? _bin;
    private bool _initialSizeApplied;

    public TableEditorForm(
        XdfDocument document,
        BinBuffer? bin,
        XdfTable table,
        AppTheme theme,
        UiDensity density,
        int initialViewIndex,
        Action<XdfTable> notifyTableChanged)
    {
        _document = document;
        _bin = bin;
        _table = table;
        _notifyTableChanged = notifyTableChanged;

        _lblTitle = new Label();
        _lblSummary = new Label();
        _lblAxes = new Label();
        _lblNoBin = new Label();
        _tabControlView = new FlatTabControl();
        _tabText = new TabPage("Text");
        _tab2D = new TabPage("2D");
        _tab3D = new TabPage("3D");
        _dgvMap = new StyledDataGridView();
        _heatmapView = new HeatmapView();
        _surfacePlotView = new SurfacePlotView();
        _btnResetView3D = new Button();

        InitializeComponent();
        ApplyAppearance(theme, density);
        _tabControlView.SelectedIndex = Math.Clamp(initialViewIndex, 0, _tabControlView.TabPages.Count - 1);
        RefreshData();
    }

    public XdfTable Table => _table;

    public void ApplyAppearance(AppTheme theme, UiDensity density)
    {
        ThemeUtility.ApplyTheme(this, theme);
        ThemeUtility.ApplyUiDensity(this, density);
    }

    public void UpdateDataSource(XdfDocument document, BinBuffer? bin)
    {
        _document = document;
        _bin = bin;
    }

    public void RefreshData()
    {
        Text = $"Table Editor - {_table.Title}";
        _lblTitle.Text = _table.Title;
        _lblSummary.Text = BuildSummaryText();
        _lblAxes.Text = BuildAxisSummaryText();

        if (!TableEditorSupport.TryReadTableValues(_document, _bin, _table, out double[] values, out string message))
        {
            _lblNoBin.Text = message;
            _lblNoBin.Visible = true;
            _tabControlView.Visible = false;
            return;
        }

        TableEditorSupport.PopulateMapGrid(_dgvMap, _document, _bin, _table, values);
        TableEditorSupport.LoadVisualizationViews(_heatmapView, _surfacePlotView, _document, _bin, _table, values);
        _lblNoBin.Visible = false;
        _tabControlView.Visible = true;

        if (!_initialSizeApplied)
        {
            ApplyPreferredInitialSize();
            _initialSizeApplied = true;
        }
    }

    private void InitializeComponent()
    {
        ThemeUtility.ThemePalette palette = ThemeUtility.GetPaletteFor(AppTheme.Dark);

        SuspendLayout();

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(14, 12, 14, 10),
            BackColor = palette.Surface,
        };

        _lblTitle.Dock = DockStyle.Top;
        _lblTitle.AutoSize = false;
        _lblTitle.Height = 30;
        _lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        _lblTitle.ForeColor = palette.Accent;

        _lblSummary.Dock = DockStyle.Top;
        _lblSummary.AutoSize = false;
        _lblSummary.Height = 22;
        _lblSummary.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        _lblSummary.ForeColor = palette.Foreground;

        _lblAxes.Dock = DockStyle.Fill;
        _lblAxes.Font = new Font("Consolas", 9F, FontStyle.Regular);
        _lblAxes.ForeColor = palette.MutedForeground;

        headerPanel.Controls.Add(_lblAxes);
        headerPanel.Controls.Add(_lblSummary);
        headerPanel.Controls.Add(_lblTitle);

        var bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = palette.Window,
        };

        _lblNoBin.Dock = DockStyle.Fill;
        _lblNoBin.TextAlign = ContentAlignment.MiddleCenter;
        _lblNoBin.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
        _lblNoBin.ForeColor = palette.Foreground;

        _tabControlView.Dock = DockStyle.Fill;
        _tabControlView.TabPages.AddRange(new[] { _tabText, _tab2D, _tab3D });

        _dgvMap.Dock = DockStyle.Fill;
        _dgvMap.AllowUserToAddRows = false;
        _dgvMap.AllowUserToDeleteRows = false;
        _dgvMap.AllowUserToResizeRows = false;
        _dgvMap.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _dgvMap.RowHeadersWidth = 60;
        _dgvMap.CellEndEdit += DgvMap_CellEndEdit;
        _tabText.Controls.Add(_dgvMap);

        _heatmapView.Dock = DockStyle.Fill;
        _tab2D.Controls.Add(_heatmapView);

        _surfacePlotView.Dock = DockStyle.Fill;
        _btnResetView3D.Text = "Reset View";
        _btnResetView3D.Size = new Size(86, 26);
        _btnResetView3D.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnResetView3D.FlatStyle = FlatStyle.Flat;
        _btnResetView3D.FlatAppearance.BorderSize = 1;
        _btnResetView3D.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 74);
        _btnResetView3D.BackColor = Color.FromArgb(50, palette.Surface.R, palette.Surface.G, palette.Surface.B);
        _btnResetView3D.ForeColor = palette.Foreground;
        _btnResetView3D.Font = new Font("Segoe UI", 7.5f);
        _btnResetView3D.Cursor = Cursors.Hand;
        _btnResetView3D.Location = new Point(_tab3D.Width - 96, 6);
        _btnResetView3D.Click += BtnResetView3D_Click;
        _tab3D.Controls.Add(_btnResetView3D);
        _tab3D.Controls.Add(_surfacePlotView);
        _btnResetView3D.BringToFront();

        foreach (TabPage tabPage in _tabControlView.TabPages)
            tabPage.BackColor = palette.Window;

        bodyPanel.Controls.Add(_lblNoBin);
        bodyPanel.Controls.Add(_tabControlView);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = palette.Window;
        ClientSize = new Size(MinimumEditorWidth, MinimumEditorHeight);
        MinimumSize = new Size(MinimumEditorWidth, MinimumEditorHeight);
        Controls.Add(bodyPanel);
        Controls.Add(headerPanel);
        StartPosition = FormStartPosition.Manual;

        ResumeLayout(false);
    }

    private void DgvMap_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_bin == null || _table.ZAxis == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        XdfTableData z = _table.ZAxis;
        if (!TableEditorSupport.CanEditValue(z.Format, z.ElementSizeBits))
        {
            RefreshData();
            return;
        }

        DataGridViewCell cell = _dgvMap.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (!TableEditorSupport.TryParseDisplayValue(cell.Value?.ToString(), z.Format, out double displayValue))
        {
            MessageBox.Show(
                "Enter a valid value.",
                "Invalid Input",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            RefreshData();
            return;
        }

        if (!TableEditorSupport.TryConvertDisplayToRaw(displayValue, z.Format, z.ElementSizeBits, out double rawValue))
        {
            MessageBox.Show(
                "This table uses a conversion formula that can't be written back yet.",
                "Write Not Supported",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshData();
            return;
        }

        int absAddr = _document.BaseOffset + z.Address;
        int elemBytes = z.ElementSizeBits / 8;
        int cellIndex = e.RowIndex * z.ColCount + e.ColumnIndex;
        _bin.WriteCell(absAddr + cellIndex * elemBytes, z.ElementSizeBits, z.Format, rawValue);

        RefreshData();
        _notifyTableChanged(_table);
    }

    private void BtnResetView3D_Click(object? sender, EventArgs e) => _surfacePlotView.ResetView();

    private string BuildSummaryText()
    {
        if (_table.ZAxis == null)
            return "No table data is defined for this entry.";

        int absAddr = _document.BaseOffset + _table.ZAxis.Address;
        return
            $"{_table.ZAxis.RowCount}x{_table.ZAxis.ColCount} @ 0x{absAddr:X}  [{_table.ZAxis.ElementSizeBits}-bit, {TableEditorSupport.GetEndianLabel(_table.ZAxis.Format)}]";
    }

    private string BuildAxisSummaryText()
    {
        string xText = _table.XAxis != null
            ? $"X: {_table.XAxis.IndexCount} @ {FormatAxisAddress(_table.XAxis)}"
            : "X: none";
        string yText = _table.YAxis != null
            ? $"Y: {_table.YAxis.IndexCount} @ {FormatAxisAddress(_table.YAxis)}"
            : "Y: none";
        return $"{xText}    {yText}";
    }

    private string FormatAxisAddress(XdfAxis axis) =>
        axis.Address.HasValue ? $"0x{_document.BaseOffset + axis.Address.Value:X}" : "inline";

    private void ApplyPreferredInitialSize()
    {
        Size desiredClientSize = CalculatePreferredClientSize();
        Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;

        int maxWidth = Math.Max(MinimumEditorWidth, workingArea.Width - MaximumScreenMargin);
        int maxHeight = Math.Max(MinimumEditorHeight, workingArea.Height - MaximumScreenMargin);

        ClientSize = new Size(
            Math.Clamp(desiredClientSize.Width, MinimumEditorWidth, maxWidth),
            Math.Clamp(desiredClientSize.Height, MinimumEditorHeight, maxHeight));
    }

    private Size CalculatePreferredClientSize()
    {
        Size gridSize = CalculateGridViewportSize();
        Size heatmapSize = CalculateHeatmapViewportSize();

        int bodyWidth = Math.Max(gridSize.Width, heatmapSize.Width) + (BodyPadding * 2);
        int bodyHeight = Math.Max(gridSize.Height, heatmapSize.Height) + (BodyPadding * 2);

        return new Size(
            Math.Max(MinimumEditorWidth, bodyWidth),
            Math.Max(MinimumEditorHeight, HeaderHeight + bodyHeight));
    }

    private Size CalculateGridViewportSize()
    {
        int width = _dgvMap.RowHeadersVisible ? _dgvMap.RowHeadersWidth : 0;
        foreach (DataGridViewColumn column in _dgvMap.Columns)
        {
            if (column.Visible)
                width += column.Width;
        }

        width += SystemInformation.VerticalScrollBarWidth + 6;

        int height = _dgvMap.ColumnHeadersVisible ? _dgvMap.ColumnHeadersHeight : 0;
        foreach (DataGridViewRow row in _dgvMap.Rows)
        {
            if (!row.IsNewRow && row.Visible)
                height += row.Height;
        }

        height += SystemInformation.HorizontalScrollBarHeight + 6;
        return new Size(width, height);
    }

    private Size CalculateHeatmapViewportSize()
    {
        if (_table.ZAxis == null)
            return Size.Empty;

        int width =
            HeatmapLeftMargin +
            (_table.ZAxis.ColCount * HeatmapCellWidth) +
            HeatmapLegendGap +
            HeatmapLegendWidth +
            HeatmapRightMargin;
        int height =
            HeatmapTopMargin +
            (_table.ZAxis.RowCount * HeatmapCellHeight) +
            HeatmapBottomMargin;

        return new Size(width, height);
    }
}
