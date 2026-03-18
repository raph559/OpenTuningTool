using OpenTuningTool.Models;

namespace OpenTuningTool.Forms;

public sealed class ValueEditDialog : Form
{
    private readonly TextBox _txtValue;

    public ValueEditDialog(string title, string prompt, string initialValue, AppTheme theme, UiDensity density)
    {
        var lblPrompt = new Label();
        _txtValue = new TextBox();
        var btnOk = new Button();
        var btnCancel = new Button();

        SuspendLayout();

        lblPrompt.AutoSize = false;
        lblPrompt.Text = prompt;
        lblPrompt.Location = new Point(14, 14);
        lblPrompt.Size = new Size(332, 34);

        _txtValue.Location = new Point(14, 52);
        _txtValue.Size = new Size(332, 23);
        _txtValue.Text = initialValue;
        _txtValue.Font = new Font("Consolas", 10F);

        btnOk.Text = "OK";
        btnOk.DialogResult = DialogResult.OK;
        btnOk.Location = new Point(190, 90);
        btnOk.Size = new Size(75, 28);

        btnCancel.Text = "Cancel";
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(271, 90);
        btnCancel.Size = new Size(75, 28);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(360, 132);
        Controls.Add(lblPrompt);
        Controls.Add(_txtValue);
        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = title;

        ThemeUtility.ApplyTheme(this, theme);
        ThemeUtility.ApplyUiDensity(this, density);
        ResumeLayout(false);
    }

    public string ValueText => _txtValue.Text;
}
