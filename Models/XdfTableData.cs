namespace OpenTuningTool.Models;

public class XdfTableData
{
	public int Address {get; }
	public int RowCount {get; }
	public int ColCount {get; }
	public int ElementSizeBits {get; }
	public int MajorStrideBits {get; }
	public int MinorStrideBits {get; }
	public XdfValueFormat Format { get; }

	public XdfTableData(
		int address,
		int rowCount,
		int colCount,
		int elementSizeBits,
		int majorStrideBits,
		int minorStrideBits,
		XdfValueFormat? format = null)
	{
		Address = address;
		RowCount = rowCount;
		ColCount = colCount;
		ElementSizeBits = elementSizeBits;
		MajorStrideBits = majorStrideBits;
		MinorStrideBits = minorStrideBits;
		Format = format ?? XdfValueFormat.Identity;
	}
}
