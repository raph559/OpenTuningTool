using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTuningTool.Models;

namespace OpenTuningTool;

public static class ThemeUtility
{
    private readonly struct ThemePalette
    {
        public ThemePalette(
            Color window,
            Color surface,
            Color input,
            Color foreground,
            Color mutedForeground,
            Color accent,
            Color grid,
            Color selection)
        {
            Window = window;
            Surface = surface;
            Input = input;
            Foreground = foreground;
            MutedForeground = mutedForeground;
            Accent = accent;
            Grid = grid;
            Selection = selection;
        }

        public Color Window { get; }

        public Color Surface { get; }

        public Color Input { get; }

        public Color Foreground { get; }

        public Color MutedForeground { get; }

        public Color Accent { get; }

        public Color Grid { get; }

        public Color Selection { get; }
    }

    private static readonly ConditionalWeakTable<Control, Font> BaseFonts = new();
    private static readonly ConditionalWeakTable<ComboBox, ComboBoxThemeState> ComboBoxThemes = new();

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO pcbi);

    public static void ApplyTheme(Form form, AppTheme theme)
    {
        ThemePalette palette = GetPalette(theme);

        ApplyTitleBarTheme(form, theme);
        ApplyScrollBarTheme(form, theme);
        ApplyControlThemeRecursive(form, palette);
        ApplyMenuTheme(form.MainMenuStrip, palette);

        form.Invalidate(true);
        form.Refresh();
    }

    public static void ApplyDarkTitleBar(Form form) => ApplyTitleBarTheme(form, AppTheme.Dark);

    public static void ApplyDarkScrollBars(Control control) => ApplyScrollBarTheme(control, AppTheme.Dark);

    public static void ApplyUiDensity(Control root, UiDensity density)
    {
        float scale = density switch
        {
            UiDensity.Compact => 0.92f,
            UiDensity.Spacious => 1.08f,
            _ => 1.00f,
        };

        ApplyUiDensityRecursive(root, scale);
    }

    private static ThemePalette GetPalette(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => new ThemePalette(
                window: Color.FromArgb(246, 247, 249),
                surface: Color.FromArgb(255, 255, 255),
                input: Color.FromArgb(240, 242, 245),
                foreground: Color.FromArgb(32, 32, 32),
                mutedForeground: Color.FromArgb(96, 96, 96),
                accent: Color.FromArgb(0, 102, 204),
                grid: Color.FromArgb(218, 220, 224),
                selection: Color.FromArgb(179, 215, 255)),
            _ => new ThemePalette(
                window: Color.FromArgb(30, 30, 30),
                surface: Color.FromArgb(37, 37, 38),
                input: Color.FromArgb(45, 45, 48),
                foreground: Color.FromArgb(220, 220, 220),
                mutedForeground: Color.FromArgb(150, 150, 150),
                accent: Color.FromArgb(0, 122, 204),
                grid: Color.FromArgb(60, 60, 60),
                selection: Color.FromArgb(0, 122, 204)),
        };
    }

    private static void ApplyTitleBarTheme(Form form, AppTheme theme)
    {
        try
        {
            if (Environment.OSVersion.Version.Major < 10)
                return;

            int dark = theme == AppTheme.Dark ? 1 : 0;

            int result = DwmSetWindowAttribute(form.Handle, 19, ref dark, sizeof(int));
            if (result != 0)
                DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
        }
        catch
        {
            // Ignore titlebar theming failures on unsupported systems.
        }
    }

    private static void ApplyScrollBarTheme(Control control, AppTheme theme)
    {
        try
        {
            if (Environment.OSVersion.Version.Major < 10)
                return;

            string scrollbarTheme = theme == AppTheme.Dark ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(control.Handle, scrollbarTheme, null);

            foreach (Control child in control.Controls)
                ApplyScrollBarTheme(child, theme);
        }
        catch
        {
            // Ignore scrollbar theming failures for controls that do not support this API.
        }
    }

    private static void ApplyControlThemeRecursive(Control control, ThemePalette palette)
    {
        try
        {
            switch (control)
            {
                case Form form:
                    form.BackColor = palette.Window;
                    form.ForeColor = palette.Foreground;
                    break;
                case MenuStrip menuStrip:
                    menuStrip.BackColor = palette.Surface;
                    menuStrip.ForeColor = palette.Foreground;
                    break;
                case StatusStrip statusStrip:
                    statusStrip.BackColor = palette.Surface;
                    statusStrip.ForeColor = palette.Foreground;
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        item.BackColor = palette.Surface;
                        item.ForeColor = palette.Foreground;
                    }
                    break;
                case SplitContainer splitContainer:
                    splitContainer.BackColor = palette.Surface;
                    splitContainer.Panel1.BackColor = palette.Surface;
                    splitContainer.Panel2.BackColor = palette.Window;
                    break;
                case SplitterPanel splitterPanel:
                    if (splitterPanel.Parent is SplitContainer parentSplit)
                        splitterPanel.BackColor = ReferenceEquals(splitterPanel, parentSplit.Panel1)
                            ? palette.Surface
                            : palette.Window;
                    else
                        splitterPanel.BackColor = palette.Surface;

                    splitterPanel.ForeColor = palette.Foreground;
                    break;
                case TabControl tabControl:
                    tabControl.BackColor = palette.Surface;
                    tabControl.ForeColor = palette.Foreground;
                    break;
                case TabPage tabPage:
                    tabPage.BackColor = palette.Window;
                    tabPage.ForeColor = palette.Foreground;
                    break;
                case DataGridView dataGridView:
                    ApplyGridTheme(dataGridView, palette);
                    break;
                case TreeView treeView:
                    treeView.BackColor = palette.Surface;
                    treeView.ForeColor = palette.Foreground;
                    treeView.LineColor = palette.Foreground;
                    break;
                case ListView listView:
                    listView.BackColor = palette.Surface;
                    listView.ForeColor = palette.Foreground;
                    break;
                case TextBox textBox:
                    textBox.BackColor = palette.Input;
                    textBox.ForeColor = palette.Foreground;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case NumericUpDown numericUpDown:
                    numericUpDown.BackColor = palette.Input;
                    numericUpDown.ForeColor = palette.Foreground;
                    break;
                case ComboBox comboBox:
                    ApplyComboBoxTheme(comboBox, palette);
                    break;
                case Button button:
                    StyleButton(button, palette);
                    break;
                case Label label:
                    ApplyLabelColor(label, palette);
                    break;
                case CheckBox checkBox:
                    checkBox.ForeColor = palette.Foreground;
                    break;
                case RadioButton radioButton:
                    radioButton.ForeColor = palette.Foreground;
                    break;
                default:
                    control.BackColor = palette.Window;
                    control.ForeColor = palette.Foreground;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyControlThemeRecursive(child, palette);
        }
        catch
        {
            // Ignore per-control failures to keep UI functional.
        }
    }

    private static void ApplyMenuTheme(MenuStrip? menuStrip, ThemePalette palette)
    {
        if (menuStrip == null)
            return;

        var renderer = new ToolStripProfessionalRenderer(new ThemedColorTable(palette));

        menuStrip.BackColor = palette.Surface;
        menuStrip.ForeColor = palette.Foreground;
        menuStrip.RenderMode = ToolStripRenderMode.Professional;
        menuStrip.Renderer = renderer;

        foreach (ToolStripItem item in menuStrip.Items)
            ApplyMenuItemTheme(item, palette);
    }

    private static void ApplyMenuItemTheme(ToolStripItem item, ThemePalette palette)
    {
        item.BackColor = palette.Surface;
        item.ForeColor = palette.Foreground;

        if (item is not ToolStripMenuItem menuItem)
            return;

        if (menuItem.DropDown is ToolStripDropDownMenu dropDown)
        {
            dropDown.Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable(palette));
            dropDown.BackColor = palette.Surface;
            dropDown.ForeColor = palette.Foreground;
            dropDown.ShowImageMargin = false;
        }

        foreach (ToolStripItem dropDownItem in menuItem.DropDownItems)
            ApplyMenuItemTheme(dropDownItem, palette);
    }

    private static void ApplyGridTheme(DataGridView grid, ThemePalette palette)
    {
        grid.BackgroundColor = palette.Window;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = palette.Grid;
        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = palette.Surface;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Foreground;
        grid.RowHeadersDefaultCellStyle.BackColor = palette.Surface;
        grid.RowHeadersDefaultCellStyle.ForeColor = palette.Foreground;

        grid.DefaultCellStyle.BackColor = palette.Input;
        grid.DefaultCellStyle.ForeColor = palette.Foreground;
        grid.DefaultCellStyle.SelectionBackColor = palette.Selection;
        grid.DefaultCellStyle.SelectionForeColor = palette.Foreground;

        grid.AlternatingRowsDefaultCellStyle.BackColor = Blend(palette.Input, palette.Window, 0.07f);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = palette.Foreground;
    }

    private static void ApplyLabelColor(Label label, ThemePalette palette)
    {
        if (label.Name.Contains("DetailTitle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(label.Text, "Settings", StringComparison.OrdinalIgnoreCase))
        {
            label.ForeColor = palette.Accent;
            return;
        }

        if (label.Font.Italic)
            label.ForeColor = palette.MutedForeground;
        else
            label.ForeColor = palette.Foreground;

        if (label.BackColor != Color.Transparent)
            label.BackColor = label.Parent is Panel ? palette.Surface : palette.Window;
    }

    private static void StyleButton(Button button, ThemePalette palette)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;

        bool primary = IsPrimaryActionButton(button);
        button.BackColor = primary ? palette.Accent : palette.Input;
        button.ForeColor = primary ? Color.White : palette.Foreground;
    }

    private static bool IsPrimaryActionButton(Button button)
    {
        string text = button.Text.Trim();
        string name = button.Name;

        if (name.Contains("Apply", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Save", StringComparison.OrdinalIgnoreCase))
            return true;

        return text.Equals("Save", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Apply", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("Accept Selected", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyComboBoxTheme(ComboBox comboBox, ThemePalette palette)
    {
        bool isDarkTheme = palette.Window.GetBrightness() < 0.50f;

        comboBox.BackColor = palette.Input;
        comboBox.ForeColor = palette.Foreground;
        comboBox.FlatStyle = FlatStyle.Standard;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        comboBox.IntegralHeight = false;

        if (comboBox.ItemHeight < 20)
            comboBox.ItemHeight = 20;

        if (!ComboBoxThemes.TryGetValue(comboBox, out ComboBoxThemeState? state))
        {
            state = new ComboBoxThemeState(palette);
            ComboBoxThemes.Add(comboBox, state);
            comboBox.DrawItem += ComboBox_DrawItem;
        }
        else
        {
            state.Palette = palette;
        }

        ApplyNativeComboTheme(comboBox, isDarkTheme);
        comboBox.Invalidate();
    }

    private static void ApplyNativeComboTheme(ComboBox comboBox, bool isDarkTheme)
    {
        try
        {
            string comboTheme = isDarkTheme ? "DarkMode_CFD" : "CFD";
            string listTheme = isDarkTheme ? "DarkMode_Explorer" : "Explorer";

            _ = comboBox.Handle;
            SetWindowTheme(comboBox.Handle, comboTheme, null);

            var info = new COMBOBOXINFO
            {
                cbSize = Marshal.SizeOf<COMBOBOXINFO>()
            };

            if (!GetComboBoxInfo(comboBox.Handle, ref info))
                return;

            if (info.hwndEdit != IntPtr.Zero)
                SetWindowTheme(info.hwndEdit, listTheme, null);

            if (info.hwndList != IntPtr.Zero)
                SetWindowTheme(info.hwndList, listTheme, null);
        }
        catch
        {
            // Ignore native theming failures on unsupported systems.
        }
    }

    private static void ComboBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;

        if (!ComboBoxThemes.TryGetValue(comboBox, out ComboBoxThemeState? state))
            return;

        if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
            return;

        ThemePalette palette = state.Palette;
        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using var backBrush = new SolidBrush(selected
            ? Blend(palette.Accent, palette.Surface, 0.65f)
            : palette.Input);
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        string text = string.Empty;
        if (e.Index >= 0 && e.Index < comboBox.Items.Count)
        {
            text = comboBox.GetItemText(comboBox.Items[e.Index]) ?? string.Empty;
        }
        else if (comboBox.SelectedIndex >= 0 && comboBox.SelectedIndex < comboBox.Items.Count)
        {
            text = comboBox.GetItemText(comboBox.Items[comboBox.SelectedIndex]) ?? string.Empty;
        }

        Rectangle textRect = Rectangle.Inflate(e.Bounds, -4, 0);
        TextRenderer.DrawText(
            e.Graphics,
            text,
            comboBox.Font,
            textRect,
            palette.Foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            e.DrawFocusRectangle();
    }

    private static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        int r = (int)(a.R + ((b.R - a.R) * t));
        int g = (int)(a.G + ((b.G - a.G) * t));
        int bVal = (int)(a.B + ((b.B - a.B) * t));
        return Color.FromArgb(r, g, bVal);
    }

    private sealed class ComboBoxThemeState
    {
        public ComboBoxThemeState(ThemePalette palette)
        {
            Palette = palette;
        }

        public ThemePalette Palette { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;

        public int top;

        public int right;

        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COMBOBOXINFO
    {
        public int cbSize;

        public RECT rcItem;

        public RECT rcButton;

        public int stateButton;

        public IntPtr hwndCombo;

        public IntPtr hwndItem;

        public IntPtr hwndList;

        public IntPtr hwndEdit;
    }

    private sealed class ThemedColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _palette;

        public ThemedColorTable(ThemePalette palette)
        {
            _palette = palette;
            UseSystemColors = false;
        }

        public override Color MenuBorder => _palette.Grid;

        public override Color MenuItemBorder => _palette.Accent;

        public override Color MenuItemSelected => Blend(_palette.Accent, _palette.Surface, 0.65f);

        public override Color MenuItemSelectedGradientBegin => Blend(_palette.Accent, _palette.Surface, 0.62f);

        public override Color MenuItemSelectedGradientEnd => Blend(_palette.Accent, _palette.Surface, 0.72f);

        public override Color MenuItemPressedGradientBegin => _palette.Input;

        public override Color MenuItemPressedGradientMiddle => _palette.Input;

        public override Color MenuItemPressedGradientEnd => _palette.Input;

        public override Color ToolStripDropDownBackground => _palette.Surface;

        public override Color ImageMarginGradientBegin => _palette.Surface;

        public override Color ImageMarginGradientMiddle => _palette.Surface;

        public override Color ImageMarginGradientEnd => _palette.Surface;

        public override Color SeparatorDark => _palette.Grid;

        public override Color SeparatorLight => _palette.Grid;

        public override Color ToolStripBorder => _palette.Grid;

        public override Color ToolStripGradientBegin => _palette.Surface;

        public override Color ToolStripGradientMiddle => _palette.Surface;

        public override Color ToolStripGradientEnd => _palette.Surface;
    }

    private static void ApplyUiDensityRecursive(Control control, float scale)
    {
        try
        {
            if (!BaseFonts.TryGetValue(control, out Font? baseFont))
            {
                baseFont = control.Font;
                BaseFonts.Add(control, baseFont);
            }

            float newSize = Math.Max(7.0f, baseFont.Size * scale);
            control.Font = new Font(baseFont.FontFamily, newSize, baseFont.Style, baseFont.Unit);

            if (control is DataGridView grid)
            {
                int rowHeight = (int)Math.Round(22 * scale);
                grid.RowTemplate.Height = Math.Max(18, rowHeight);
                grid.ColumnHeadersHeight = Math.Max(22, (int)Math.Round(26 * scale));
            }

            foreach (Control child in control.Controls)
                ApplyUiDensityRecursive(child, scale);
        }
        catch
        {
            // Ignore control-level font errors to avoid destabilizing the UI.
        }
    }
}
