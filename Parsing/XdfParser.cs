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
		int defaultTypeFlags = 0;

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

			defaultTypeFlags = ParseDefaultTypeFlags(header.Element("DEFAULTS"));
		}

		// Parse tables
		IEnumerable<XElement> rawTables = doc.Descendants("XDFTABLE");
		foreach (XElement rawTable in rawTables)
			xdf.AddTable(ParseTable(rawTable, defaultTypeFlags));

		// Parse constants
		IEnumerable<XElement> rawConstants = doc.Descendants("XDFCONSTANT");
		foreach (XElement rawConstant in rawConstants)
			xdf.AddConstant(ParseConstant(rawConstant, defaultTypeFlags));

		return xdf;
	}

	private static int ParseDefaultTypeFlags(XElement? defaults)
	{
		if (defaults == null) return 0;

		int typeFlags = 0;
		if ((defaults.ParseNullableIntAttribute("signed") ?? 0) != 0)
			typeFlags |= XdfValueFormat.SignedFlag;
		if ((defaults.ParseNullableIntAttribute("lsbfirst") ?? 0) != 0)
			typeFlags |= XdfValueFormat.LittleEndianFlag;
		if ((defaults.ParseNullableIntAttribute("float") ?? 0) != 0)
			typeFlags |= XdfValueFormat.FloatingPointFlag;

		return typeFlags;
	}

	private static XdfValueFormat ParseValueFormat(XElement owner, XElement embeddeddata, int defaultTypeFlags)
	{
		int typeFlags = embeddeddata.ParseNullableIntAttribute("mmedtypeflags") ?? defaultTypeFlags;
		int? decimalPlaces = owner.Element("decimalpl")?.ParseNullableIntElement();
		int? outputType = owner.Element("outputtype")?.ParseNullableIntElement();
		string units = owner.Element("units")?.Value ?? string.Empty;
		string? mathEquation = owner.Element("MATH")?.Attribute("equation")?.Value;

		return new XdfValueFormat(typeFlags, decimalPlaces, outputType, units, mathEquation);
	}

	private static IReadOnlyDictionary<int, string> ParseAxisLabels(XElement rawAxis)
	{
		var labels = new Dictionary<int, string>();
		foreach (XElement rawLabel in rawAxis.Elements("LABEL"))
		{
			int? index = rawLabel.ParseNullableIntAttribute("index");
			string? value = rawLabel.Attribute("value")?.Value;
			if (index.HasValue && value != null)
				labels[index.Value] = value;
		}

		return labels;
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

	public XdfAxis ParseAxis(XElement rawAxis, int defaultTypeFlags)
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

		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");
		int majorStrideBits = embeddeddata.ParseIntAttribute("mmedmajorstridebits");
		int minorStrideBits = embeddeddata.ParseIntAttribute("mmedminorstridebits");
		int? address = embeddeddata.ParseNullableIntAttribute("mmedaddress");
		XdfValueFormat format = ParseValueFormat(rawAxis, embeddeddata, defaultTypeFlags);
		IReadOnlyDictionary<int, string> labels = ParseAxisLabels(rawAxis);

		return new XdfAxis(
			Convert.ToChar(id),
			indexCount,
			address,
			elementSizeBits,
			majorStrideBits,
			minorStrideBits,
			format,
			labels);
	}

	public XdfTableData ParseTableData(XElement rawTableData, int defaultTypeFlags)
	{
		XElement? embeddeddata = rawTableData.Element("EMBEDDEDDATA");
		string parentTableTitle = rawTableData.Ancestors("XDFTABLE").FirstOrDefault()?.Element("title")?.Value ?? "unknown table";
		if (embeddeddata == null) throw new InvalidDataException($"EMBEDDEDDATA element is missing in Z axis of '{parentTableTitle}' table.");

		int address = embeddeddata.ParseIntAttribute("mmedaddress");
		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");
		int rowCount = embeddeddata.ParseNullableIntAttribute("mmedrowcount") ?? 1;
		int colCount = embeddeddata.ParseNullableIntAttribute("mmedcolcount") ?? 1;
		int majorStrideBits = embeddeddata.ParseNullableIntAttribute("mmedmajorstridebits") ?? 0;
		int minorStrideBits = embeddeddata.ParseNullableIntAttribute("mmedminorstridebits") ?? 0;
		XdfValueFormat format = ParseValueFormat(rawTableData, embeddeddata, defaultTypeFlags);

		return new XdfTableData(address, rowCount, colCount, elementSizeBits, majorStrideBits, minorStrideBits, format);
	}

	public XdfTable ParseTable(XElement rawTable, int defaultTypeFlags)
	{
		(int uniqueId, string title, string? description) = ParseCommonFields(rawTable);

		IEnumerable<XElement> axes = rawTable.Descendants("XDFAXIS");

		XElement? rawXAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "x");
		if (rawXAxis == null) throw new InvalidDataException("A table X axis cannot be null.");
		XdfAxis xAxis = ParseAxis(rawXAxis, defaultTypeFlags);

		XElement? rawYAxis = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "y");
		if (rawYAxis == null) throw new InvalidDataException("A table Y axis cannot be null.");
		XdfAxis yAxis = ParseAxis(rawYAxis, defaultTypeFlags);

		XElement? rawTableData = axes.FirstOrDefault(axis => axis.Attribute("id")?.Value == "z");
		if (rawTableData == null) throw new InvalidDataException("A table's TableData (Z axis) cannot be null.");
		XdfTableData tableData = ParseTableData(rawTableData, defaultTypeFlags);

		return new XdfTable(uniqueId, title, description, xAxis, yAxis, tableData);
	}

	public XdfConstant ParseConstant(XElement rawConstant, int defaultTypeFlags)
	{
		(int uniqueId, string title, string? description) = ParseCommonFields(rawConstant);

		XElement? embeddeddata = rawConstant.Element("EMBEDDEDDATA");
		if (embeddeddata == null) throw new InvalidDataException($"EMBEDDEDDATA element is missing in the constant '{title}'.");

		int address = embeddeddata.ParseIntAttribute("mmedaddress");
		int elementSizeBits = embeddeddata.ParseIntAttribute("mmedelementsizebits");
		XdfValueFormat format = ParseValueFormat(rawConstant, embeddeddata, defaultTypeFlags);

		return new XdfConstant(uniqueId, title, description, address, elementSizeBits, format);
	}
}
