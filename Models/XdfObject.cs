namespace OpenTuningTool.Models;

public abstract class XdfObject 
{
	public int UniqueId { get; }

	public XdfObject(int uniqueId)
	{
		UniqueId = uniqueId;
	}

}