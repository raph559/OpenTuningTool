using OpenTuningTool.Controls;
using OpenTuningTool.Models;

namespace OpenTuningTool.Forms;

public sealed class TableSearchForm : Form
{
    private readonly Func<string, IReadOnlyList<XdfTable>> _searchTables;
    private readonly Action<XdfTable> _activateTable;

    private readonly Label _lblSearch;
    private readonly TextBox _txtSearch;
    private readonly StyledDataGridView _gridResults;
    private readonly DataGridViewTextBoxColumn _colTitle;
    private readonly DataGridViewTextBoxColumn _colDescription;
    private readonly Label _lblCount;
    private readonly Button _btnOpen;
    private readonly Button _btnClose;

    public TableSearchForm(
        Func<string, IReadOnlyList<XdfTable>> searchTables,
        Action<XdfTable> activateTable,
        UiDensity uiDensity = UiDensity.Comfortable,
        AppTheme theme = AppTheme.Dark)
    {
        _searchTables = searchTables;
        _activateTable = activateTable;

        _lblSearch = new Label();
        _txtSearch = new TextBox();
        _gridResults = new StyledDataGridView();
        _colTitle = new DataGridViewTextBoxColumn();
        _colDescription = new DataGridViewTextBoxColumn();
        _lblCount = new Label();
        _btnOpen = new Button();
        _btnClose = new Button();

        InitializeComponent();
        ThemeUtility.ApplyTheme(this, theme);
        ThemeUtility.ApplyUiDensity(this, uiDensity);
        RefreshResults();
    }

    public void FocusSearchBox()
    {
        _txtSearch.Focus();
        _txtSearch.SelectAll();
    }

    public void RefreshResults()
    {
        string searchText = _txtSearch.Text.Trim();
        XdfTable? selectedTable = GetSelectedTable();
        IReadOnlyList<XdfTable> results = _searchTables(searchText);

        _gridResults.SuspendLayout();
        _gridResults.Rows.Clear();

        foreach (XdfTable table in results)
        {
            int rowIndex = _gridResults.Rows.Add(table.Title, table.Description ?? string.Empty);
            _gridResults.Rows[rowIndex].Tag = table;
        }

        if (_gridResults.Rows.Count > 0)
        {
            DataGridViewRow? preferredRow = null;
            if (selectedTable != null)
            {
                foreach (DataGridViewRow row in _gridResults.Rows)
                {
                    if (!ReferenceEquals(row.Tag, selectedTable)) continue;
                    preferredRow = row;
                    break;
                }
            }

            preferredRow ??= _gridResults.Rows[0];
            _gridResults.ClearSelection();
            preferredRow.Selected = true;
            _gridResults.CurrentCell = preferredRow.Cells[0];
            _gridResults.FirstDisplayedScrollingRowIndex = preferredRow.Index;
        }
        else
        {
            _gridResults.ClearSelection();
        }

        _gridResults.ResumeLayout();

        int count = results.Count;
        _lblCount.Text = searchText.Length == 0
            ? "Type to search tables."
            : count == 0
            ? "No matching tables."
            : $"{count} matching table{(count == 1 ? string.Empty : "s")}.";
        _btnOpen.Enabled = _gridResults.SelectedRows.Count > 0;
    }

