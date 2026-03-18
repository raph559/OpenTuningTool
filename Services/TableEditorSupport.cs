using OpenTuningTool.Controls;
using OpenTuningTool.Models;
using System.Globalization;

namespace OpenTuningTool.Services;

internal static class TableEditorSupport
{
    public static bool TryReadTableValues(
        XdfDocument? document,
        BinBuffer? bin,
        XdfTable table,
        out double[] values,
        out string message)
    {
        values = Array.Empty<double>();

        if (table.ZAxis == null)
        {
            message = "No data address for this table.";
            return false;
        }

        if (document == null || bin == null)
        {
            message = "Open a BIN file (File -> Open BIN...) to view and edit values.";
            return false;
        }

        int absAddr = document.BaseOffset + table.ZAxis.Address;
        int byteCount = table.ZAxis.RowCount * table.ZAxis.ColCount * (table.ZAxis.ElementSizeBits / 8);
        if (!bin.IsAddressValid(absAddr, byteCount))
        {
            message = $"Address 0x{absAddr:X} is outside the BIN ({bin.Length:N0} bytes).";
            return false;
        }

        values = bin.ReadMap(
            absAddr,
            table.ZAxis.RowCount,
            table.ZAxis.ColCount,
            table.ZAxis.ElementSizeBits,
            table.ZAxis.Format);
        message = string.Empty;
        return true;
    }

