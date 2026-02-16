namespace OpenTuningTool.Models;

class XdfDocument
{
	private List<XdfTable> ?_tables;
	private Dictionary<int, XdfObject> ?_objectsById;

	public IReadOnlyList<XdfTable> ?Tables;
	public IReadOnlyDictionary<int, XdfObject> ?Objects;

	public XdfDocument(){}

	internal void AddTable(XdfTable table)
	{
		if (table == null) return;
	}
}