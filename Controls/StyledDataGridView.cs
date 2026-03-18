using System.Reflection;

namespace OpenTuningTool.Controls;

public sealed class StyledDataGridView : DataGridView
{
    private int _hoveredRow = -1;
    private Color _bgColor = Color.FromArgb(30, 30, 30);
    private Color _surfaceColor = Color.FromArgb(37, 37, 38);
    private Color _inputColor = Color.FromArgb(45, 45, 48);
    private Color _fgColor = Color.FromArgb(220, 220, 220);
    private Color _accentColor = Color.FromArgb(0, 122, 204);
    private Color _gridColor = Color.FromArgb(60, 60, 60);
    private Color _hoverColor = Color.FromArgb(55, 55, 60);

    public StyledDataGridView()
    {
        // Enable double buffering via reflection (not publicly exposed)
        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(this, true);

        EnableHeadersVisualStyles = false;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        SelectionMode = DataGridViewSelectionMode.CellSelect;
        AllowUserToResizeRows = false;

        ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 30;
        RowTemplate.Height = 24;
    }

    public void ApplyThemeColors(Color bg, Color surface, Color input, Color fg, Color accent, Color grid)
    {
        _bgColor = bg;
        _surfaceColor = surface;
        _inputColor = input;
        _fgColor = fg;
        _accentColor = accent;
        _gridColor = grid;
        _hoverColor = Color.FromArgb(
            Math.Min(input.R + 12, 255),
            Math.Min(input.G + 12, 255),
            Math.Min(input.B + 12, 255));

        BackgroundColor = bg;
        GridColor = grid;

        ColumnHeadersDefaultCellStyle.BackColor = surface;
        ColumnHeadersDefaultCellStyle.ForeColor = fg;
        ColumnHeadersDefaultCellStyle.SelectionBackColor = surface;
        ColumnHeadersDefaultCellStyle.SelectionForeColor = fg;

        RowHeadersDefaultCellStyle.BackColor = surface;
        RowHeadersDefaultCellStyle.ForeColor = fg;
        RowHeadersDefaultCellStyle.SelectionBackColor = surface;
        RowHeadersDefaultCellStyle.SelectionForeColor = fg;

        DefaultCellStyle.BackColor = input;
        DefaultCellStyle.ForeColor = fg;
        DefaultCellStyle.SelectionBackColor = accent;
        DefaultCellStyle.SelectionForeColor = fg;

        // Alternating rows: slightly different shade
        AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(
            (int)(input.R * 0.93 + bg.R * 0.07),
            (int)(input.G * 0.93 + bg.G * 0.07),
            (int)(input.B * 0.93 + bg.B * 0.07));
        AlternatingRowsDefaultCellStyle.ForeColor = fg;

        Invalidate();
    }

    protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseEnter(e);
        if (e.RowIndex != _hoveredRow && e.RowIndex >= 0)
        {
            int oldRow = _hoveredRow;
            _hoveredRow = e.RowIndex;
            if (oldRow >= 0 && oldRow < Rows.Count)
                InvalidateRow(oldRow);
            InvalidateRow(_hoveredRow);
        }
    }

    protected override void OnCellMouseLeave(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseLeave(e);
        if (_hoveredRow >= 0 && _hoveredRow < Rows.Count)
        {
            int old = _hoveredRow;
            _hoveredRow = -1;
            InvalidateRow(old);
        }
    }

    protected override void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex == _hoveredRow && e.RowIndex >= 0)
        {
            // Paint hover highlight across the full row
            Rectangle rowBounds = GetRowDisplayRectangle(e.RowIndex, true);
            if (rowBounds.Height > 0)
            {
                using var brush = new SolidBrush(_hoverColor);
                e.Graphics.FillRectangle(brush, rowBounds);
                e.PaintParts &= ~DataGridViewPaintParts.Background;
            }
        }

        base.OnRowPrePaint(e);
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        // Custom header bottom border (accent line instead of default)
        if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        {
            e.Paint(e.ClipBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);

            if (e.Graphics != null)
            {
                using var pen = new Pen(_accentColor, 1.5f);
                int y = e.CellBounds.Bottom - 1;
                e.Graphics.DrawLine(pen, e.CellBounds.Left, y, e.CellBounds.Right, y);
            }

            e.Handled = true;
            return;
        }

        base.OnCellPainting(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredRow >= 0 && _hoveredRow < Rows.Count)
        {
            int old = _hoveredRow;
            _hoveredRow = -1;
            InvalidateRow(old);
        }
    }
}
