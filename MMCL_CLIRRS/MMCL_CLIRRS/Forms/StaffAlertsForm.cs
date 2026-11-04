namespace MMCL_CLIRRS;

public class StaffAlertsForm : Form
{
    private readonly ReservationMonitorService _monitor;
    private readonly ListView _list = new ListView();
    private readonly AppButton _btnNext;
    private readonly AppButton _btnAll;
    private readonly Label _lblCount = new Label();
    private readonly Label _lblHint = new Label();

    public StaffAlertsForm(ReservationMonitorService monitor)
    {
        _monitor = monitor;

        Text = "Staff Alerts";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(800, 610);

        Controls.Add(Theme.CreateHeader(
            "Grace-period alerts",
            "Seats released after the check-in window. Oldest alerts appear first.",
            this));

        var summary = new CardPanel
        {
            Location = new Point(24, 106),
            Size = new Size(752, 88),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };
        Controls.Add(summary);

        summary.Controls.Add(new Label
        {
            Text = "PENDING ALERTS",
            Location = new Point(20, 17),
            AutoSize = true,
            Font = Theme.Font(8f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        _lblCount.Location = new Point(20, 34);
        _lblCount.Size = new Size(70, 42);
        _lblCount.Font = Theme.Font(24f, FontStyle.Bold);
        _lblCount.ForeColor = Theme.Danger;
        _lblCount.Text = "0";
        summary.Controls.Add(_lblCount);

        _lblHint.Location = new Point(94, 45);
        _lblHint.Size = new Size(630, 24);
        _lblHint.Font = Theme.Font(9.5f);
        _lblHint.ForeColor = Theme.Muted;
        _lblHint.Text = "You are all caught up.";
        summary.Controls.Add(_lblHint);

        var listCard = new CardPanel
        {
            Location = new Point(24, 210),
            Size = new Size(752, 306),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(16, 14, 16, 16)
        };
        Controls.Add(listCard);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.GridLines = false;
        _list.BorderStyle = BorderStyle.None;
        _list.BackColor = Theme.Surface;
        _list.ForeColor = Theme.Ink;
        _list.Font = Theme.Font(9.25f);
        _list.Columns.Add("Time", 78);
        _list.Columns.Add("Spot / Room", 138);
        _list.Columns.Add("User", 190);
        _list.Columns.Add("Reserved for", 142);
        _list.Columns.Add("Note", 168);
        listCard.Controls.Add(_list);

        _btnNext = Theme.MakeButton("Acknowledge next", AppButtonKind.Primary, 180, 40);
        _btnAll = Theme.MakeButton("Acknowledge all", AppButtonKind.Secondary, 166, 40);
        var btnClose = Theme.MakeButton("Close", AppButtonKind.Secondary, 118, 40);

        _btnNext.Location = new Point(24, 536);
        _btnAll.Location = new Point(216, 536);
        btnClose.Location = new Point(658, 536);

        _btnNext.Click += (s, e) => _monitor.AcknowledgeNext();
        _btnAll.Click += (s, e) => _monitor.AcknowledgeAll();
        btnClose.Click += (s, e) => Close();
        CancelButton = btnClose;

        Controls.Add(_btnNext);
        Controls.Add(_btnAll);
        Controls.Add(btnClose);

        _monitor.AlertsChanged += Reload;
        FormClosed += (s, e) => _monitor.AlertsChanged -= Reload;

        Reload();
    }

    private void Reload()
    {
        List<StaffNotification> pending = _monitor.GetPendingAlerts();

        _list.BeginUpdate();
        _list.Items.Clear();

        if (pending.Count == 0)
        {
            var empty = new ListViewItem("No pending alerts");
            empty.SubItems.Add("—");
            empty.SubItems.Add("—");
            empty.SubItems.Add("—");
            empty.SubItems.Add("Everything is up to date");
            empty.ForeColor = Theme.Muted;
            _list.Items.Add(empty);
        }
        else
        {
            foreach (StaffNotification alert in pending)
            {
                var row = new ListViewItem(alert.RaisedAt.ToString("HH:mm:ss"));
                row.SubItems.Add(alert.SpotOrRoom);
                row.SubItems.Add($"{alert.UserName} ({alert.UserIdNumber})");
                row.SubItems.Add(alert.ReservedFor.ToString("dd MMM HH:mm"));
                row.SubItems.Add("Released after grace period");
                row.Tag = alert;
                _list.Items.Add(row);
            }
        }

        _list.EndUpdate();

        _lblCount.Text = pending.Count.ToString();
        _lblCount.ForeColor = pending.Count > 0 ? Theme.Danger : Theme.Success;
        _lblHint.Text = pending.Count > 0
            ? "Acknowledge alerts after the front desk has followed up."
            : "You are all caught up. New alerts will appear here automatically.";

        _btnNext.Enabled = pending.Count > 0;
        _btnAll.Enabled = pending.Count > 0;
    }
}
