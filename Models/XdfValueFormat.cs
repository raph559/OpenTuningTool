namespace OpenTuningTool.Models;

public sealed class XdfValueFormat
{
    public const int SignedFlag = 0x01;
    public const int LittleEndianFlag = 0x02;
    public const int FloatingPointFlag = 0x10000;

    public static XdfValueFormat Identity { get; } = new();

    public int TypeFlags { get; }
    public int? DecimalPlaces { get; }
    public int? OutputType { get; }
    public string Units { get; }
    public string? MathEquation { get; }

    public bool IsSigned => !IsFloatingPoint && (TypeFlags & SignedFlag) != 0;
    public bool IsLittleEndian => (TypeFlags & LittleEndianFlag) != 0;
    public bool IsFloatingPoint => (TypeFlags & FloatingPointFlag) != 0;

    public XdfValueFormat(
        int typeFlags = 0,
        int? decimalPlaces = null,
        int? outputType = null,
        string? units = null,
        string? mathEquation = null)
    {
        TypeFlags = typeFlags;
        DecimalPlaces = decimalPlaces;
        OutputType = outputType;
        Units = units ?? string.Empty;
        MathEquation = string.IsNullOrWhiteSpace(mathEquation) ? null : mathEquation.Trim();
    }
}
