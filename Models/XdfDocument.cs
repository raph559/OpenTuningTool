namespace OpenTuningTool.Models;

class XdfDocument
{
	private readonly List<XdfTable> _tables;
	private readonly List<XdfConstant> _constants;
	private readonly Dictionary<int, XdfObject> _objectsById;

	public IReadOnlyList<XdfTable> Tables => _tables;
	public IReadOnlyList<XdfConstant> Constants => _constants;
	public IReadOnlyDictionary<int, XdfObject> Objects => _objectsById;

	public XdfDocument()
	{
		_tables = new List<XdfTable>();
		_constants = new List<XdfConstant>();
		_objectsById = new Dictionary<int, XdfObject>();
	}

	internal void AddTable(XdfTable table)
	{
		ArgumentNullException.ThrowIfNull(table);

		if (_objectsById.ContainsKey(table.UniqueId)) 
			throw new InvalidOperationException("An object with this ID already exists.");

		_tables.Add(table);
		_objectsById.Add(table.UniqueId, table);
	}

	internal void AddConstant(XdfConstant constant)
	{
		ArgumentNullException.ThrowIfNull(constant);

		if (_objectsById.ContainsKey(constant.UniqueId))
			throw new InvalidOperationException("An object with this ID already exists.");

		_constants.Add(constant);
		_objectsById.Add(constant.UniqueId, constant);
	}
}