using System.Drawing.Drawing2D;

namespace OpenTuningTool.Controls;

public sealed class HeatmapView : UserControl
{
    private readonly record struct HeatmapLayout(
        float OriginX,
        float OriginY,
        float CellWidth,
        float CellHeight,
        float LegendX,
        float LegendY,
        float LegendHeight);

    private double[] _values = [];
    private string[] _displayValues = [];
    private int _rows;
    private int _cols;
    private double[]? _xLabels;
    private double[]? _yLabels;
    private double _minVal;
    private double _maxVal;
    private bool _hasData;

    private int _hoveredRow = -1;
    private int _hoveredCol = -1;

    private float _zoomFactor = 1.0f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 6.0f;

    private const int MinLeftMargin = 60;
    private const int MinTopMargin = 30;
    private const int MinRightMargin = 70;
    private const int MinBottomMargin = 24;
    private const int LegendWidth = 20;
    private const int LegendGap = 12;
    private const float BaseCellWidth = 36f;
    private const float BaseCellHeight = 20f;
    private const float MaxValueFontSize = 11.5f;

    private int _leftMargin = MinLeftMargin;
    private int _topMargin = MinTopMargin;
    private int _rightMargin = MinRightMargin;
    private int _bottomMargin = MinBottomMargin;
    private float _minimumCellWidth = BaseCellWidth;
    private float _minimumCellHeight = BaseCellHeight;

    private Color _bgColor = Color.FromArgb(30, 30, 30);
    private Color _fgColor = Color.FromArgb(220, 220, 220);
    private Color _gridColor = Color.FromArgb(60, 60, 60);
    private Color _accentColor = Color.FromArgb(0, 122, 204);

    private readonly ToolTip _tooltip = new() { InitialDelay = 100, ReshowDelay = 50 };
    private string _lastTooltip = string.Empty;

