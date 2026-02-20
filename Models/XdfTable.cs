namespace OpenTuningTool.Models;

public class XdfTable:XdfObject
{
	public XdfAxis? XAxis { get; internal set; }
	public XdfAxis? YAxis { get; internal set; }
	public XdfTableData? ZAxis { get; internal set; }

	public XdfTable(int uniqueId, string title, string? description) 
		: base(uniqueId, title, description)
	{
		
	}
}