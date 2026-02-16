namespace OpenTuningTool.Models;

public class XdfTableData
{
	public int Address {get; }
	public int RowCount {get; }
	public int ColCount {get; }
	public int ElementSizeBits {get; }
	public int MajorStrideBits {get; }
	public int MinorStrideBits {get; }

	public XdfTableData (int address, int rowCount, int colCount, int elementSizeBits, int majorStrideBits, int minorStrideBits)
	{
		Address = address;
		RowCount = rowCount;
		ColCount = colCount;
		ElementSizeBits = elementSizeBits;
		MajorStrideBits = majorStrideBits;
		MinorStrideBits = minorStrideBits;
	}
}