namespace OpenTuningTool.Models;

public class XdfConstant:XdfObject
{
	public int Address {get;}
	public int ElementSizeBits {get;}

	public XdfConstant(int uniqueId, string title, string description, int address, int elementSizeBits) 
		: base(uniqueId, title, description)
	{
		Address = address;
		ElementSizeBits = elementSizeBits;
	}
}