namespace OpenTuningTool;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblPrompt = new Label();
        txtUserName = new TextBox();
        btnGenerate = new Button();
        btnClear = new Button();
        lblGreeting = new Label();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(560, 210);
        Controls.Add(lblGreeting);
        Controls.Add(btnClear);
        Controls.Add(btnGenerate);
        Controls.Add(txtUserName);
        Controls.Add(lblPrompt);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "OpenTuningTool";

        lblPrompt.AutoSize = true;
        lblPrompt.Location = new Point(24, 28);
        lblPrompt.Name = "lblPrompt";
        lblPrompt.Size = new Size(67, 15);
        lblPrompt.TabIndex = 0;
        lblPrompt.Text = "Driver/Name";

        txtUserName.Location = new Point(24, 52);
        txtUserName.Name = "txtUserName";
        txtUserName.Size = new Size(360, 23);
        txtUserName.TabIndex = 1;

        btnGenerate.Location = new Point(400, 51);
        btnGenerate.Name = "btnGenerate";
        btnGenerate.Size = new Size(126, 25);
        btnGenerate.TabIndex = 2;
        btnGenerate.Text = "Generate";
        btnGenerate.UseVisualStyleBackColor = true;

        btnClear.Location = new Point(400, 86);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(126, 25);
        btnClear.TabIndex = 3;
        btnClear.Text = "Clear";
        btnClear.UseVisualStyleBackColor = true;

        lblGreeting.BorderStyle = BorderStyle.FixedSingle;
        lblGreeting.Location = new Point(24, 98);
        lblGreeting.Name = "lblGreeting";
        lblGreeting.Padding = new Padding(8);
        lblGreeting.Size = new Size(360, 80);
        lblGreeting.TabIndex = 4;
        lblGreeting.Text = "Greeting";
    }

    #endregion

    private Label lblPrompt;
    private TextBox txtUserName;
    private Button btnGenerate;
    private Button btnClear;
    private Label lblGreeting;
}
