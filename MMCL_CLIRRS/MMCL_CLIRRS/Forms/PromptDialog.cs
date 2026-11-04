namespace MMCL_CLIRRS;

public static class PromptDialog
{
    public static string? Show(IWin32Window owner, string title, string message,
                               bool maskInput = false, string okText = "Verify")
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(470, 258)
        };
        Theme.StyleForm(form);

        form.Controls.Add(Theme.CreateHeader(title, "Secure staff access", form, null, false));

        form.Controls.Add(new Label
        {
            Text = message,
            Location = new Point(30, 106),
            Size = new Size(410, 44),
            Font = Theme.Font(9.75f),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        var box = new TextBox { UseSystemPasswordChar = maskInput };
        var shell = new FieldShell(box, 42)
        {
            Location = new Point(30, 150),
            Width = 410
        };
        form.Controls.Add(shell);

        var ok = Theme.MakeButton(okText, AppButtonKind.Primary, 118, 40);
        ok.DialogResult = DialogResult.OK;
        ok.Location = new Point(322, 204);

        var cancel = Theme.MakeButton("Cancel", AppButtonKind.Secondary, 118, 40);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(194, 204);

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Controls.Add(ok);
        form.Controls.Add(cancel);

        shell.Field.Focus();
        return form.ShowDialog(owner) == DialogResult.OK ? box.Text.Trim() : null;
    }
}
