namespace MMCL_CLIRRS;

public class ActivityLogForm : Form
{
    public ActivityLogForm(List<ActivityLogEntry> entries)
    {
        Text = "Activity Log";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 550);

        Controls.Add(Theme.CreateHeader(
            "Activity log",
            "Recent staff operations, most recent first.",
            this));

        var card = new CardPanel
        {
            Location = new Point(24, 106),
            Size = new Size(852, 418),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(18, 16, 18, 18)
        };
        Controls.Add(card);

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Ink,
            HorizontalScrollbar = true,
            IntegralHeight = false
        };

        if (entries.Count == 0)
        {
            list.Items.Add("No operations recorded yet.");
            list.ForeColor = Theme.Muted;
        }
        else
        {
            list.DataSource = entries;
        }

        card.Controls.Add(list);
    }
}
