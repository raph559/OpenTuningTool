namespace OpenTuningTool.Services;

internal static class KeyboardShortcutSupport
{
    public static bool IsTextInputControlFocused(Control root)
    {
        Control? focusedControl = FindFocusedControl(root);
        if (focusedControl == null)
            return false;

        if (focusedControl is TextBoxBase)
            return true;

        if (focusedControl is UpDownBase)
            return true;

        if (focusedControl is ComboBox comboBox)
            return comboBox.DropDownStyle != ComboBoxStyle.DropDownList;

        if (focusedControl is DataGridView dataGridView)
            return dataGridView.IsCurrentCellInEditMode || dataGridView.EditingControl is TextBoxBase;

        return false;
    }

    private static Control? FindFocusedControl(Control root)
    {
        Control? current = root;
        while (current is ContainerControl container && container.ActiveControl != null)
            current = container.ActiveControl;

        return current;
    }
}
