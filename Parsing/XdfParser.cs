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
			throw new NullReferenceException("extracted uniqueid cannot be null.");
		int uniqueId = Convert.ToInt32(uniqueIdStr, 16);
		
		// Get and check validity of title
		string? title = element.Element("title")?.Value;
		if (title == null)
			throw new NullReferenceException("extracted title cannot be null.");

		// Get description (can be null) 
		string? description = element.Element("description")?.Value;

		return (uniqueId, title, description);
	}

	public XdfTable ParseTable(XElement rawTable)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawTable);

		XdfTable table = new XdfTable(uniqueId, title, description);
		return table;
	}

	public XdfConstant ParseConstant(XElement rawConstant)
	{
		// Parse common fields (uniqueid, title, description) and check their validity
		(int uniqueId, string title, string? description) = ParseCommonFields(rawConstant);

		// Get and check validity of address
		string? addressStr = rawConstant.Element("EMBEDDEDDATA")?.Attribute("mmedaddress")?.Value;
		if (addressStr == null)
			throw new NullReferenceException("extracted address cannot be null.");
		int address = Convert.ToInt32(addressStr, 16);

		// Get and check validity of elementsizebits
		string? elementSizeBitsStr = rawConstant.Element("EMBEDDEDDATA")?.Attribute("mmedelementsizebits")?.Value;
		if (elementSizeBitsStr == null)
			throw new NullReferenceException("extracted elementsizebits cannot be null.");
		int elementSizeBits = Convert.ToInt32(elementSizeBitsStr);

		XdfConstant constant = new XdfConstant(uniqueId, title, description, address, elementSizeBits);
		return constant;
	}
}
