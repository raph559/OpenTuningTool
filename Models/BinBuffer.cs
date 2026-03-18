namespace OpenTuningTool.Models;

/// <summary>
/// Wraps a mutable byte array representing a raw ECU BIN file.
/// Provides typed reads and writes with big- or little-endian support.
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

    public double[] ReadMap(int offset, int rows, int cols, int elementSizeBits, bool bigEndian)
    {
        int elemBytes = elementSizeBits / 8;
        int count = rows * cols;
        var result = new double[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadElement(offset + i * elemBytes, elementSizeBits, bigEndian);
        return result;
    }

    // -----------------------------------------------------------------------
    // Write a single cell back (called from cell-edit events)
    // -----------------------------------------------------------------------

    public void WriteCell(int offset, int elementSizeBits, bool bigEndian, double value)
    {
        WriteElement(offset, elementSizeBits, bigEndian, value);
        IsDirty = true;
    }

    // -----------------------------------------------------------------------
    // Internal element read/write
    // -----------------------------------------------------------------------

    private double ReadElement(int pos, int bits, bool bigEndian)
    {
        return bits switch
        {
            8  => _data[pos],
            16 => bigEndian
                ? (double)((_data[pos] << 8) | _data[pos + 1])
                : (double)(_data[pos] | (_data[pos + 1] << 8)),
            32 => bigEndian
                ? (double)(((uint)_data[pos] << 24) | ((uint)_data[pos + 1] << 16)
                           | ((uint)_data[pos + 2] << 8) | _data[pos + 3])
                : (double)((uint)_data[pos] | ((uint)_data[pos + 1] << 8)
                           | ((uint)_data[pos + 2] << 16) | ((uint)_data[pos + 3] << 24)),
            _  => 0.0
        };
    }

    private void WriteElement(int pos, int bits, bool bigEndian, double value)
    {
        long clamped = bits switch
        {
            8  => (long)Math.Clamp(Math.Round(value), 0, 255),
            16 => (long)Math.Clamp(Math.Round(value), 0, 65535),
            32 => (long)Math.Clamp(Math.Round(value), 0, uint.MaxValue),
            _  => 0
        };

        switch (bits)
        {
            case 8:
                _data[pos] = (byte)clamped;
                break;
            case 16:
                ushort u16 = (ushort)clamped;
                if (bigEndian) { _data[pos] = (byte)(u16 >> 8); _data[pos + 1] = (byte)(u16 & 0xFF); }
                else           { _data[pos] = (byte)(u16 & 0xFF); _data[pos + 1] = (byte)(u16 >> 8); }
                break;
            case 32:
                uint u32 = (uint)clamped;
                if (bigEndian)
                {
                    _data[pos]     = (byte)(u32 >> 24); _data[pos + 1] = (byte)(u32 >> 16);
                    _data[pos + 2] = (byte)(u32 >> 8);  _data[pos + 3] = (byte)(u32);
                }
                else
                {
                    _data[pos]     = (byte)(u32);        _data[pos + 1] = (byte)(u32 >> 8);
                    _data[pos + 2] = (byte)(u32 >> 16);  _data[pos + 3] = (byte)(u32 >> 24);
                }
                break;
        }
    }
}
