using OpenTuningTool.Models;
using OpenTuningTool.Services;

namespace OpenTuningTool.Forms;

/// <summary>
/// Shows CalibrAI detection results and lets the user accept candidates
/// to import into the current XDF document.
/// </summary>
public partial class DetectResultsForm : Form
{
    private readonly List<MapCandidateResult> _candidates;

    public IReadOnlyList<MapCandidateResult> SelectedCandidates { get; private set; }
        = new List<MapCandidateResult>();

    public DetectResultsForm(
        List<MapCandidateResult> candidates,
        UiDensity uiDensity = UiDensity.Comfortable,
        AppTheme theme = AppTheme.Dark)
    {
        _candidates = candidates;
        InitializeComponent();
        ThemeUtility.ApplyTheme(this, theme);
        ThemeUtility.ApplyUiDensity(this, uiDensity);
        PopulateGrid();
    }

    private void PopulateGrid()
    {
        foreach (var c in _candidates)
        {
            int rowIdx = dataGridView.Rows.Add(
                true,               // ✓ selected
                c.AddressHex,
                c.ByteSize,
                c.Rows,
                c.Cols,
                c.ElementSizeBits,
                c.Endian,
                c.Confidence.ToString("F3"));
            dataGridView.Rows[rowIdx].Tag = c;
        }

        lblCount.Text = $"{_candidates.Count} map candidate(s) detected.";
    }

    private void BtnAcceptSelected_Click(object sender, EventArgs e)
    {
        var selected = new List<MapCandidateResult>();
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (row.Tag is MapCandidateResult candidate &&
                row.Cells[0] is DataGridViewCheckBoxCell cb &&
                cb.Value is true)
            {
                selected.Add(candidate);
            }
        }

        if (selected.Count == 0)
        {
            MessageBox.Show(
                "No candidates selected. Check at least one row.",
                "Nothing Selected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SelectedCandidates = selected;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnAcceptAll_Click(object sender, EventArgs e)
    {
        SelectedCandidates = _candidates;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void BtnSelectAll_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dataGridView.Rows)
            if (row.Cells[0] is DataGridViewCheckBoxCell cb)
                cb.Value = true;
    }

    private void BtnSelectNone_Click(object sender, EventArgs e)
    {
        foreach (DataGridViewRow row in dataGridView.Rows)
            if (row.Cells[0] is DataGridViewCheckBoxCell cb)
                cb.Value = false;
    }
}
