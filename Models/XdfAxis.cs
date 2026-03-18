namespace OpenTuningTool.Models;

public class XdfAxis
{
	public char Id {get; }
	public int IndexCount {get; }
	public int? Address {get; }
	public int ElementSizeBits {get; }
	public int MajorStrideBits {get; }
	public int MinorStrideBits {get; }
	public XdfValueFormat Format { get; }
	public IReadOnlyDictionary<int, string> Labels { get; }

	public XdfAxis(
		char id,
		int indexCount,
		int? address,
		int elementSizeBits,
		int majorStrideBits,
		int minorStrideBits,
		XdfValueFormat? format = null,
		IReadOnlyDictionary<int, string>? labels = null)
	{
		Id = id;
		IndexCount = indexCount;
		Address = address;
		ElementSizeBits = elementSizeBits;
		MajorStrideBits = majorStrideBits;
		MinorStrideBits = minorStrideBits;
		Format = format ?? XdfValueFormat.Identity;
		Labels = labels != null
			? new Dictionary<int, string>(labels)
			: new Dictionary<int, string>();
	}
}
