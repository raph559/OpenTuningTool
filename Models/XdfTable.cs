namespace OpenTuningTool.Models;

public class XdfTable:XdfObject
{
	XdfAxis? XAxis { get; set; }
	XdfAxis? YAxis { get; set; }
	XdfTableData? ZAxis { get; set; }

	public XdfTable(int uniqueId, string title, string description) 
		: base(uniqueId, title, description){}
}