    private void InitializeComponent()
    {
        // Colors for modern dark theme
        var bgDark = Color.FromArgb(30, 30, 30);
        var bgPanel = Color.FromArgb(37, 37, 38);
        var bgControl = Color.FromArgb(45, 45, 48);
        var fgLight = Color.FromArgb(220, 220, 220);
        var accent = Color.FromArgb(0, 122, 204);

        SuspendLayout();

        _lblSearch.AutoSize = true;
        _lblSearch.Location = new Point(15, 17);
        _lblSearch.Text = "Find:";
        _lblSearch.ForeColor = fgLight;
        _lblSearch.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

        _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _txtSearch.Location = new Point(60, 14);
        _txtSearch.Size = new Size(448, 25);
        _txtSearch.PlaceholderText = "Search table name or description...";
        _txtSearch.BackColor = bgControl;
        _txtSearch.ForeColor = fgLight;
        _txtSearch.BorderStyle = BorderStyle.FixedSingle;
        _txtSearch.Font = new Font("Segoe UI", 9.5F);
        _txtSearch.TextChanged += TxtSearch_TextChanged;
        _txtSearch.KeyDown += TxtSearch_KeyDown;

        _gridResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _gridResults.Location = new Point(15, 50);
        _gridResults.Size = new Size(590, 210);
        _gridResults.AllowUserToAddRows = false;
        _gridResults.AllowUserToDeleteRows = false;
        _gridResults.AllowUserToResizeColumns = false;
        _gridResults.AllowUserToResizeRows = false;
        _gridResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridResults.BackgroundColor = bgDark;
        _gridResults.BorderStyle = BorderStyle.None;
        _gridResults.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _gridResults.EditMode = DataGridViewEditMode.EditProgrammatically;
        _gridResults.MultiSelect = false;
        _gridResults.ReadOnly = true;
        _gridResults.RowHeadersVisible = false;
        _gridResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridResults.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _gridResults.Font = new Font("Segoe UI", 9.5F);
        _gridResults.Columns.AddRange(new DataGridViewColumn[] { _colTitle, _colDescription });
        _gridResults.ApplyThemeColors(bgDark, bgPanel, bgControl, fgLight, accent, Color.FromArgb(60, 60, 60));
        _gridResults.SelectionChanged += ListResults_SelectedIndexChanged;
        _gridResults.CellDoubleClick += GridResults_CellDoubleClick;

        _colTitle.Name = "Table";
        _colTitle.HeaderText = "Table";
        _colTitle.FillWeight = 36;
        _colTitle.MinimumWidth = 160;
        _colTitle.SortMode = DataGridViewColumnSortMode.NotSortable;
        _colDescription.Name = "Description";
        _colDescription.HeaderText = "Description";
        _colDescription.FillWeight = 64;
        _colDescription.MinimumWidth = 250;
        _colDescription.SortMode = DataGridViewColumnSortMode.NotSortable;

        _lblCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _lblCount.AutoSize = true;
        _lblCount.Location = new Point(15, 274);
        _lblCount.Text = "Type to search tables.";
        _lblCount.ForeColor = Color.DarkGray;
        _lblCount.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

        _btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOpen.Enabled = false;
        _btnOpen.Location = new Point(435, 269);
        _btnOpen.Size = new Size(82, 30);
        _btnOpen.Text = "Open";
        _btnOpen.FlatStyle = FlatStyle.Flat;
        _btnOpen.FlatAppearance.BorderSize = 0;
        _btnOpen.BackColor = accent;
        _btnOpen.ForeColor = Color.White;
        _btnOpen.Cursor = Cursors.Hand;
        _btnOpen.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnOpen.Click += BtnOpen_Click;

        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnClose.Location = new Point(523, 269);
        _btnClose.Size = new Size(82, 30);
        _btnClose.Text = "Close";
        _btnClose.FlatStyle = FlatStyle.Flat;
        _btnClose.FlatAppearance.BorderSize = 0;
        _btnClose.BackColor = bgControl;
        _btnClose.ForeColor = fgLight;
        _btnClose.Cursor = Cursors.Hand;
        _btnClose.Click += BtnClose_Click;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 312);
        MinimumSize = new Size(500, 260);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Table Search";
        BackColor = bgDark;
        ForeColor = fgLight;
        Controls.AddRange(new Control[]
        {
            _lblSearch,
            _txtSearch,
            _gridResults,
            _lblCount,
            _btnOpen,
            _btnClose
        });
        AcceptButton = _btnOpen;
        CancelButton = _btnClose;

        ResumeLayout(false);
        PerformLayout();
    }

    private void TxtSearch_TextChanged(object? sender, EventArgs e) => RefreshResults();

    private void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        e.Handled = true;
        e.SuppressKeyPress = true;
        ActivateSelectedTable();
    }

    private void ListResults_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _btnOpen.Enabled = _gridResults.SelectedRows.Count > 0;
    }

    private void GridResults_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
            ActivateSelectedTable();
    }

    private void BtnOpen_Click(object? sender, EventArgs e) => ActivateSelectedTable();

    private void BtnClose_Click(object? sender, EventArgs e) => Close();

    private void ActivateSelectedTable()
    {
        XdfTable? table = GetSelectedTable();
        if (table == null) return;

        _activateTable(table);
        Close();
    }

    private XdfTable? GetSelectedTable()
    {
        if (_gridResults.SelectedRows.Count > 0)
            return _gridResults.SelectedRows[0].Tag as XdfTable;

        return _gridResults.CurrentRow?.Tag as XdfTable;
    }
}
