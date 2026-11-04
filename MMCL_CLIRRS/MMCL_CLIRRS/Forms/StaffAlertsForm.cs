namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #2: Reservation Scheduling and Availability
//  The staff alert screen. Every alert here means a reserved seat reached the
//  5-minute grace period without being occupied and was released.
//  Alerts are listed oldest first (they wait in a queue).
// ============================================================
public class StaffAlertsForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationMonitorService _monitor;
    private readonly ListBox _list = new ListBox();
    private readonly Button _btnNext = new Button();
    private readonly Button _btnAll = new Button();

    public StaffAlertsForm(ReservationMonitorService monitor)
    {
        _monitor = monitor;

        Text = "Staff Alerts";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 440);

        var header = new Label
        {
            Text = "Grace-period alerts (oldest first)",
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Maroon,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        _list.Dock = DockStyle.Fill;
        _list.BorderStyle = BorderStyle.None;
        _list.HorizontalScrollbar = true;
        _list.IntegralHeight = false;

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            BackColor = Color.FromArgb(246, 247, 249)
        };

        StyleButton(_btnNext, "Acknowledge Next", Maroon, Color.White, 12);
        StyleButton(_btnAll, "Acknowledge All", Color.White, Maroon, 190);
        var btnClose = new Button();
        StyleButton(btnClose, "Close", Color.White, Maroon, 728);
        btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnNext.Click += (s, e) => _monitor.AcknowledgeNext();
        _btnAll.Click += (s, e) => _monitor.AcknowledgeAll();
        btnClose.Click += (s, e) => Close();
        CancelButton = btnClose;

        footer.Controls.Add(_btnNext);
        footer.Controls.Add(_btnAll);
        footer.Controls.Add(btnClose);

        // The fill control goes in first, then the docked header and footer.
        Controls.Add(_list);
        Controls.Add(footer);
        Controls.Add(header);

        // Stay up to date while open: new alerts arrive and acknowledged ones disappear.
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
            _list.Items.Add("No pending alerts.");
        }
        else
        {
            foreach (StaffNotification alert in pending)
                _list.Items.Add(alert);
        }

        _list.EndUpdate();

        _btnNext.Enabled = pending.Count > 0;
        _btnAll.Enabled = pending.Count > 0;
    }

    private static void StyleButton(Button button, string text, Color back, Color fore, int x)
    {
        button.Text = text;
        button.Location = new Point(x, 10);
        button.Size = new Size(160, 38);
        button.BackColor = back;
        button.ForeColor = fore;
        button.FlatStyle = FlatStyle.Flat;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.FlatAppearance.BorderColor = Maroon;
    }
}
