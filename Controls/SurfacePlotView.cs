using System.Drawing.Drawing2D;

namespace OpenTuningTool.Controls;

public sealed class SurfacePointEventArgs(int row, int col, double value) : EventArgs
{
    public int Row   { get; } = row;
    public int Col   { get; } = col;
    public double Value { get; } = value;
}

public sealed class SurfacePlotView : UserControl
{
    private double[] _values = [];
    private int _rows;
    private int _cols;
    private double[]? _xLabels;
    private double[]? _yLabels;
    private double _minVal;
    private double _maxVal;
    private bool _hasData;

    // Camera
    private double _rotX = -30.0;
    private double _rotY = 45.0;
    private double _zoom = 1.0;
    private const double MinZoom     = 0.3;
    private const double MaxZoom     = 5.0;
    private const double Perspective = 0.0012;

    // Right-click orbit
    private bool  _isOrbiting;
    private Point _orbitLastMouse;

    // Left-click interaction
    private int   _pendingDragIndex = -1;
    private Point _leftDragStart;
    private bool  _isValueDragging;
    private bool  _isBoxSelecting;
    private Point _boxSelectCurrent;
    private double[] _dragBaseValues = [];
    private const int DragThreshold = 5;

    // Theme colors
    private Color _bgColor     = Color.FromArgb(30, 30, 30);
    private Color _fgColor     = Color.FromArgb(220, 220, 220);
    private Color _gridColor   = Color.FromArgb(60, 60, 60);
    private Color _accentColor = Color.FromArgb(0, 122, 204);

    // Tooltip
    private readonly ToolTip _tooltip = new() { InitialDelay = 200, ReshowDelay = 100 };

    // Cached projection
    private PointF[]? _projectedPoints;
    private double[]? _normalizedValues;

    // Multi-selection
    private readonly HashSet<int> _selectedIndices = new();

    // Events
    public event EventHandler<SurfacePointEventArgs>? PointSelected;
    public event EventHandler<SurfacePointEventArgs>? PointActivated;
    public event Action<IReadOnlyCollection<int>>?    SelectionChanged;
    public event Action<int[], double[]>?             PointsValueChanged;

    public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;

    public void SetSelectedIndices(IEnumerable<int> indices)
    {
        _selectedIndices.Clear();
        _selectedIndices.UnionWith(indices);
        Invalidate();
    }