    public static void PopulateMapGrid(
        DataGridView grid,
        XdfDocument? document,
        BinBuffer? bin,
        XdfTable table,
        double[] values)
    {
        int rows = table.ZAxis!.RowCount;
        int cols = table.ZAxis.ColCount;
        XdfValueFormat zFormat = table.ZAxis.Format;

        grid.SuspendLayout();
        grid.Rows.Clear();
        grid.Columns.Clear();
        grid.ReadOnly = !CanEditValue(zFormat, table.ZAxis.ElementSizeBits);

        double[]? xVals = TryReadAxisValues(document, bin, table, table.XAxis);
        for (int c = 0; c < cols; c++)
        {
            string header = GetAxisDisplayLabel(document, table, table.XAxis, c, xVals);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
            });
        }

        double[]? yVals = TryReadAxisValues(document, bin, table, table.YAxis);
        for (int r = 0; r < rows; r++)
        {
            string rowHeader = GetAxisDisplayLabel(document, table, table.YAxis, r, yVals);
            int idx = grid.Rows.Add();
            grid.Rows[idx].HeaderCell.Value = rowHeader;
            for (int c = 0; c < cols; c++)
                grid.Rows[idx].Cells[c].Value = FormatDisplayValue(values[r * cols + c], zFormat);
        }

        ResizeMapGridColumns(grid);
        grid.ResumeLayout();
    }

    public static void ResizeMapGridColumns(DataGridView grid)
    {
        if (grid.Columns.Count == 0)
        {
            grid.RowHeadersWidth = 60;
            return;
        }

        const int cellPadding = 20;
        const int headerPadding = 24;
        TextFormatFlags measureFlags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        Font headerFont = grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font;
        Font cellFont = grid.DefaultCellStyle.Font ?? grid.Font;
        Font rowHeaderFont = grid.RowHeadersDefaultCellStyle.Font ?? grid.Font;

        foreach (DataGridViewColumn column in grid.Columns)
        {
            int width = MeasureGridText(column.HeaderText, headerFont, measureFlags) + headerPadding;

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                DataGridViewCell cell = row.Cells[column.Index];
                string text = Convert.ToString(cell.FormattedValue ?? cell.Value, CultureInfo.CurrentCulture) ?? string.Empty;
                width = Math.Max(width, MeasureGridText(text, cellFont, measureFlags) + cellPadding);
            }

            column.Width = Math.Max(width, 56);
        }

        int rowHeaderWidth = 60;
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;

            string text = Convert.ToString(row.HeaderCell.FormattedValue ?? row.HeaderCell.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            rowHeaderWidth = Math.Max(rowHeaderWidth, MeasureGridText(text, rowHeaderFont, measureFlags) + cellPadding);
        }

        grid.RowHeadersWidth = rowHeaderWidth;
    }

    public static void LoadVisualizationViews(
        HeatmapView heatmapView,
        SurfacePlotView surfacePlotView,
        XdfDocument? document,
        BinBuffer? bin,
        XdfTable table,
        double[] values)
    {
        int rows = table.ZAxis!.RowCount;
        int cols = table.ZAxis.ColCount;
        double[]? xVals = TryReadAxisValues(document, bin, table, table.XAxis);
        double[]? yVals = TryReadAxisValues(document, bin, table, table.YAxis);
        string[] displayValues = new string[values.Length];
        for (int i = 0; i < values.Length; i++)
            displayValues[i] = FormatDisplayValue(values[i], table.ZAxis.Format);

        heatmapView.LoadData(values, rows, cols, xVals, yVals, displayValues);
        surfacePlotView.LoadData(values, rows, cols, xVals, yVals);
    }

    public static double[]? TryReadAxisValues(
        XdfDocument? document,
        BinBuffer? bin,
        XdfTable? selectedTable,
        XdfAxis? axis)
    {
        if (bin == null || document == null || axis == null)
            return null;

        XdfTable? breakpointTable = TryResolveAxisBreakpointTable(document, selectedTable, axis);
        if (breakpointTable?.ZAxis != null)
        {
            XdfTableData zAxis = breakpointTable.ZAxis;
            int absAddr = document.BaseOffset + zAxis.Address;
            int valueCount = zAxis.RowCount * zAxis.ColCount;
            int byteCount = valueCount * (zAxis.ElementSizeBits / 8);
            if (!bin.IsAddressValid(absAddr, byteCount))
                return null;

            double[] values = bin.ReadMap(absAddr, zAxis.RowCount, zAxis.ColCount, zAxis.ElementSizeBits, zAxis.Format);
            if (values.Length == axis.IndexCount)
                return values;

            if (values.Length > axis.IndexCount)
                return values.Take(axis.IndexCount).ToArray();

            return null;
        }

        if (!axis.Address.HasValue)
            return null;

        int axisAbsAddr = document.BaseOffset + axis.Address.Value;
        int axisByteCount = axis.IndexCount * (axis.ElementSizeBits / 8);
        if (!bin.IsAddressValid(axisAbsAddr, axisByteCount))
            return null;

        return bin.ReadMap(axisAbsAddr, 1, axis.IndexCount, axis.ElementSizeBits, axis.Format);
    }

    public static XdfTable? TryResolveAxisBreakpointTable(XdfDocument? document, XdfTable? selectedTable, XdfAxis axis)
    {
        if (document == null || !axis.Address.HasValue)
            return null;

        int rawAddress = axis.Address.Value;

        XdfTable? exactMatch = document.Tables.FirstOrDefault(table =>
            !ReferenceEquals(table, selectedTable) &&
            table.ZAxis != null &&
            table.ZAxis.Address == rawAddress &&
            table.ZAxis.RowCount * table.ZAxis.ColCount == axis.IndexCount);

        if (exactMatch != null)
            return exactMatch;

        return document.Tables.FirstOrDefault(table =>
            !ReferenceEquals(table, selectedTable) &&
            table.ZAxis != null &&
            table.ZAxis.Address == rawAddress);
    }

    public static string GetAxisDisplayLabel(
        XdfDocument? document,
        XdfTable? selectedTable,
        XdfAxis? axis,
        int index,
        double[]? values)
    {
        XdfValueFormat displayFormat = axis != null
            ? TryResolveAxisBreakpointTable(document, selectedTable, axis)?.ZAxis?.Format ?? axis.Format
            : XdfValueFormat.Identity;

        if (axis != null && values != null && index < values.Length)
            return FormatDisplayValue(values[index], displayFormat);

        if (axis != null && axis.Labels.TryGetValue(index, out string? label))
            return label;

        return index.ToString(CultureInfo.CurrentCulture);
    }

    public static string GetEndianLabel(XdfValueFormat format) =>
        format.IsLittleEndian ? "little-endian" : "big-endian";

    public static bool CanEditValue(XdfValueFormat format, int elementSizeBits)
    {
        if ((format.OutputType ?? 0) == 4)
            return false;

        if (!XdfEquationEvaluator.IsSupported(format.MathEquation))
            return false;

        if (XdfEquationEvaluator.IsIdentity(format.MathEquation))
            return true;

        if (format.IsFloatingPoint)
            return false;

        return elementSizeBits is 8 or 16;
    }

    public static bool TryConvertDisplayToRaw(double displayValue, XdfValueFormat format, int elementSizeBits, out double rawValue)
    {
        if (XdfEquationEvaluator.IsIdentity(format.MathEquation))
        {
            rawValue = displayValue;
            return true;
        }

        if (format.IsFloatingPoint)
        {
            rawValue = 0;
            return false;
        }

        return XdfEquationEvaluator.TryInvertDiscrete(
            format.MathEquation,
            displayValue,
            elementSizeBits,
            format.IsSigned,
            out rawValue);
    }

    public static bool TryParseDisplayValue(string? text, XdfValueFormat format, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();
        if ((format.OutputType ?? 0) == 3)
        {
            string hexText = trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? trimmed[2..]
                : trimmed;

            if (long.TryParse(hexText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue))
            {
                value = hexValue;
                return true;
            }
        }

        return double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands,
                   CultureInfo.CurrentCulture, out value) ||
               double.TryParse(trimmed, NumberStyles.Float | NumberStyles.AllowThousands,
                   CultureInfo.InvariantCulture, out value);
    }

    public static string FormatDisplayValue(double value, XdfValueFormat format)
    {
        int outputType = format.OutputType ?? 2;
        if (outputType == 3 && Math.Abs(value) <= long.MaxValue)
            return $"0x{(long)Math.Round(value):X}";

        int? decimalPlaces = format.DecimalPlaces;
        if (decimalPlaces.HasValue)
        {
            int places = Math.Clamp(decimalPlaces.Value, 0, 10);
            double rounded = Math.Round(value, places, MidpointRounding.AwayFromZero);
            return rounded.ToString($"F{places}", CultureInfo.CurrentCulture);
        }

        if (outputType == 1)
            return value.ToString("G6", CultureInfo.CurrentCulture);

        if (Math.Abs(value - Math.Round(value)) < 0.000001 && Math.Abs(value) <= long.MaxValue)
            return ((long)Math.Round(value)).ToString(CultureInfo.CurrentCulture);

        return value.ToString("G6", CultureInfo.CurrentCulture);
    }

    private static int MeasureGridText(string? text, Font font, TextFormatFlags flags)
    {
        string measureText = string.IsNullOrEmpty(text) ? " " : text;
        return TextRenderer.MeasureText(measureText, font, Size.Empty, flags).Width;
    }
}
