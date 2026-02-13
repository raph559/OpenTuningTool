namespace OpenTuningTool.Models;

public class XdfTable:XdfObject
{
	public string Title { get; }
	public string ?Description { get; }

	//XdfAxis? XAxis { get; set; }
	//XdfAxis? YAxis { get; set; }

	public XdfTable(int uniqueId, string title, string ?description) 
		: base(uniqueId)
	{
		Title = title;
		Description = description;
	}
}