    public SurfacePlotView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Default;
    }

    public void LoadData(double[] values, int rows, int cols, double[]? xLabels, double[]? yLabels)
    {
        _values  = (double[])values.Clone();
        _rows    = rows;
        _cols    = cols;
        _xLabels = xLabels;
        _yLabels = yLabels;
        _hasData = values.Length > 0 && rows > 0 && cols > 0;

        _selectedIndices.Clear();

        if (_hasData)
        {
            RecomputeMinMax();
            RebuildNormalizedValues();
        }

        _projectedPoints = null;
        Invalidate();
    }

    public void ResetView()
    {
        _rotX = -30.0;
        _rotY = 45.0;
        _zoom = 1.0;
        _projectedPoints = null;
        Invalidate();
    }

    public void ApplyThemeColors(Color bg, Color fg, Color grid, Color accent)
    {
        _bgColor     = bg;
        _fgColor     = fg;
        _gridColor   = grid;
        _accentColor = accent;
        Invalidate();
    }

    private void RecomputeMinMax()
    {
        if (_values.Length == 0) return;
        _minVal = _values[0];
        _maxVal = _values[0];
        for (int i = 1; i < _values.Length; i++)
        {
            if (_values[i] < _minVal) _minVal = _values[i];
            if (_values[i] > _maxVal) _maxVal = _values[i];
        }
    }

    private void RebuildNormalizedValues()
    {
        double range = _maxVal - _minVal;
        if (range == 0) range = 1;
        _normalizedValues = new double[_values.Length];
        for (int i = 0; i < _values.Length; i++)
            _normalizedValues[i] = (_values[i] - _minVal) / range;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bgBrush = new SolidBrush(_bgColor);
        g.FillRectangle(bgBrush, ClientRectangle);

        if (!_hasData || _normalizedValues == null)
        {
            using var msgBrush = new SolidBrush(Color.FromArgb(150, _fgColor));
            using var msgFont  = new Font("Segoe UI", 10f, FontStyle.Italic);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("No map data to display.", msgFont, msgBrush, ClientRectangle, sf);
            return;
        }

        float cx    = Width  / 2f;
        float cy    = Height / 2f;
        float scale = (float)(Math.Min(Width, Height) * 0.29 * _zoom);

        double rxRad = _rotX * Math.PI / 180.0;
        double ryRad = _rotY * Math.PI / 180.0;
        double cosX = Math.Cos(rxRad), sinX = Math.Sin(rxRad);
        double cosY = Math.Cos(ryRad), sinY = Math.Sin(ryRad);

        // Auto-centre the cube
        RectangleF cubeBounds = GetProjectedBounds(cx, cy, scale, cosX, sinX, cosY, sinY);
        cx += (Width  / 2f) - (cubeBounds.Left + cubeBounds.Width  / 2f);
        cy += (Height / 2f) - (cubeBounds.Top  + cubeBounds.Height / 2f);

        int totalPts = _rows * _cols;
        var projected = new PointF[totalPts];
        var depths    = new float[totalPts];

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                int idx = r * _cols + c;
                double x3 = (_cols > 1) ? (2.0 * c / (_cols - 1) - 1.0) : 0;
                double z3 = (_rows > 1) ? (2.0 * r / (_rows - 1) - 1.0) : 0;
                double y3 = (idx < _normalizedValues.Length) ? (_normalizedValues[idx] * 2.0) - 1.0 : -1.0;

                double rx = x3 * cosY - z3 * sinY;
                double rz = x3 * sinY + z3 * cosY;
                double ry  = y3 * cosX - rz * sinX;
                double rz2 = y3 * sinX + rz * cosX;

                double f = 1.0 / (1.0 + rz2 * Perspective * scale);
                projected[idx] = new PointF(cx + (float)(rx * scale * f),
                                            cy - (float)(ry * scale * f));
                depths[idx]    = (float)rz2;
            }
        }

        _projectedPoints = projected;

        int quadCount = (_rows - 1) * (_cols - 1);
        var quads = new (int tl, int tr, int br, int bl, float depth, double avgNorm)[quadCount];
        int qi = 0;

        for (int r = 0; r < _rows - 1; r++)
        {
            for (int c = 0; c < _cols - 1; c++)
            {
                int tl = r * _cols + c;
                int tr = r * _cols + c + 1;
                int bl = (r + 1) * _cols + c;
                int br = (r + 1) * _cols + c + 1;

                float  avgDepth = (depths[tl] + depths[tr] + depths[bl] + depths[br]) / 4f;
                double avgNorm  = (_normalizedValues[Math.Min(tl, _normalizedValues.Length - 1)] +
                                   _normalizedValues[Math.Min(tr, _normalizedValues.Length - 1)] +
                                   _normalizedValues[Math.Min(bl, _normalizedValues.Length - 1)] +
                                   _normalizedValues[Math.Min(br, _normalizedValues.Length - 1)]) / 4.0;

                quads[qi++] = (tl, tr, br, bl, avgDepth, avgNorm);
            }
        }

        Array.Sort(quads, (a, b) => b.depth.CompareTo(a.depth));
        DrawCubeGuides(g, cx, cy, scale, cosX, sinX, cosY, sinY);

        using var wirePen = new Pen(Color.FromArgb(60, _fgColor), 0.5f);

        for (int i = 0; i < quadCount; i++)
        {
            var q = quads[i];
            PointF[] pts = [ projected[q.tl], projected[q.tr], projected[q.br], projected[q.bl] ];

            Color fillColor = ThemeUtility.ValueToHeatColor(q.avgNorm);
            float shade = Math.Clamp(0.6f + 0.4f * (1f - (q.depth + 1f) / 2f), 0.4f, 1.0f);
            fillColor = Color.FromArgb(
                (int)(fillColor.R * shade),
                (int)(fillColor.G * shade),
                (int)(fillColor.B * shade));

            using var fillBrush = new SolidBrush(fillColor);
            g.FillPolygon(fillBrush, pts);
            g.DrawPolygon(wirePen, pts);
        }

        DrawCubeFrame(g, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawAxisOverlay(g, cx, cy, scale, cosX, sinX, cosY, sinY);

        // Selected point highlights
        if (_selectedIndices.Count > 0)
        {
            using var selFill = new SolidBrush(Color.FromArgb(230, _accentColor));
            using var selRing = new Pen(_accentColor, 2.5f);
            foreach (int idx in _selectedIndices)
            {
                if (idx < 0 || idx >= projected.Length) continue;
                var pt = projected[idx];
                g.FillEllipse(selFill, pt.X - 4f, pt.Y - 4f, 8f, 8f);
                g.DrawEllipse(selRing, pt.X - 7f, pt.Y - 7f, 14f, 14f);
            }
        }

        // Box-select rubber band
        if (_isBoxSelecting)
        {
            var rect = GetBoxRect();
            using var bandFill = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
            using var bandPen  = new Pen(Color.White, 1f) { DashStyle = DashStyle.Dash };
            g.FillRectangle(bandFill, rect);
            g.DrawRectangle(bandPen, rect);
        }

        using var hintFont  = new Font("Segoe UI", 7.5f);
        using var hintBrush = new SolidBrush(Color.FromArgb(80, _fgColor));
        g.DrawString(
            "Right-drag orbit  ·  Left-click select  ·  Drag point to edit  ·  Wheel zoom  ·  Dbl-click reset",
            hintFont, hintBrush, 8f, Height - 18f);
    }

    // -----------------------------------------------------------------------
    // Mouse handling
    // -----------------------------------------------------------------------

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Right)
        {
            _isOrbiting     = true;
            _orbitLastMouse = e.Location;
            Cursor          = Cursors.Hand;
        }
        else if (e.Button == MouseButtons.Left && _hasData)
        {
            _pendingDragIndex = FindNearestPoint(e.Location, 20f);
            _leftDragStart    = e.Location;
            _boxSelectCurrent = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isOrbiting)
        {
            int dx = e.X - _orbitLastMouse.X;
            int dy = e.Y - _orbitLastMouse.Y;
            _rotY += dx * 0.5;
            _rotX += dy * 0.5;
            _rotX = Math.Clamp(_rotX, -89, 89);
            _orbitLastMouse  = e.Location;
            _projectedPoints = null;
            Invalidate();
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            int dx = e.X - _leftDragStart.X;
            int dy = e.Y - _leftDragStart.Y;
            bool pastThreshold = Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold;

            if (pastThreshold && !_isValueDragging && !_isBoxSelecting)
            {
                if (_pendingDragIndex >= 0)
                {
                    _isValueDragging = true;
                    _dragBaseValues  = (double[])_values.Clone();
                    Cursor           = Cursors.SizeNS;
                }
                else
                {
                    _isBoxSelecting = true;
                    Cursor          = Cursors.Cross;
                }
            }

            if (_isValueDragging)
            {
                double range = _maxVal - _minVal;
                if (range == 0) range = 1.0;
                double valueDelta = -(double)dy * range / Math.Max(Height, 1);

                IEnumerable<int> targets = _selectedIndices.Count > 0
                    ? _selectedIndices
                    : Enumerable.Repeat(_pendingDragIndex, 1);

                foreach (int idx in targets)
                {
                    if (idx >= 0 && idx < _values.Length)
                        _values[idx] = _dragBaseValues[idx] + valueDelta;
                }

                RecomputeMinMax();
                RebuildNormalizedValues();
                _projectedPoints = null;
                Invalidate();
            }
            else if (_isBoxSelecting)
            {
                _boxSelectCurrent = e.Location;
                Invalidate();
            }
            return;
        }

        // Hover tooltip (no button held)
        if (!_hasData || _projectedPoints == null) return;

        int nearest = FindNearestPoint(e.Location, 20f);
        if (nearest >= 0 && nearest < _values.Length)
        {
            int r = nearest / _cols;
            int c = nearest % _cols;
            _tooltip.SetToolTip(this, $"[{r},{c}] = {_values[nearest]:G6}");
            Cursor = Cursors.Hand;
        }
        else
        {
            _tooltip.SetToolTip(this, string.Empty);
            Cursor = Cursors.Default;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Right && _isOrbiting)
        {
            _isOrbiting = false;
            Cursor       = Cursors.Default;
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        if (_isValueDragging)
        {
            _isValueDragging = false;
            Cursor = Cursors.Default;

            IEnumerable<int> targets = _selectedIndices.Count > 0
                ? _selectedIndices
                : Enumerable.Repeat(_pendingDragIndex, 1);

            int[]    changedIndices = targets.Where(i => i >= 0 && i < _values.Length).Distinct().ToArray();
            double[] changedValues  = changedIndices.Select(i => _values[i]).ToArray();

            if (changedIndices.Length > 0)
                PointsValueChanged?.Invoke(changedIndices, changedValues);
        }
        else if (_isBoxSelecting)
        {
            _isBoxSelecting = false;
            Cursor = Cursors.Default;

            if (!ModifierKeys.HasFlag(Keys.Control))
                _selectedIndices.Clear();

            if (_projectedPoints != null)
            {
                var rect = GetBoxRect();
                for (int i = 0; i < _projectedPoints.Length; i++)
                {
                    if (rect.Contains((int)_projectedPoints[i].X, (int)_projectedPoints[i].Y))
                        _selectedIndices.Add(i);
                }
            }

            Invalidate();
            SelectionChanged?.Invoke(_selectedIndices);
        }
        else
        {
            // Simple click
            if (!ModifierKeys.HasFlag(Keys.Control))
            {
                _selectedIndices.Clear();
                if (_pendingDragIndex >= 0)
                    _selectedIndices.Add(_pendingDragIndex);
            }
            else if (_pendingDragIndex >= 0)
            {
                if (!_selectedIndices.Remove(_pendingDragIndex))
                    _selectedIndices.Add(_pendingDragIndex);
            }

            Invalidate();
            SelectionChanged?.Invoke(_selectedIndices);

            // Fire legacy single-point event
            if (_pendingDragIndex >= 0 && _pendingDragIndex < _values.Length)
                PointSelected?.Invoke(this, CreateEventArgs(_pendingDragIndex));
        }

        _pendingDragIndex = -1;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left) return;

        int idx = FindNearestPoint(e.Location, 14f);
        if (idx < 0 || idx >= _values.Length) return;

        _selectedIndices.Clear();
        _selectedIndices.Add(idx);
        Invalidate();
        SelectionChanged?.Invoke(_selectedIndices);
        PointActivated?.Invoke(this, CreateEventArgs(idx));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        double factor = e.Delta > 0 ? 1.12 : 0.89;
        _zoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        _projectedPoints = null;
        Invalidate();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private int FindNearestPoint(Point mousePos, float maxDistPx)
    {
        if (_projectedPoints == null) return -1;
        float bestDist = maxDistPx * maxDistPx;
        int   bestIdx  = -1;
        for (int i = 0; i < _projectedPoints.Length; i++)
        {
            float dx = _projectedPoints[i].X - mousePos.X;
            float dy = _projectedPoints[i].Y - mousePos.Y;
            float d2 = dx * dx + dy * dy;
            if (d2 < bestDist) { bestDist = d2; bestIdx = i; }
        }
        return bestIdx;
    }

    private Rectangle GetBoxRect()
    {
        int x = Math.Min(_leftDragStart.X, _boxSelectCurrent.X);
        int y = Math.Min(_leftDragStart.Y, _boxSelectCurrent.Y);
        int w = Math.Abs(_leftDragStart.X - _boxSelectCurrent.X);
        int h = Math.Abs(_leftDragStart.Y - _boxSelectCurrent.Y);
        return new Rectangle(x, y, w, h);
    }

    private SurfacePointEventArgs CreateEventArgs(int index)
        => new(index / _cols, index % _cols, _values[index]);

    // -----------------------------------------------------------------------
    // Drawing helpers
    // -----------------------------------------------------------------------

    private void DrawCubeGuides(Graphics g, float cx, float cy, float scale,
                                double cosX, double sinX, double cosY, double sinY)
    {
        using var guidePen = new Pen(Color.FromArgb(38, _fgColor.R, _fgColor.G, _fgColor.B), 0.6f);
        const int divisions = 4;
        for (int i = 0; i <= divisions; i++)
        {
            double t = -1.0 + (2.0 * i / divisions);
            DrawProjectedLine(g, guidePen, -1, -1, t, 1, -1, t, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, t, -1, -1, t, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, -1, t, -1, -1, t, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, -1, -1, t, -1, 1, t, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, -1, t, 1, 1, t, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, t, -1, 1, t, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        }
    }

    private void DrawCubeFrame(Graphics g, float cx, float cy, float scale,
                               double cosX, double sinX, double cosY, double sinY)
    {
        using var framePen = new Pen(Color.FromArgb(118, _fgColor.R, _fgColor.G, _fgColor.B), 1f);
        DrawProjectedLine(g, framePen, -1, -1, -1,  1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1, -1, -1,  1,  1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1,  1, -1, -1,  1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1,  1, -1, -1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1, -1,  1,  1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1, -1,  1,  1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1,  1,  1, -1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1,  1,  1, -1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1, -1, -1, -1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1, -1, -1,  1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen,  1,  1, -1,  1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1,  1, -1, -1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
    }

    private void DrawAxisOverlay(Graphics g, float cx, float cy, float scale,
                                 double cosX, double sinX, double cosY, double sinY)
    {
        using var axisPen    = new Pen(_accentColor, 1.8f);
        using var axisBrush  = new SolidBrush(_accentColor);
        using var labelBrush = new SolidBrush(_fgColor);
        using var axisFont   = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var valueFont  = new Font("Consolas", 7.5f);

        PointF origin = Project3D(-1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF xEnd   = Project3D( 1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF yEnd   = Project3D(-1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF zEnd   = Project3D(-1,  1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);

        g.DrawLine(axisPen, origin, xEnd);
        g.DrawLine(axisPen, origin, yEnd);
        g.DrawLine(axisPen, origin, zEnd);
        g.FillEllipse(axisBrush, origin.X - 3f, origin.Y - 3f, 6f, 6f);

        DrawAxisText(g, axisFont, axisBrush, "X", xEnd, 6f, -16f);
        DrawAxisText(g, axisFont, axisBrush, "Y", yEnd, 6f, -16f);
        DrawAxisText(g, axisFont, axisBrush, "Z", zEnd, 6f, -16f);

        DrawAxisText(g, valueFont, labelBrush, GetXAxisStartLabel(), origin,  8f,  8f);
        DrawAxisText(g, valueFont, labelBrush, GetXAxisEndLabel(),   xEnd,    8f,  8f);
        DrawAxisText(g, valueFont, labelBrush, GetYAxisStartLabel(), origin, -38f, 8f);
        DrawAxisText(g, valueFont, labelBrush, GetYAxisEndLabel(),   yEnd,    8f,  8f);
        DrawAxisText(g, valueFont, labelBrush, GetZStartLabel(),     origin, -40f, -14f);
        DrawAxisText(g, valueFont, labelBrush, GetZEndLabel(),       zEnd,    8f,  -2f);
    }

    private static void DrawAxisText(Graphics g, Font font, Brush brush, string text,
                                     PointF anchor, float offsetX, float offsetY)
        => g.DrawString(text, font, brush, anchor.X + offsetX, anchor.Y + offsetY);

    private void DrawProjectedLine(Graphics g, Pen pen,
                                   double x1, double y1, double z1,
                                   double x2, double y2, double z2,
                                   float cx, float cy, float scale,
                                   double cosX, double sinX, double cosY, double sinY)
        => g.DrawLine(pen,
            Project3D(x1, y1, z1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(x2, y2, z2, cx, cy, scale, cosX, sinX, cosY, sinY));

    private string GetXAxisStartLabel() => GetAxisLabel(_xLabels, 0, 0);
    private string GetXAxisEndLabel()   => GetAxisLabel(_xLabels, Math.Max(0, _cols - 1), Math.Max(0, _cols - 1));
    private string GetYAxisStartLabel() => GetAxisLabel(_yLabels, 0, 0);
    private string GetYAxisEndLabel()   => GetAxisLabel(_yLabels, Math.Max(0, _rows - 1), Math.Max(0, _rows - 1));
    private string GetZStartLabel()     => FormatAxisValue(_minVal);
    private string GetZEndLabel()       => FormatAxisValue(_maxVal);

    private static string GetAxisLabel(double[]? labels, int preferredIndex, int fallbackValue)
    {
        if (labels == null || labels.Length == 0) return fallbackValue.ToString();
        return FormatAxisValue(labels[Math.Clamp(preferredIndex, 0, labels.Length - 1)]);
    }

    private static string FormatAxisValue(double value)
    {
        if (Math.Abs(value - Math.Round(value)) < 0.000001 && Math.Abs(value) <= long.MaxValue)
            return ((long)Math.Round(value)).ToString();
        return value.ToString("G5");
    }

    private RectangleF GetProjectedBounds(float cx, float cy, float scale,
                                          double cosX, double sinX, double cosY, double sinY)
    {
        PointF[] corners =
        [
            Project3D(-1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D( 1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D( 1,  1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1,  1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D( 1, -1,  1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D( 1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1,  1,  1, cx, cy, scale, cosX, sinX, cosY, sinY),
        ];

        float minX = corners[0].X, maxX = corners[0].X;
        float minY = corners[0].Y, maxY = corners[0].Y;
        for (int i = 1; i < corners.Length; i++)
        {
            minX = Math.Min(minX, corners[i].X); maxX = Math.Max(maxX, corners[i].X);
            minY = Math.Min(minY, corners[i].Y); maxY = Math.Max(maxY, corners[i].Y);
        }
        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    private PointF Project3D(double x3, double y3, double z3,
                              float cx, float cy, float scale,
                              double cosX, double sinX, double cosY, double sinY)
    {
        double rx  = x3 * cosY - z3 * sinY;
        double rz  = x3 * sinY + z3 * cosY;
        double ry  = y3 * cosX - rz * sinX;
        double rz2 = y3 * sinX + rz * cosX;
        double f   = 1.0 / (1.0 + rz2 * Perspective * scale);
        return new PointF(cx + (float)(rx * scale * f),
                          cy - (float)(ry * scale * f));
    }
}
