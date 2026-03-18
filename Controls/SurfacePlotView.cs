using System.Drawing.Drawing2D;

namespace OpenTuningTool.Controls;

public sealed class SurfacePointEventArgs(int row, int col, double value) : EventArgs
{
    public int Row { get; } = row;
    public int Col { get; } = col;
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
    private double _rotX = -30.0; // pitch (degrees)
    private double _rotY = 45.0;  // yaw (degrees)
    private double _zoom = 1.0;
    private const double MinZoom = 0.3;
    private const double MaxZoom = 5.0;
    private const double Perspective = 0.0012;

    // Mouse drag
    private bool _isDragging;
    private Point _lastMouse;
    private Point _mouseDownLocation;
    private bool _dragExceededClickThreshold;

    // Theme colors
    private Color _bgColor = Color.FromArgb(30, 30, 30);
    private Color _fgColor = Color.FromArgb(220, 220, 220);
    private Color _gridColor = Color.FromArgb(60, 60, 60);
    private Color _accentColor = Color.FromArgb(0, 122, 204);

    // Tooltip
    private readonly ToolTip _tooltip = new() { InitialDelay = 200, ReshowDelay = 100 };

    // Cached projection
    private PointF[]? _projectedPoints;
    private double[]? _normalizedValues;
    private int _selectedIndex = -1;

    public event EventHandler<SurfacePointEventArgs>? PointSelected;
    public event EventHandler<SurfacePointEventArgs>? PointActivated;

