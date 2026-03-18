using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace OpenTuningTool.Controls;

public sealed class ModernSearchBox : UserControl
{
    private readonly TextBox _innerTextBox;
    private Color _bgColor = Color.FromArgb(45, 45, 48);
    private Color _fgColor = Color.FromArgb(220, 220, 220);
    private Color _borderColor = Color.FromArgb(70, 70, 74);
    private Color _iconColor = Color.FromArgb(150, 150, 150);
    private Color _placeholderColor = Color.FromArgb(120, 120, 120);
    private string _placeholder = "Search...";

    public ModernSearchBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);

        _innerTextBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = _bgColor,
            ForeColor = _fgColor,
            Font = new Font("Segoe UI", 9f),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _innerTextBox.TextChanged += (_, _) => OnSearchTextChanged();
        _innerTextBox.GotFocus += (_, _) => Invalidate();
        _innerTextBox.LostFocus += (_, _) => Invalidate();

        Controls.Add(_innerTextBox);
        Height = 28;
        UpdateTextBoxBounds();
    }

    public event EventHandler? SearchTextChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string Text
    {
        get => _innerTextBox.Text;
        set => _innerTextBox.Text = value;
    }

    public void ApplyThemeColors(Color bg, Color fg, Color border, Color icon)
    {
        _bgColor = bg;
        _fgColor = fg;
        _borderColor = border;
        _iconColor = icon;
        _placeholderColor = Color.FromArgb(
            (fg.R + bg.R) / 2,
            (fg.G + bg.G) / 2,
            (fg.B + bg.B) / 2);

        _innerTextBox.BackColor = bg;
        _innerTextBox.ForeColor = fg;
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateTextBoxBounds();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Background with rounded rect
        Rectangle bounds = new(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedRect(bounds, 6);
        using var bgBrush = new SolidBrush(_bgColor);
        g.FillPath(bgBrush, path);

        // Border
        Color borderColor = _innerTextBox.Focused
            ? Color.FromArgb(0, 122, 204)
            : _borderColor;
        using var borderPen = new Pen(borderColor, 1f);
        g.DrawPath(borderPen, path);

        // Magnifying glass icon (left side)
        int iconSize = 12;
        int iconX = 8;
        int iconY = (Height - iconSize) / 2;

        using var iconPen = new Pen(_iconColor, 1.5f);
        // Circle
        g.DrawEllipse(iconPen, iconX, iconY, iconSize - 3, iconSize - 3);
        // Handle
        g.DrawLine(iconPen, iconX + iconSize - 4, iconY + iconSize - 4,
                   iconX + iconSize, iconY + iconSize);

        // Placeholder text when empty and not focused
        if (string.IsNullOrEmpty(_innerTextBox.Text) && !_innerTextBox.Focused)
        {
            using var placeholderBrush = new SolidBrush(_placeholderColor);
            var textRect = new RectangleF(26, (Height - Font.Height) / 2f, Width - 32, Height);
            g.DrawString(_placeholder, new Font("Segoe UI", 9f, FontStyle.Italic),
                         placeholderBrush, textRect);
        }
    }

    private void UpdateTextBoxBounds()
    {
        int leftPadding = 26; // space for icon
        int topPadding = (Height - _innerTextBox.PreferredHeight) / 2;
        int textBoxWidth = Math.Max(0, Width - leftPadding - 8);
        _innerTextBox.SetBounds(leftPadding, topPadding, textBoxWidth, _innerTextBox.PreferredHeight);
    }

    private void OnSearchTextChanged()
    {
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
