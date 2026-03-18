using OpenTuningTool.Models;

namespace OpenTuningTool.Forms;

public sealed class TableSearchForm : Form
{
    private readonly Func<string, IReadOnlyList<XdfTable>> _searchTables;
    private readonly Action<XdfTable> _activateTable;

    private readonly Label _lblSearch;
    private readonly TextBox _txtSearch;
    private readonly ListView _listResults;
    private readonly ColumnHeader _colTitle;
    private readonly ColumnHeader _colDescription;
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
        _listResults = new ListView();
        _colTitle = new ColumnHeader();
        _colDescription = new ColumnHeader();
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

        _listResults.BeginUpdate();
        _listResults.Items.Clear();

        foreach (XdfTable table in results)
        {
            var item = new ListViewItem(table.Title) { Tag = table };
            item.SubItems.Add(table.Description ?? string.Empty);
            _listResults.Items.Add(item);
        }

        if (_listResults.Items.Count > 0)
        {
            ListViewItem? preferredItem = null;
            if (selectedTable != null)
            {
                foreach (ListViewItem item in _listResults.Items)
                {
                    if (!ReferenceEquals(item.Tag, selectedTable)) continue;
                    preferredItem = item;
                    break;
                }
            }

            preferredItem ??= _listResults.Items[0];
            preferredItem.Selected = true;
            preferredItem.Focused = true;
            preferredItem.EnsureVisible();
        }

        _listResults.EndUpdate();

        int count = results.Count;
        _lblCount.Text = searchText.Length == 0
            ? "Type to search tables."
            : count == 0
            ? "No matching tables."
            : $"{count} matching table{(count == 1 ? string.Empty : "s")}.";
        _btnOpen.Enabled = _listResults.SelectedItems.Count > 0;
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

        _listResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _listResults.Location = new Point(15, 50);
        _listResults.Size = new Size(590, 210);
        _listResults.FullRowSelect = true;
        _listResults.GridLines = false;
        _listResults.HideSelection = false;
        _listResults.MultiSelect = false;
        _listResults.View = View.Details;
        _listResults.BackColor = bgPanel;
        _listResults.ForeColor = fgLight;
        _listResults.BorderStyle = BorderStyle.None;
        _listResults.Font = new Font("Segoe UI", 9.5F);
        _listResults.Columns.AddRange(new[] { _colTitle, _colDescription });
        _listResults.SelectedIndexChanged += ListResults_SelectedIndexChanged;
        _listResults.DoubleClick += ListResults_DoubleClick;

        _colTitle.Text = "Table";
        _colTitle.Width = 200;
        _colDescription.Text = "Description";
        _colDescription.Width = 380;

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
            _listResults,
            _lblCount,
            _btnOpen,
            _btnClose
        });

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
        _btnOpen.Enabled = _listResults.SelectedItems.Count > 0;
    }

    private void ListResults_DoubleClick(object? sender, EventArgs e) => ActivateSelectedTable();

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
        if (_listResults.SelectedItems.Count == 0) return null;
        return _listResults.SelectedItems[0].Tag as XdfTable;
    }
}