    public SurfacePlotView()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.SizeAll;
    }

    public void LoadData(double[] values, int rows, int cols, double[]? xLabels, double[]? yLabels)
    {
        _values = values;
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

            double range = _maxVal - _minVal;
            if (range == 0) range = 1;
            _normalizedValues = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
                _normalizedValues[i] = (values[i] - _minVal) / range;
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
        _bgColor = bg;
        _fgColor = fg;
        _gridColor = grid;
        _accentColor = accent;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bgBrush = new SolidBrush(_bgColor);
        g.FillRectangle(bgBrush, ClientRectangle);

        if (!_hasData || _normalizedValues == null)
        {
            using var msgBrush = new SolidBrush(Color.FromArgb(150, _fgColor));
            using var msgFont = new Font("Segoe UI", 10f, FontStyle.Italic);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("No map data to display.", msgFont, msgBrush, ClientRectangle, sf);
            return;
        }

        float cx = Width / 2f;
        float cy = Height / 2f;
        float scale = (float)(Math.Min(Width, Height) * 0.29 * _zoom);

        // Convert rotation to radians
        double rxRad = _rotX * Math.PI / 180.0;
        double ryRad = _rotY * Math.PI / 180.0;
        double cosX = Math.Cos(rxRad), sinX = Math.Sin(rxRad);
        double cosY = Math.Cos(ryRad), sinY = Math.Sin(ryRad);

        RectangleF cubeBounds = GetProjectedBounds(cx, cy, scale, cosX, sinX, cosY, sinY);
        float offsetX = (Width / 2f) - (cubeBounds.Left + cubeBounds.Width / 2f);
        float offsetY = (Height / 2f) - (cubeBounds.Top + cubeBounds.Height / 2f);
        cx += offsetX;
        cy += offsetY;

        // Project all grid points
        int totalPts = _rows * _cols;
        var projected = new PointF[totalPts];
        var depths = new float[totalPts];

        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                int idx = r * _cols + c;
                // Map to -1..1 space
                double x3 = (_cols > 1) ? (2.0 * c / (_cols - 1) - 1.0) : 0;
                double z3 = (_rows > 1) ? (2.0 * r / (_rows - 1) - 1.0) : 0;
                double y3 = (idx < _normalizedValues.Length) ? (_normalizedValues[idx] * 2.0) - 1.0 : -1.0;

                // Rotate Y (yaw)
                double rx = x3 * cosY - z3 * sinY;
                double rz = x3 * sinY + z3 * cosY;

                // Rotate X (pitch)
                double ry = y3 * cosX - rz * sinX;
                double rz2 = y3 * sinX + rz * cosX;

                // Perspective projection
                double perspFactor = 1.0 / (1.0 + rz2 * Perspective * scale);
                float sx = cx + (float)(rx * scale * perspFactor);
                float sy = cy - (float)(ry * scale * perspFactor);

                projected[idx] = new PointF(sx, sy);
                depths[idx] = (float)rz2;
            }
        }

        _projectedPoints = projected;

        // Build quads and sort by average depth (painter's algorithm)
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

                float avgDepth = (depths[tl] + depths[tr] + depths[bl] + depths[br]) / 4f;
                double avgNorm = (_normalizedValues[Math.Min(tl, _normalizedValues.Length - 1)] +
                                  _normalizedValues[Math.Min(tr, _normalizedValues.Length - 1)] +
                                  _normalizedValues[Math.Min(bl, _normalizedValues.Length - 1)] +
                                  _normalizedValues[Math.Min(br, _normalizedValues.Length - 1)]) / 4.0;

                quads[qi++] = (tl, tr, br, bl, avgDepth, avgNorm);
            }
        }

        // Sort back to front (largest depth first)
        Array.Sort(quads, (a, b) => b.depth.CompareTo(a.depth));

        DrawCubeGuides(g, cx, cy, scale, cosX, sinX, cosY, sinY);

        // Draw quads
        using var wirePen = new Pen(Color.FromArgb(60, _fgColor), 0.5f);

        for (int i = 0; i < quadCount; i++)
        {
            var q = quads[i];
            PointF[] pts =
            [
                projected[q.tl],
                projected[q.tr],
                projected[q.br],
                projected[q.bl]
            ];

            Color fillColor = ThemeUtility.ValueToHeatColor(q.avgNorm);
            // Slight shading based on depth for 3D effect
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

        if (_selectedIndex >= 0 && _projectedPoints != null && _selectedIndex < _projectedPoints.Length)
        {
            PointF selectedPoint = _projectedPoints[_selectedIndex];
            using var selectedPen = new Pen(_accentColor, 2.5f);
            using var selectedBrush = new SolidBrush(Color.FromArgb(230, _accentColor));
            g.FillEllipse(selectedBrush, selectedPoint.X - 4f, selectedPoint.Y - 4f, 8f, 8f);
            g.DrawEllipse(selectedPen, selectedPoint.X - 7f, selectedPoint.Y - 7f, 14f, 14f);
        }
    }

    private void DrawCubeGuides(Graphics g, float cx, float cy, float scale,
                                double cosX, double sinX, double cosY, double sinY)
    {
        using var guidePen = new Pen(Color.FromArgb(38, _fgColor.R, _fgColor.G, _fgColor.B), 0.6f);
        const int divisions = 4;

        for (int i = 0; i <= divisions; i++)
        {
            double t = -1.0 + (2.0 * i / divisions);

            // Floor plane.
            DrawProjectedLine(g, guidePen, -1, -1, t, 1, -1, t, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, t, -1, -1, t, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);

            // Left wall.
            DrawProjectedLine(g, guidePen, -1, t, -1, -1, t, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, -1, -1, t, -1, 1, t, cx, cy, scale, cosX, sinX, cosY, sinY);

            // Rear wall.
            DrawProjectedLine(g, guidePen, -1, t, 1, 1, t, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
            DrawProjectedLine(g, guidePen, t, -1, 1, t, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        }
    }

    private void DrawCubeFrame(Graphics g, float cx, float cy, float scale,
                               double cosX, double sinX, double cosY, double sinY)
    {
        using var framePen = new Pen(Color.FromArgb(118, _fgColor.R, _fgColor.G, _fgColor.B), 1f);

        DrawProjectedLine(g, framePen, -1, -1, -1, 1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, -1, -1, 1, 1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, 1, -1, -1, 1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1, 1, -1, -1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);

        DrawProjectedLine(g, framePen, -1, -1, 1, 1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, -1, 1, 1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, 1, 1, -1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1, 1, 1, -1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);

        DrawProjectedLine(g, framePen, -1, -1, -1, -1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, -1, -1, 1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, 1, 1, -1, 1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        DrawProjectedLine(g, framePen, -1, 1, -1, -1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
    }

    private void DrawAxisOverlay(Graphics g, float cx, float cy, float scale,
                                 double cosX, double sinX, double cosY, double sinY)
    {
        using var axisPen = new Pen(_accentColor, 1.8f);
        using var axisBrush = new SolidBrush(_accentColor);
        using var labelBrush = new SolidBrush(_fgColor);
        using var axisFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var valueFont = new Font("Consolas", 7.5f);

        PointF origin = Project3D(-1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF xEnd = Project3D(1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF yEnd = Project3D(-1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF zEnd = Project3D(-1, 1, -1, cx, cy, scale, cosX, sinX, cosY, sinY);

        g.DrawLine(axisPen, origin, xEnd);
        g.DrawLine(axisPen, origin, yEnd);
        g.DrawLine(axisPen, origin, zEnd);
        g.FillEllipse(axisBrush, origin.X - 3f, origin.Y - 3f, 6f, 6f);

        DrawAxisText(g, axisFont, axisBrush, "X", xEnd, 6f, -16f);
        DrawAxisText(g, axisFont, axisBrush, "Y", yEnd, 6f, -16f);
        DrawAxisText(g, axisFont, axisBrush, "Z", zEnd, 6f, -16f);

        DrawAxisText(g, valueFont, labelBrush, GetXAxisStartLabel(), origin, 8f, 8f);
        DrawAxisText(g, valueFont, labelBrush, GetXAxisEndLabel(), xEnd, 8f, 8f);

        DrawAxisText(g, valueFont, labelBrush, GetYAxisStartLabel(), origin, -38f, 8f);
        DrawAxisText(g, valueFont, labelBrush, GetYAxisEndLabel(), yEnd, 8f, 8f);

        DrawAxisText(g, valueFont, labelBrush, GetZStartLabel(), origin, -40f, -14f);
        DrawAxisText(g, valueFont, labelBrush, GetZEndLabel(), zEnd, 8f, -2f);
    }

    private static void DrawAxisText(Graphics g, Font font, Brush brush, string text, PointF anchor, float offsetX, float offsetY)
    {
        g.DrawString(text, font, brush, anchor.X + offsetX, anchor.Y + offsetY);
    }

    private void DrawProjectedLine(Graphics g, Pen pen,
                                   double x1, double y1, double z1,
                                   double x2, double y2, double z2,
                                   float cx, float cy, float scale,
                                   double cosX, double sinX, double cosY, double sinY)
    {
        PointF p1 = Project3D(x1, y1, z1, cx, cy, scale, cosX, sinX, cosY, sinY);
        PointF p2 = Project3D(x2, y2, z2, cx, cy, scale, cosX, sinX, cosY, sinY);
        g.DrawLine(pen, p1, p2);
    }

    private string GetXAxisStartLabel() => GetAxisLabel(_xLabels, 0, 0);

    private string GetXAxisEndLabel() => GetAxisLabel(_xLabels, Math.Max(0, _cols - 1), Math.Max(0, _cols - 1));

    private string GetYAxisStartLabel() => GetAxisLabel(_yLabels, 0, 0);

    private string GetYAxisEndLabel() => GetAxisLabel(_yLabels, Math.Max(0, _rows - 1), Math.Max(0, _rows - 1));

    private string GetZStartLabel() => FormatAxisValue(_minVal);

    private string GetZEndLabel() => FormatAxisValue(_maxVal);

    private static string GetAxisLabel(double[]? labels, int preferredIndex, int fallbackValue)
    {
        if (labels == null || labels.Length == 0)
            return fallbackValue.ToString();

        int safeIndex = Math.Clamp(preferredIndex, 0, labels.Length - 1);
        return FormatAxisValue(labels[safeIndex]);
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
            Project3D(1, -1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(1, 1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1, 1, -1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(1, -1, 1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY),
            Project3D(-1, 1, 1, cx, cy, scale, cosX, sinX, cosY, sinY),
        ];

        float minX = corners[0].X;
        float maxX = corners[0].X;
        float minY = corners[0].Y;
        float maxY = corners[0].Y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Math.Min(minX, corners[i].X);
            maxX = Math.Max(maxX, corners[i].X);
            minY = Math.Min(minY, corners[i].Y);
            maxY = Math.Max(maxY, corners[i].Y);
        }

        return RectangleF.FromLTRB(minX, minY, maxX, maxY);
    }

    private PointF Project3D(double x3, double y3, double z3,
                              float cx, float cy, float scale,
                              double cosX, double sinX, double cosY, double sinY)
    {
        double rx = x3 * cosY - z3 * sinY;
        double rz = x3 * sinY + z3 * cosY;
        double ry = y3 * cosX - rz * sinX;
        double rz2 = y3 * sinX + rz * cosX;

        double perspFactor = 1.0 / (1.0 + rz2 * Perspective * scale);
        float sx = cx + (float)(rx * scale * perspFactor);
        float sy = cy - (float)(ry * scale * perspFactor);
        return new PointF(sx, sy);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastMouse = e.Location;
            _mouseDownLocation = e.Location;
            _dragExceededClickThreshold = false;
            Cursor = Cursors.Hand;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDragging)
        {
            int dx = e.X - _lastMouse.X;
            int dy = e.Y - _lastMouse.Y;
            if (!_dragExceededClickThreshold)
            {
                int clickDx = e.X - _mouseDownLocation.X;
                int clickDy = e.Y - _mouseDownLocation.Y;
                _dragExceededClickThreshold = (clickDx * clickDx) + (clickDy * clickDy) > 25;
            }

            _rotY += dx * 0.5;
            _rotX += dy * 0.5;
            _rotX = Math.Clamp(_rotX, -89, 89);
            _lastMouse = e.Location;
            _projectedPoints = null;
            Invalidate();
        }
        else if (_hasData && _projectedPoints != null)
        {
            // Find nearest point for tooltip
            float bestDist = 20f * 20f; // 20px threshold
            int bestIdx = -1;
            for (int i = 0; i < _projectedPoints.Length; i++)
            {
                float dx = _projectedPoints[i].X - e.X;
                float dy = _projectedPoints[i].Y - e.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 < bestDist)
                {
                    bestDist = d2;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0 && bestIdx < _values.Length)
            {
                int r = bestIdx / _cols;
                int c = bestIdx % _cols;
                _tooltip.SetToolTip(this, $"[{r},{c}] = {_values[bestIdx]:G6}");
            }
            else
            {
                _tooltip.SetToolTip(this, string.Empty);
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            if (!_dragExceededClickThreshold && TryGetNearestPointIndex(e.Location, out int selectedIndex))
            {
                SelectPoint(selectedIndex);
                PointSelected?.Invoke(this, CreateEventArgs(selectedIndex));
            }

            _isDragging = false;
            Cursor = Cursors.SizeAll;
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left)
            return;

        if (!TryGetNearestPointIndex(e.Location, out int selectedIndex))
            return;

        SelectPoint(selectedIndex);
        PointActivated?.Invoke(this, CreateEventArgs(selectedIndex));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        double factor = e.Delta > 0 ? 1.12 : 0.89;
        _zoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        _projectedPoints = null;
        Invalidate();
    }

    private bool TryGetNearestPointIndex(Point location, out int index)
    {
        index = -1;
        if (!_hasData || _projectedPoints == null || _projectedPoints.Length == 0)
            return false;

        float bestDist = 14f * 14f;
        for (int i = 0; i < _projectedPoints.Length; i++)
        {
            float dx = _projectedPoints[i].X - location.X;
            float dy = _projectedPoints[i].Y - location.Y;
            float d2 = dx * dx + dy * dy;
            if (d2 < bestDist)
            {
                bestDist = d2;
                index = i;
            }
        }

        return index >= 0 && index < _values.Length;
    }

    private void SelectPoint(int index)
    {
        if (_selectedIndex == index)
            return;

        _selectedIndex = index;
        Invalidate();
    }

    private SurfacePointEventArgs CreateEventArgs(int index)
    {
        int row = index / _cols;
        int col = index % _cols;
        return new SurfacePointEventArgs(row, col, _values[index]);
    }
}
