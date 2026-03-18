namespace OpenTuningTool.Models;

/// <summary>
/// Wraps a mutable byte array representing a raw ECU BIN file.
/// Provides typed reads and writes using XDF type flags and math equations.
/// </summary>
public class BinBuffer
{
    private readonly byte[] _data;

    public bool IsDirty { get; private set; }
    public int Length => _data.Length;

    private BinBuffer(byte[] data) { _data = data; }

    public static BinBuffer Load(string path) => new BinBuffer(File.ReadAllBytes(path));

    public void Save(string path)
    {
        File.WriteAllBytes(path, _data);
        IsDirty = false;
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    public bool IsAddressValid(int offset, int byteCount) =>
        offset >= 0 && byteCount >= 0 && offset + byteCount <= _data.Length;

    // -----------------------------------------------------------------------
    // Read a flat array of numeric values
    // -----------------------------------------------------------------------

    public double[] ReadMap(int offset, int rows, int cols, int elementSizeBits, XdfValueFormat format) =>
        ReadMap(offset, rows, cols, elementSizeBits, format.TypeFlags, format.MathEquation);

    public double[] ReadMap(int offset, int rows, int cols, int elementSizeBits, int typeFlags, string? mathEquation = null)
    {
        int elemBytes = elementSizeBits / 8;
        int count = rows * cols;
        var result = new double[count];

        for (int i = 0; i < count; i++)
        {
            double rawValue = ReadElement(offset + i * elemBytes, elementSizeBits, typeFlags);
            result[i] = XdfEquationEvaluator.TryEvaluate(mathEquation, rawValue, out double convertedValue)
                ? convertedValue
                : rawValue;
        }

        return result;
    }

    public double[] ReadMap(int offset, int rows, int cols, int elementSizeBits, bool bigEndian)
    {
        int typeFlags = bigEndian ? 0 : XdfValueFormat.LittleEndianFlag;
        return ReadMap(offset, rows, cols, elementSizeBits, typeFlags);
    }

    // -----------------------------------------------------------------------
    // Write a single cell back (called from cell-edit events)
    // -----------------------------------------------------------------------

    public void WriteCell(int offset, int elementSizeBits, XdfValueFormat format, double value)
    {
        WriteCell(offset, elementSizeBits, format.TypeFlags, value);
    }

    public void WriteCell(int offset, int elementSizeBits, int typeFlags, double value)
    {
        WriteElement(offset, elementSizeBits, typeFlags, value);
        IsDirty = true;
    }

    public void WriteCell(int offset, int elementSizeBits, bool bigEndian, double value)
    {
        int typeFlags = bigEndian ? 0 : XdfValueFormat.LittleEndianFlag;
        WriteCell(offset, elementSizeBits, typeFlags, value);
    }

    // -----------------------------------------------------------------------
    // Internal element read/write
    // -----------------------------------------------------------------------

    private double ReadElement(int pos, int bits, int typeFlags)
    {
        bool littleEndian = (typeFlags & XdfValueFormat.LittleEndianFlag) != 0;
        bool floatingPoint = (typeFlags & XdfValueFormat.FloatingPointFlag) != 0;
        bool signed = !floatingPoint && (typeFlags & XdfValueFormat.SignedFlag) != 0;

        return bits switch
        {
            8 => signed ? (sbyte)_data[pos] : _data[pos],
            16 => Read16(pos, littleEndian, signed),
            32 => Read32(pos, littleEndian, signed, floatingPoint),
            _  => 0.0
        };
    }

    private double Read16(int pos, bool littleEndian, bool signed)
    {
        ushort raw = littleEndian
            ? (ushort)(_data[pos] | (_data[pos + 1] << 8))
            : (ushort)((_data[pos] << 8) | _data[pos + 1]);

        return signed ? (short)raw : raw;
    }

    private double Read32(int pos, bool littleEndian, bool signed, bool floatingPoint)
    {
        uint raw = littleEndian
            ? (uint)(_data[pos] | (_data[pos + 1] << 8) | (_data[pos + 2] << 16) | (_data[pos + 3] << 24))
            : ((uint)_data[pos] << 24) | ((uint)_data[pos + 1] << 16) | ((uint)_data[pos + 2] << 8) | _data[pos + 3];

        if (floatingPoint)
            return BitConverter.Int32BitsToSingle(unchecked((int)raw));

        return signed ? unchecked((int)raw) : raw;
    }

    private void WriteElement(int pos, int bits, int typeFlags, double value)
    {
        bool littleEndian = (typeFlags & XdfValueFormat.LittleEndianFlag) != 0;
        bool floatingPoint = (typeFlags & XdfValueFormat.FloatingPointFlag) != 0;
        bool signed = !floatingPoint && (typeFlags & XdfValueFormat.SignedFlag) != 0;

        switch (bits)
        {
            case 8:
                Write8(pos, value, signed);
                break;
            case 16:
                Write16(pos, value, signed, littleEndian);
                break;
            case 32:
                Write32(pos, value, signed, littleEndian, floatingPoint);
                break;
        }
    }

    private void Write8(int pos, double value, bool signed)
    {
        if (signed)
        {
            double clamped = Math.Clamp(Math.Round(value), sbyte.MinValue, sbyte.MaxValue);
            _data[pos] = unchecked((byte)(sbyte)clamped);
            return;
        }

        _data[pos] = (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
    }

    private void Write16(int pos, double value, bool signed, bool littleEndian)
    {
        ushort raw;
        if (signed)
        {
            double clamped = Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
            raw = unchecked((ushort)(short)clamped);
        }
        else
        {
            raw = (ushort)Math.Clamp(Math.Round(value), ushort.MinValue, ushort.MaxValue);
        }

        if (littleEndian)
        {
            _data[pos] = (byte)(raw & 0xFF);
            _data[pos + 1] = (byte)(raw >> 8);
        }
        else
        {
            _data[pos] = (byte)(raw >> 8);
            _data[pos + 1] = (byte)(raw & 0xFF);
        }
    }

    private void Write32(int pos, double value, bool signed, bool littleEndian, bool floatingPoint)
    {
        uint raw;
        if (floatingPoint)
        {
            raw = unchecked((uint)BitConverter.SingleToInt32Bits((float)value));
        }
        else if (signed)
        {
            double clamped = Math.Clamp(Math.Round(value), int.MinValue, int.MaxValue);
            raw = unchecked((uint)(int)clamped);
        }
        else
        {
            double clamped = Math.Clamp(Math.Round(value), 0d, uint.MaxValue);
            raw = (uint)clamped;
        }

        if (littleEndian)
        {
            _data[pos] = (byte)(raw & 0xFF);
            _data[pos + 1] = (byte)((raw >> 8) & 0xFF);
            _data[pos + 2] = (byte)((raw >> 16) & 0xFF);
            _data[pos + 3] = (byte)((raw >> 24) & 0xFF);
        }
        else
        {
            _data[pos] = (byte)((raw >> 24) & 0xFF);
            _data[pos + 1] = (byte)((raw >> 16) & 0xFF);
            _data[pos + 2] = (byte)((raw >> 8) & 0xFF);
            _data[pos + 3] = (byte)(raw & 0xFF);
        }
    }
}
