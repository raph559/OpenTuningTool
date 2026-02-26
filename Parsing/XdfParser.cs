using OpenTuningTool.Models;

using System.Xml.Linq;

namespace OpenTuningTool.Parsing;

public class XdfParser{
	public XdfDocument Parse(string filePath)
	{
		XDocument doc = XDocument.Load(filePath);
		XdfDocument xdf = new XdfDocument();

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
		// Get and check validity of uniqueid
		string? uniqueIdStr = element.Attribute("uniqueid")?.Value;
		if (uniqueIdStr == null)
			throw new InvalidDataException("Extracted uniqueid cannot be null.");
		int uniqueId = Convert.ToInt32(uniqueIdStr, 16);
		
		// Get and check validity of title
		string? title = element.Element("title")?.Value;
		if (title == null)
			throw new InvalidDataException("Extracted title cannot be null.");

		// Get description (can be null) 
		string? description = element.Element("description")?.Value;

		return (uniqueId, title, description);
	}

	public XdfAxis ParseAxis(XElement rawAxis)
	{
		string? id = rawAxis.Attribute("id")?.Value;
		if (id == null)
			throw new InvalidDataException("An axis id cannot be null.");

		string? indexCountStr = rawAxis.Element("indexcount")?.Value;
		if (indexCountStr == null)
			throw new InvalidDataException("An axis index count cannot be null.");
		int indexCount = Convert.ToInt32(indexCountStr);

		XElement? embeddeddata = rawAxis.Element("EMBEDDEDDATA");
		string? addressStr = embeddeddata?.Attribute("mmedaddress")?.Value;
		int? address = null;
		if (addressStr != null) // Valid axis can have null addresses.
			address = Convert.ToInt32(addressStr, 16);

		string? elementSizeBitsStr = embeddeddata?.Attribute("mmedelementsizebits")?.Value;
		if (elementSizeBitsStr == null)
			throw new InvalidDataException("An axis element size bits cannot be null.");
		int elementSizeBits = Convert.ToInt32(elementSizeBitsStr);

		string? majorStrideBitsStr = embeddeddata?.Attribute("mmedmajorstridebits")?.Value;
		if (majorStrideBitsStr == null)
			throw new InvalidDataException("An axis major stride bits cannot be null.");
		int majorStrideBits = Convert.ToInt32(majorStrideBitsStr);

		string? minorStrideBitsStr = embeddeddata?.Attribute("mmedminorstridebits")?.Value;
		if (minorStrideBitsStr == null)
			throw new InvalidDataException("An axis minor stride bits cannot be null.");
		int minorStrideBits = Convert.ToInt32(minorStrideBitsStr);

		XdfAxis axis = new XdfAxis(Convert.ToChar(id), indexCount, address, elementSizeBits, majorStrideBits, minorStrideBits);
		return axis;
	}

	public XdfTableData ParseTableData(XElement rawTableData)
	{
		XElement? embeddeddata = rawTableData.Element("EMBEDDEDDATA");
		string? addressStr = embeddeddata?.Attribute("mmedaddress")?.Value;
		if (addressStr == null)
			throw new InvalidDataException("A table data address cannot be null.");
		int address = Convert.ToInt32(addressStr, 16);

		string? rowCountStr = embeddeddata?.Attribute("mmedrowcount")?.Value;
		if (rowCountStr == null)
			throw new InvalidDataException("A table data row count cannot be null.");
		int rowCount = Convert.ToInt32(rowCountStr);

		string? colCountStr = embeddeddata?.Attribute("mmedcolcount")?.Value;
		if (colCountStr == null)
			throw new InvalidDataException("A table data column count cannot be null.");
		int colCount = Convert.ToInt32(colCountStr);

		string? elementSizeBitsStr = embeddeddata?.Attribute("mmedelementsizebits")?.Value;
		if (elementSizeBitsStr == null)
			throw new InvalidDataException("A table data element size bits cannot be null.");
		int elementSizeBits = Convert.ToInt32(elementSizeBitsStr);

		string? majorStrideBitsStr = embeddeddata?.Attribute("mmedmajorstridebits")?.Value;
		if (majorStrideBitsStr == null)
			throw new InvalidDataException("A table data major stride bits cannot be null.");
		int majorStrideBits = Convert.ToInt32(majorStrideBitsStr);

		string? minorStrideBitsStr = embeddeddata?.Attribute("mmedminorstridebits")?.Value;
		if (minorStrideBitsStr == null)
			throw new InvalidDataException("A table data mino stride bits cannot be null.");
		int minorStrideBits = Convert.ToInt32(minorStrideBitsStr);

		XdfTableData tableData = new XdfTableData(address, rowCount, colCount, elementSizeBits, majorStrideBits, minorStrideBits);
		return tableData;
	}

	public XdfTable ParseTable(XElement rawTable)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawTable);

		// Parse axes and check their validity
		IEnumerable<XElement> axes = rawTable.Descendants("XDFAXIS");

		XElement? rawXAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "x");
		if (rawXAxis == null)
			throw new InvalidDataException("A table X axis cannot be null.");
		XdfAxis xAxis = ParseAxis(rawXAxis);

		XElement? rawYAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "y");
		if (rawYAxis == null)
			throw new InvalidDataException("A table Y axis cannot be null.");
		XdfAxis yAxis = ParseAxis(rawYAxis);

		XElement? rawTableData = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "z");
		if (rawTableData == null)
			throw new InvalidDataException("A table's TableData (Z axis) cannot be null.");
		XdfTableData tableData = ParseTableData(rawTableData);

		XdfTable table = new XdfTable(uniqueId, title, description, xAxis, yAxis, tableData);
		return table;
	}

	public XdfConstant ParseConstant(XElement rawConstant)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawConstant);

		// Get and check validity of address
		string? addressStr = rawConstant.Element("EMBEDDEDDATA")?.Attribute("mmedaddress")?.Value;
		if (addressStr == null)
			throw new InvalidDataException("Extracted address cannot be null.");
		int address = Convert.ToInt32(addressStr, 16);

		// Get and check validity of elementsizebits
		string? elementSizeBitsStr = rawConstant.Element("EMBEDDEDDATA")?.Attribute("mmedelementsizebits")?.Value;
		if (elementSizeBitsStr == null)
			throw new InvalidDataException("Extracted elementsizebits cannot be null.");
		int elementSizeBits = Convert.ToInt32(elementSizeBitsStr);

		XdfConstant constant = new XdfConstant(uniqueId, title, description, address, elementSizeBits);
		return constant;
	}
}
