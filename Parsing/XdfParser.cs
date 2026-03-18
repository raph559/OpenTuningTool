using OpenTuningTool.Models;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OpenTuningTool.Parsing;

public class XdfParser
{
	public XdfDocument Parse(string filePath)
	{
		XDocument doc = XDocument.Load(filePath);
		XdfDocument xdf = new XdfDocument();

		// Parse base offset from XDFHEADER
		XElement? header = doc.Root?.Element("XDFHEADER");
		if (header != null)
		{
			XElement? baseOffsetEl = header.Element("BASEOFFSET");
			if (baseOffsetEl != null)
			{
				int offset   = baseOffsetEl.ParseNullableIntAttribute("offset")   ?? 0;
				int subtract = baseOffsetEl.ParseNullableIntAttribute("subtract") ?? 0;
				xdf.SetBaseOffset(subtract != 0 ? -offset : offset);
			}
		}

		// Parse tables
		IEnumerable<XElement> rawTables = doc.Descendants("XDFTABLE");
		foreach (XElement rawTable in rawTables)
			xdf.AddTable(ParseTable(rawTable));

		// Parse constants
		IEnumerable<XElement> rawConstants = doc.Descendants("XDFCONSTANT");
		foreach (XElement rawConstant in rawConstants)
			xdf.AddConstant(ParseConstant(rawConstant));

		return xdf;
	}

	private (int uniqueId, string title, string? description) ParseCommonFields(XElement element)
	{
		// UniqueID
		int uniqueId = element.ParseIntAttribute("uniqueid");

		// Title
		XElement? titleElement = element.Element("title");
		if (titleElement == null) throw new InvalidDataException("A title element cannot be null.");
		string title = titleElement.ParseStringElement();

		// Get description (can be null so just take the value) 
		string? description = element.Element("description")?.Value;

		return (uniqueId, title, description);
	}

	public XdfAxis ParseAxis(XElement rawAxis)
	{
		// ID
		string id = rawAxis.ParseStringAttribute("id");

		// Index Count
		XElement? indexCountElement = rawAxis.Element("indexcount");
		if (indexCountElement == null) throw new InvalidDataException("An axis index count cannot be null.");
		int indexCount = indexCountElement.ParseIntElement();

		// Embedded Data
		XElement? embeddeddata = rawAxis.Element("EMBEDDEDDATA");
		if (embeddeddata == null)
		{
			string parentTableTitle = rawAxis.Ancestors("XDFTABLE").FirstOrDefault()?.Element("title")?.Value ?? "unknown table";
			throw new InvalidDataException($"EMBEDDEDDATA element is missing in axis '{id}' of '{parentTableTitle}' table.");
		}

		// Others
		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");
		int majorStrideBits = embeddeddata.ParseIntAttribute("mmedmajorstridebits");
		int minorStrideBits = embeddeddata.ParseIntAttribute("mmedminorstridebits");

		int? address = embeddeddata.ParseNullableIntAttribute("mmedaddress");

		return new XdfAxis(Convert.ToChar(id), indexCount, address, elementSizeBits, majorStrideBits, minorStrideBits);
	}

	public XdfTableData ParseTableData(XElement rawTableData)
	{
		// Embedded Data
		XElement? embeddeddata = rawTableData.Element("EMBEDDEDDATA");
		string parentTableTitle = rawTableData.Ancestors("XDFTABLE").FirstOrDefault()?.Element("title")?.Value ?? "unknown table";
		if (embeddeddata == null) throw new InvalidDataException($"EMBEDDEDDATA element is missing in Z axis of '{parentTableTitle}' table.");

		// Required fields
		int address = embeddeddata.ParseIntAttribute("mmedaddress");
		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");

		// Optional fields (defaulting to 1 for rows/cols, and 0 for strides)
		int rowCount = embeddeddata.ParseNullableIntAttribute("mmedrowcount") ?? 1;
		int colCount = embeddeddata.ParseNullableIntAttribute("mmedcolcount") ?? 1;
		int majorStrideBits = embeddeddata.ParseNullableIntAttribute("mmedmajorstridebits") ?? 0;
		int minorStrideBits = embeddeddata.ParseNullableIntAttribute("mmedminorstridebits") ?? 0;

		return new XdfTableData(address, rowCount, colCount, elementSizeBits, majorStrideBits, minorStrideBits);
	}

	public XdfTable ParseTable(XElement rawTable)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawTable);

		// Parse axes and check their validity
		IEnumerable<XElement> axes = rawTable.Descendants("XDFAXIS");

		XElement? rawXAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "x");
		if (rawXAxis == null) throw new InvalidDataException("A table X axis cannot be null.");
		XdfAxis xAxis = ParseAxis(rawXAxis);

		XElement? rawYAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "y");
		if (rawYAxis == null) throw new InvalidDataException("A table Y axis cannot be null.");
		XdfAxis yAxis = ParseAxis(rawYAxis);

		XElement? rawTableData = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "z");
		if (rawTableData == null) throw new InvalidDataException("A table's TableData (Z axis) cannot be null.");
		XdfTableData tableData = ParseTableData(rawTableData);

		return new XdfTable(uniqueId, title, description, xAxis, yAxis, tableData);
	}

	public XdfConstant ParseConstant(XElement rawConstant)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawConstant);

		XElement? embeddeddata = rawConstant.Element("EMBEDDEDDATA");
		if (embeddeddata == null) throw new InvalidDataException($"EMBEDDEDDATA element is missing in the constant '{title}'.");

		int address = embeddeddata.ParseIntAttribute("mmedaddress");
		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");

		return new XdfConstant(uniqueId, title, description, address, elementSizeBits);
	}
}