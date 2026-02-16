namespace OpenTuningTool.Models;

public abstract class XdfObject 
{
	public int UniqueId { get; }
	public string Title { get; }
	public string Description { get; }

	public XdfObject(int uniqueId, string title, string description)
	{
		UniqueId = uniqueId;
		Title = title;
		Description = description;
	}
}