namespace OpenTuningTool.Models;

public class XdfDocument
{
	private readonly List<XdfTable> _tables;
	private readonly List<XdfConstant> _constants;
	private readonly Dictionary<int, List<XdfObject>> _objectsById;

	public IReadOnlyList<XdfTable> Tables => _tables;
	public IReadOnlyList<XdfConstant> Constants => _constants;
	public IReadOnlyDictionary<int, List<XdfObject>> Objects => _objectsById;

	/// <summary>
	/// File-byte offset to add to every raw XDF address to get the actual byte
	/// position inside the BIN file.  Parsed from XDFHEADER/BASEOFFSET.
	/// </summary>
	public int BaseOffset { get; private set; }

	internal void SetBaseOffset(int baseOffset) => BaseOffset = baseOffset;

	public XdfDocument()
	{
		_tables = new List<XdfTable>();
		_constants = new List<XdfConstant>();
		_objectsById = new Dictionary<int, List<XdfObject>>();
	}

	internal void AddObjectInDictionnary(XdfObject obj)
	{
		// Try to get existing list for this ID
		if (!_objectsById.TryGetValue(obj.UniqueId, out List<XdfObject>? list))
		{
			// If it doesnt exist, create and add to the dictionary
			list = new List<XdfObject>();
			_objectsById.Add(obj.UniqueId, list);
		}

		// The list exists, so just add the item
		list.Add(obj);
	}

	internal void AddTable(XdfTable table)
	{
		ArgumentNullException.ThrowIfNull(table);
		_tables.Add(table);
		
		AddObjectInDictionnary(table);
	}

	internal void AddConstant(XdfConstant constant)
	{
		ArgumentNullException.ThrowIfNull(constant);
		_constants.Add(constant);

		AddObjectInDictionnary(constant);
	}
}