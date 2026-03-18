using System.Drawing.Drawing2D;

namespace OpenTuningTool.Controls;

public sealed class FlatTabControl : TabControl
{
    private Color _bgColor = Color.FromArgb(30, 30, 30);
    private Color _surfaceColor = Color.FromArgb(37, 37, 38);
    private Color _fgColor = Color.FromArgb(220, 220, 220);
    private Color _accentColor = Color.FromArgb(0, 122, 204);
    private Color _mutedColor = Color.FromArgb(150, 150, 150);

    public FlatTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.OptimizedDoubleBuffer, true);
        DrawMode = TabDrawMode.OwnerDrawFixed;
        ItemSize = new Size(80, 30);
        SizeMode = TabSizeMode.Fixed;
        Padding = new Point(16, 4);
    }

    public void ApplyThemeColors(Color bg, Color surface, Color fg, Color accent, Color muted)
    {
        _bgColor = bg;
        _surfaceColor = surface;
        _fgColor = fg;
        _accentColor = accent;
        _mutedColor = muted;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Background
        using var bgBrush = new SolidBrush(_bgColor);
        g.FillRectangle(bgBrush, ClientRectangle);

        if (TabCount == 0) return;

        // Tab strip background
        Rectangle stripRect = new(0, 0, Width, ItemSize.Height);
        using var stripBrush = new SolidBrush(_surfaceColor);
        g.FillRectangle(stripBrush, stripRect);

        // Bottom separator line
        using var sepPen = new Pen(Color.FromArgb(50, _fgColor), 1f);
        g.DrawLine(sepPen, 0, ItemSize.Height, Width, ItemSize.Height);

        // Draw each tab
        for (int i = 0; i < TabCount; i++)
        {
            Rectangle tabRect = GetTabRect(i);
            bool selected = i == SelectedIndex;

            // Tab text
            Color textColor = selected ? _fgColor : _mutedColor;
            using var textBrush = new SolidBrush(textColor);
            var font = selected
                ? new Font("Segoe UI", 9f, FontStyle.Bold)
                : new Font("Segoe UI", 9f, FontStyle.Regular);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(TabPages[i].Text, font, textBrush, tabRect, sf);
            font.Dispose();

            // Accent underline for selected tab
            if (selected)
            {
                using var accentPen = new Pen(_accentColor, 2.5f);
                int y = tabRect.Bottom - 1;
                g.DrawLine(accentPen, tabRect.Left + 4, y, tabRect.Right - 4, y);
            }
        }

        // Fill tab page area below the strip
        Rectangle pageRect = new(0, ItemSize.Height + 1, Width, Height - ItemSize.Height - 1);
        g.FillRectangle(bgBrush, pageRect);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Suppress default background to eliminate gray border
    }

    protected override void CreateHandle()
    {
        base.CreateHandle();
        // Remove visual styles border
        Appearance = TabAppearance.Normal;
    }
}
