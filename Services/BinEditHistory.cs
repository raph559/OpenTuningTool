using OpenTuningTool.Models;

namespace OpenTuningTool.Services;

internal sealed class BinEditHistory
{
    private readonly Stack<BinCellEdit> _undoStack = new();
    private readonly Stack<BinCellEdit> _redoStack = new();

    public void Record(BinCellEdit edit)
    {
        if (edit.PreviousRawValue.Equals(edit.NewRawValue))
            return;

        _undoStack.Push(edit);
        _redoStack.Clear();
    }

    public bool TryUndo(BinBuffer? bin, out BinCellEdit edit)
    {
        edit = default;
        if (bin == null || _undoStack.Count == 0)
            return false;

        edit = _undoStack.Pop();
        bin.WriteCell(edit.Offset, edit.ElementSizeBits, edit.TypeFlags, edit.PreviousRawValue);
        _redoStack.Push(edit);
        return true;
    }

    public bool TryRedo(BinBuffer? bin, out BinCellEdit edit)
    {
        edit = default;
        if (bin == null || _redoStack.Count == 0)
            return false;

        edit = _redoStack.Pop();
        bin.WriteCell(edit.Offset, edit.ElementSizeBits, edit.TypeFlags, edit.NewRawValue);
        _undoStack.Push(edit);
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
