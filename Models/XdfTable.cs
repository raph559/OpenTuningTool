namespace OpenTuningTool.Models;

public class XdfTable:XdfObject
{
    public XdfAxis? XAxis { get; }
    public XdfAxis? YAxis { get; }
    public XdfTableData? ZAxis { get; }

    public XdfTable(int uniqueId, string title, string? description, XdfAxis? xAxis, XdfAxis? yAxis, XdfTableData? zAxis) 
        : base(uniqueId, title, description)
    {
        XAxis = xAxis;
        YAxis = yAxis;
        ZAxis = zAxis;
    }
}