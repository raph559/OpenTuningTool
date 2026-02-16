namespace OpenTuningTool.Models;

public class XdfAxis
{
	public char Id {get; }
	public int IndexCount {get; }
	public int ElementSizeBits {get; }
	public int MajorStrideBits {get; }
	public int MinorStrideBits {get; }

	public XdfAxis(char id, int indexCount,int elementSizeBits, int majorStrideBits, int minorStrideBits)
	{
		Id = id;
		IndexCount = indexCount;
		ElementSizeBits = elementSizeBits;
		MajorStrideBits = majorStrideBits;
		MinorStrideBits = minorStrideBits;
	}
}