    public HeatmapView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void LoadData(double[] values, int rows, int cols, double[]? xLabels, double[]? yLabels, string[]? displayValues = null)
    {
        _values = values;
        _displayValues = displayValues != null && displayValues.Length == values.Length
            ? displayValues
            : values.Select(FormatCellValue).ToArray();
        _rows = rows;
        _cols = cols;
        _xLabels = xLabels;
        _yLabels = yLabels;
        _hasData = values.Length > 0 && rows > 0 && cols > 0;

        if (_hasData)
        {
            _minVal = values[0];
            _maxVal = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < _minVal) _minVal = values[i];
                if (values[i] > _maxVal) _maxVal = values[i];
            }
        }

        _zoomFactor = 1.0f;
        UpdateLayoutMetrics();
        UpdateScrollSize();
        Invalidate();
    }

    public void ApplyThemeColors(Color bg, Color fg, Color grid, Color accent)
    {
        _bgColor = bg;
        _fgColor = fg;
        _gridColor = grid;
        _accentColor = accent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bgBrush = new SolidBrush(_bgColor);
        g.FillRectangle(bgBrush, ClientRectangle);

        if (!_hasData)
        {
            using var msgBrush = new SolidBrush(Color.FromArgb(150, _fgColor));
            using var msgFont = new Font("Segoe UI", 10f, FontStyle.Italic);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("No map data to display.", msgFont, msgBrush, ClientRectangle, sf);
            return;
        }

        HeatmapLayout layout = CalculateLayout();
        float cellW = layout.CellWidth;
        float cellH = layout.CellHeight;

        using var gridPen = new Pen(_gridColor, 0.5f);
        using var labelFont = CreateLabelFont();
        using var labelBrush = new SolidBrush(Color.FromArgb(180, _fgColor));
        using var darkTextBrush = new SolidBrush(Color.FromArgb(24, 24, 24));
        using var lightTextBrush = new SolidBrush(Color.FromArgb(245, 245, 245));
        using var valueFont = CreateValueFont();
        using var valueFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoWrap
        };

        double range = _maxVal - _minVal;
        if (range == 0) range = 1;

        // Draw cells
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                float x = layout.OriginX + c * cellW;
                float y = layout.OriginY + r * cellH;
                int idx = r * _cols + c;
                if (idx >= _values.Length) continue;

                double norm = (_values[idx] - _minVal) / range;
                Color cellColor = ThemeUtility.ValueToHeatColor(norm);

                using var cellBrush = new SolidBrush(cellColor);
                g.FillRectangle(cellBrush, x, y, cellW, cellH);
                g.DrawRectangle(gridPen, x, y, cellW, cellH);

                RectangleF textBounds = new(x + 2, y + 2, cellW - 4, cellH - 4);
                Brush textBrush = GetCellTextBrush(cellColor, darkTextBrush, lightTextBrush);
                g.DrawString(GetDisplayValue(idx), valueFont, textBrush, textBounds, valueFormat);

                // Hover highlight
                if (r == _hoveredRow && c == _hoveredCol)
                {
                    using var hoverPen = new Pen(_accentColor, 2f);
                    g.DrawRectangle(hoverPen, x + 1, y + 1, cellW - 2, cellH - 2);
                }
            }
        }

        // X-axis labels (top)
        if (_xLabels != null)
        {
            int step = Math.Max(1, _cols / 20);
            for (int c = 0; c < _cols; c += step)
            {
                if (c >= _xLabels.Length) break;
                float x = _leftMargin + c * cellW + cellW / 2;
                string label = FormatAxisValue(_xLabels[c]);
                var size = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, labelBrush, x - size.Width / 2, layout.OriginY - size.Height - 2);
            }
        }

        // Y-axis labels (left)
        if (_yLabels != null)
        {
            int step = Math.Max(1, _rows / 20);
            for (int r = 0; r < _rows; r += step)
            {
                if (r >= _yLabels.Length) break;
                float y = layout.OriginY + r * cellH + cellH / 2;
                string label = FormatAxisValue(_yLabels[r]);
                var size = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, labelBrush, layout.OriginX - size.Width - 4, y - size.Height / 2);
            }
        }

        // Color legend bar
        float legendX = layout.LegendX;
        float legendY = layout.LegendY;
        float legendH = layout.LegendHeight;

        if (legendH > 0)
        {
            for (int py = 0; py < (int)legendH; py++)
            {
                double norm = 1.0 - (double)py / legendH;
                Color c = ThemeUtility.ValueToHeatColor(norm);
                using var pen = new Pen(c, 1f);
                g.DrawLine(pen, legendX, legendY + py, legendX + LegendWidth, legendY + py);
            }

            g.DrawRectangle(gridPen, legendX, legendY, LegendWidth, legendH);

            // Min/Max labels
            string maxStr = _maxVal.ToString("G5");
            string minStr = _minVal.ToString("G5");
            g.DrawString(maxStr, labelFont, labelBrush, legendX, legendY - 14);
            var minSize = g.MeasureString(minStr, labelFont);
            g.DrawString(minStr, labelFont, labelBrush, legendX, legendY + legendH + 2);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_hasData) return;

        HeatmapLayout layout = CalculateLayout();
        float cellW = layout.CellWidth;
        float cellH = layout.CellHeight;
        float mx = e.X - layout.OriginX;
        float my = e.Y - layout.OriginY;

        int col = (int)(mx / cellW);
        int row = (int)(my / cellH);

        if (row >= 0 && row < _rows && col >= 0 && col < _cols && mx >= 0 && my >= 0)
        {
            if (row != _hoveredRow || col != _hoveredCol)
            {
                _hoveredRow = row;
                _hoveredCol = col;
                int idx = row * _cols + col;
                if (idx < _values.Length)
                {
                    string tip = $"[{row},{col}] = {GetDisplayValue(idx)}";
                    if (tip != _lastTooltip)
                    {
                        _tooltip.SetToolTip(this, tip);
                        _lastTooltip = tip;
                    }
                }
                Invalidate();
            }
        }
        else if (_hoveredRow != -1 || _hoveredCol != -1)
        {
            _hoveredRow = -1;
            _hoveredCol = -1;
            _tooltip.SetToolTip(this, string.Empty);
            _lastTooltip = string.Empty;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredRow != -1 || _hoveredCol != -1)
        {
            _hoveredRow = -1;
            _hoveredCol = -1;
            _tooltip.SetToolTip(this, string.Empty);
            _lastTooltip = string.Empty;
            Invalidate();
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        UpdateLayoutMetrics();
        UpdateScrollSize();
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            float delta = e.Delta > 0 ? 1.15f : 0.87f;
            _zoomFactor = Math.Clamp(_zoomFactor * delta, MinZoom, MaxZoom);
            UpdateScrollSize();
            Invalidate();
        }
        else
        {
            base.OnMouseWheel(e);
        }
    }

    private float GetCellWidth()
    {
        return Math.Max(BaseCellWidth * _zoomFactor, _minimumCellWidth);
    }

    private float GetCellHeight()
    {
        return Math.Max(BaseCellHeight * _zoomFactor, _minimumCellHeight);
    }

    private void UpdateScrollSize() { }

    private void UpdateLayoutMetrics()
    {
        if (!_hasData)
        {
            _leftMargin = MinLeftMargin;
            _topMargin = MinTopMargin;
            _rightMargin = MinRightMargin;
            _bottomMargin = MinBottomMargin;
            _minimumCellWidth = BaseCellWidth;
            _minimumCellHeight = BaseCellHeight;
            return;
        }

        TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        using var valueFont = CreateValueFont();
        using var labelFont = CreateLabelFont();

        int maxValueWidth = 0;
        int maxValueHeight = 0;
        foreach (string text in _displayValues)
        {
            Size size = TextRenderer.MeasureText(string.IsNullOrEmpty(text) ? " " : text, valueFont, Size.Empty, flags);
            maxValueWidth = Math.Max(maxValueWidth, size.Width);
            maxValueHeight = Math.Max(maxValueHeight, size.Height);
        }

        _minimumCellWidth = Math.Max(BaseCellWidth, maxValueWidth + 10);
        _minimumCellHeight = Math.Max(BaseCellHeight, maxValueHeight + 8);

        int maxYLabelWidth = 0;
        if (_yLabels != null)
        {
            foreach (double value in _yLabels)
            {
                string text = FormatAxisValue(value);
                Size size = TextRenderer.MeasureText(text, labelFont, Size.Empty, flags);
                maxYLabelWidth = Math.Max(maxYLabelWidth, size.Width);
            }
        }

        int maxXLabelHeight = 0;
        if (_xLabels != null)
        {
            foreach (double value in _xLabels)
            {
                string text = FormatAxisValue(value);
                Size size = TextRenderer.MeasureText(text, labelFont, Size.Empty, flags);
                maxXLabelHeight = Math.Max(maxXLabelHeight, size.Height);
            }
        }

        string maxLegendText = _maxVal.ToString("G5");
        string minLegendText = _minVal.ToString("G5");
        int legendLabelWidth = Math.Max(
            TextRenderer.MeasureText(maxLegendText, labelFont, Size.Empty, flags).Width,
            TextRenderer.MeasureText(minLegendText, labelFont, Size.Empty, flags).Width);

        _leftMargin = Math.Max(MinLeftMargin, maxYLabelWidth + 8);
        _topMargin = Math.Max(MinTopMargin, maxXLabelHeight + 6);
        _rightMargin = Math.Max(MinRightMargin, LegendGap + LegendWidth + legendLabelWidth + 12);
        _bottomMargin = MinBottomMargin;
    }

    private Font CreateValueFont()
    {
        float size = Math.Clamp(Math.Max(Font.Size - 0.5f, 8f), 8f, MaxValueFontSize);
        return new Font("Consolas", size, FontStyle.Bold);
    }

    private Font CreateLabelFont()
    {
        float size = Math.Max(Font.Size - 1.25f, 7.5f);
        return new Font("Consolas", size, FontStyle.Regular);
    }

    private string GetDisplayValue(int index)
    {
        if (index >= 0 && index < _displayValues.Length)
            return _displayValues[index];

        if (index >= 0 && index < _values.Length)
            return FormatCellValue(_values[index]);

        return string.Empty;
    }

    private static Brush GetCellTextBrush(Color cellColor, Brush darkTextBrush, Brush lightTextBrush)
    {
        // WCAG-style luminance approximation keeps the text readable across the heat scale.
        double luminance =
            (0.2126 * cellColor.R / 255.0) +
            (0.7152 * cellColor.G / 255.0) +
            (0.0722 * cellColor.B / 255.0);
        return luminance > 0.58 ? darkTextBrush : lightTextBrush;
    }

    private static string FormatCellValue(double value)
    {
        if (Math.Abs(value - Math.Round(value)) < 0.000001 && Math.Abs(value) <= long.MaxValue)
            return ((long)Math.Round(value)).ToString();

        return value.ToString("G5");
    }

    private static string FormatAxisValue(double value)
    {
        if (value == Math.Floor(value) && Math.Abs(value) < 1e9)
            return ((int)value).ToString();
        return value.ToString("G4");
    }

    private HeatmapLayout CalculateLayout()
    {
        float preferredCellWidth = GetCellWidth();
        float preferredCellHeight = GetCellHeight();
        float availableWidth = Math.Max(1f, ClientSize.Width - _leftMargin - _rightMargin);
        float availableHeight = Math.Max(1f, ClientSize.Height - _topMargin - _bottomMargin);

        float fittedCellWidth = _cols > 0 ? availableWidth / _cols : preferredCellWidth;
        float fittedCellHeight = _rows > 0 ? availableHeight / _rows : preferredCellHeight;
        float cellWidth = Math.Min(preferredCellWidth, fittedCellWidth);
        float cellHeight = Math.Min(preferredCellHeight, fittedCellHeight);

        float contentWidth = _cols * cellWidth;
        float contentHeight = _rows * cellHeight;
        float originX = _leftMargin + Math.Max(0f, (availableWidth - contentWidth) / 2f);
        float originY = _topMargin + Math.Max(0f, (availableHeight - contentHeight) / 2f);
        float legendX = originX + contentWidth + LegendGap;

        return new HeatmapLayout(originX, originY, cellWidth, cellHeight, legendX, originY, contentHeight);
    }
}
