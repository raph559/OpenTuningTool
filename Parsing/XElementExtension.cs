using System.Xml.Linq;

namespace OpenTuningTool.Parsing;

public static class XElementExtension
{
	public static int ParseIntAttribute(this XElement element, string attributeName)
	{
		string? attributeValue = element.Attribute(attributeName)?.Value;

		if (attributeValue == null)
		{
			string contextName = "unknown element";
			XElement? parentTable = element.Ancestors("XDFTABLE").FirstOrDefault();
			if (parentTable != null)
			{ 
				contextName = parentTable.Element("title")?.Value ?? contextName;
			}

			throw new InvalidDataException($"The attribute '{attributeName}' could not be found in {contextName}.");
		}

		return 0;
	}
}