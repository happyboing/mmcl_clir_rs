namespace MMCL_CLIRRS;

public static class PromptDialog
{
    public static string? Show(IWin32Window owner, string title, string message,
                               bool maskInput = false, string okText = "Verify")
    {
        using var form = new Form
        {
            Text = title,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(380, 165)
        };

        var label = new Label { Text = message, Location = new Point(20, 20), Size = new Size(340, 45) };
        var box = new TextBox
        {
            Location = new Point(20, 75),
            Width = 340,
            UseSystemPasswordChar = maskInput
        };

        var ok = new Button
        {
            Text = okText,
            DialogResult = DialogResult.OK,
            Location = new Point(170, 115),
            Size = new Size(90, 36),
            BackColor = Color.FromArgb(183, 28, 28),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(270, 115),
            Size = new Size(90, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(183, 28, 28),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(183, 28, 28);

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Controls.AddRange(new Control[] { label, box, ok, cancel });

        return form.ShowDialog(owner) == DialogResult.OK ? box.Text.Trim() : null;
    }
}