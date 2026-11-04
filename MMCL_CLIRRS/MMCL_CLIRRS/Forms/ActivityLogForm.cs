namespace MMCL_CLIRRS;

public class ActivityLogForm : Form
{
    public ActivityLogForm(List<ActivityLogEntry> entries)
    {
        Text = "Activity Log";
        BackColor = Color.White;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 420);

        var header = new Label
        {
            Text = "Recent operations (most recent first)",
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(183, 28, 28),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            BorderStyle = BorderStyle.None,
            HorizontalScrollbar = true
        };

        if (entries.Count == 0) list.Items.Add("No operations recorded yet.");
        else list.DataSource = entries;

        Controls.Add(list);
        Controls.Add(header);
    }
}