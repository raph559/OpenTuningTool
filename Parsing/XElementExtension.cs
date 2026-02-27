using System.Xml.Linq;

namespace OpenTuningTool.Parsing;

public static class XElementExtension
{
	public static int ParseIntAttribute(this XElement element, string attributeName)
	{
		string? valueStr = element.Attribute(attributeName)?.Value;
		string targetName = $"The attribute '{attributeName}'";

		return ProcessAndConvert(element, valueStr, targetName);
	}

	public static int ParseIntElement(this XElement element)
	{
		string? valueStr = element.Value;
		string targetName = $"The value of <{element.Name.LocalName}>";

		return ProcessAndConvert(element, valueStr, targetName);
	}

	// Special method as some valid axis can have null addresses
	public static int? ParseNullableIntAttribute(this XElement element, string attributeName)
	{
		string? valueStr = element.Attribute(attributeName)?.Value;

		if (string.IsNullOrWhiteSpace(valueStr))
		{
			return null;
		}

		return valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(valueStr, 16) : Convert.ToInt32(valueStr);
	}

	// Error handling and conversion
	private static int ProcessAndConvert(XElement element, string? valueStr, string targetName)
	{
		if (string.IsNullOrWhiteSpace(valueStr))
		{
			string contextName = "an unknown element";

			// Check if inside a table
			XElement? parentTable = element.Ancestors("XDFTABLE").FirstOrDefault();
			if (parentTable != null)
			{
				string tableTitle = parentTable.Element("title")?.Value ?? "unknown table";

				// Check if inside an axis within this table
				XElement? parentAxis = element.Ancestors("XDFAXIS").FirstOrDefault();
				if (parentAxis != null)
				{
					string axisId = parentAxis.Attribute("id")?.Value ?? "unknown";
					contextName = $"axis '{axisId}' of table '{tableTitle}'";
				}
				else
				{
					contextName = $"table '{tableTitle}'";
				}
			}
			else
			{
				// Check if inside a constant
				XElement? parentConstant = element.Ancestors("XDFCONSTANT").FirstOrDefault();
				if (parentConstant != null)
				{
					string constantTitle = parentConstant.Element("title")?.Value ?? "unknown constant";
					contextName = $"constant '{constantTitle}'";
				}
			}

			throw new InvalidDataException($"{targetName} could not be found or is empty in {contextName}.");
		}

		// Auto convert from hex if needed
		return valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(valueStr, 16) : Convert.ToInt32(valueStr);
	}

	public static string ParseStringAttribute(this XElement element, string attributeName)
	{
		string? valueStr = element.Attribute(attributeName)?.Value;
		string targetName = $"The attribute '{attributeName}'";

		return ProcessString(element, valueStr, targetName);
	}

	public static string ParseStringElement(this XElement element)
	{
		string? valueStr = element.Value;
		string targetName = $"The value of <{element.Name.LocalName}>";

		return ProcessString(element, valueStr, targetName);
	}

	// Error handling for strings
	private static string ProcessString(XElement element, string? valueStr, string targetName)
	{
		if (string.IsNullOrWhiteSpace(valueStr))
		{
			string contextName = "an unknown element";

			// Check if inside a table
			XElement? parentTable = element.Ancestors("XDFTABLE").FirstOrDefault();
			if (parentTable != null)
			{
				string tableTitle = parentTable.Element("title")?.Value ?? "unknown table";

				// Check if inside an axis within this table
				XElement? parentAxis = element.Ancestors("XDFAXIS").FirstOrDefault();
				if (parentAxis != null)
				{
					string axisId = parentAxis.Attribute("id")?.Value ?? "unknown";
					contextName = $"axis '{axisId}' of table '{tableTitle}'";
				}
				else
				{
					contextName = $"table '{tableTitle}'";
				}
			}
			else
			{
				// Check if inside a constant
				XElement? parentConstant = element.Ancestors("XDFCONSTANT").FirstOrDefault();
				if (parentConstant != null)
				{
					string constantTitle = parentConstant.Element("title")?.Value ?? "unknown constant";
					contextName = $"constant '{constantTitle}'";
				}
			}

			throw new InvalidDataException($"{targetName} could not be found or is empty in {contextName}.");
		}

		return valueStr;
	}
}