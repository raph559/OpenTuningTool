namespace OpenTuningTool.Models;

public readonly record struct BinCellEdit(
    int Offset,
    int ElementSizeBits,
    int TypeFlags,
    double PreviousRawValue,
    double NewRawValue,
    string Label);
