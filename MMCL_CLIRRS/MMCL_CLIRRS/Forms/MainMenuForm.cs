using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

public class MainMenuForm : Form
{
    private readonly ReservationManagementService _management;
    private readonly ReservationSearchService _search;
    private readonly ReservationMonitorService _monitor;
    private readonly MenuCard _alertsCard;
    private readonly System.Windows.Forms.Timer _monitorTimer = new();

    private readonly StaffAccessControl _staffAccess = new StaffAccessControl("2468");

    public MainMenuForm(ReservationManagementService management, ReservationSearchService search,
                        ReservationMonitorService monitor)
    {
        _management = management;
        _search = search;
        _monitor = monitor;

        Text = "MMCL CLIRRS";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 580);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        shell.Controls.Add(BuildBrandPanel(), 0, 0);
        shell.Controls.Add(BuildActionPanel(), 1, 0);
        Controls.Add(shell);

        var mapCard = MakeCard("Interactive seat map", "\uE707", Theme.Primary, 146);
        var manageCard = MakeCard("Search & manage", "\uE721", Theme.Info, 242);
        var reportsCard = MakeCard("Reports & statistics", "\uE9D9", Theme.Gold, 338);
        _alertsCard = MakeCard("Grace-period alerts", "\uE7BA", Theme.Warning, 434);

        var actionPanel = (Panel)shell.GetControlFromPosition(1, 0)!;
        actionPanel.Controls.Add(mapCard);
        actionPanel.Controls.Add(manageCard);
        actionPanel.Controls.Add(reportsCard);
        actionPanel.Controls.Add(_alertsCard);

        mapCard.Click += (s, e) => { using var f = new SeatMapForm(_management, _monitor); f.ShowDialog(this); };
        manageCard.Click += (s, e) => OpenStaffScreen();
        reportsCard.Click += (s, e) => OpenStatisticsScreen();
        _alertsCard.Click += (s, e) => OpenAlertsScreen();

        _monitor.AlertRaised += OnAlertRaised;
        _monitor.AlertsChanged += UpdateAlertsCard;
        _monitorTimer.Interval = 1000;
        _monitorTimer.Tick += (s, e) => _monitor.ProcessDue(DateTime.Now);
        _monitorTimer.Start();

        FormClosed += (s, e) =>
        {
            _monitorTimer.Stop();
            _monitorTimer.Dispose();
            _monitor.AlertRaised -= OnAlertRaised;
            _monitor.AlertsChanged -= UpdateAlertsCard;
        };

        UpdateAlertsCard();
    }

    private static Panel BuildBrandPanel()
    {
        var brand = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.PrimaryDark
        };

        brand.Paint += (s, e) =>
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var gradient = new LinearGradientBrush(
                brand.ClientRectangle, Theme.PrimaryDark, Theme.Primary, LinearGradientMode.Vertical);
            g.FillRectangle(gradient, brand.ClientRectangle);

            using var ringPen = new Pen(Color.FromArgb(28, 255, 255, 255), 1f);
            g.DrawEllipse(ringPen, 146, -92, 230, 230);
            g.DrawEllipse(ringPen, 176, 414, 190, 190);
            g.DrawEllipse(ringPen, -96, 300, 210, 210);
        };

        brand.Controls.Add(new Label
        {
            Text = "CLIR",
            Location = new Point(0, 230),
            Size = new Size(260, 64),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Font(30f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        });

        brand.Controls.Add(new Label
        {
            Text = "Reservation System",
            Location = new Point(0, 298),
            Size = new Size(260, 26),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Font(11f, FontStyle.Bold),
            ForeColor = Theme.Gold,
            BackColor = Color.Transparent
        });

        return brand;
    }

    private static Panel BuildActionPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(32, 0, 32, 0)
        };

        panel.Controls.Add(new Label
        {
            Text = "Reservation desk",
            Location = new Point(32, 34),
            AutoSize = true,
            Font = Theme.Font(22f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        panel.Controls.Add(new Label
        {
            Text = "Choose an action to get started.",
            Location = new Point(34, 76),
            AutoSize = true,
            Font = Theme.Font(10f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        panel.Controls.Add(new Label
        {
            Text = "STUDENT & STAFF TOOLS",
            Location = new Point(34, 116),
            AutoSize = true,
            Font = Theme.Font(8f, FontStyle.Bold),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        return panel;
    }

    private static MenuCard MakeCard(string title, string icon,
                                     Color accent, int y)
    {
        return new MenuCard
        {
            Title = title,
            Icon = icon,
            Accent = accent,
            Location = new Point(32, y),
            Size = new Size(496, 88),
            AccessibleName = title
        };
    }

    private void OnAlertRaised(StaffNotification alert)
    {
        System.Media.SystemSounds.Exclamation.Play();
    }

    private void UpdateAlertsCard()
    {
        int count = _monitor.PendingAlertCount;
        bool hasAlerts = count > 0;

        _alertsCard.Badge = hasAlerts ? count.ToString() : "";
        _alertsCard.Accent = hasAlerts ? Theme.Danger : Theme.Warning;
    }

    private void OpenAlertsScreen()
    {
        if (!TryStaffAccess()) return;

        using var form = new StaffAlertsForm(_monitor);
        form.ShowDialog(this);
    }

    private void OpenStaffScreen()
    {
        if (!TryStaffAccess()) return;

        using var form = new ReservationSearchForm(_search);
        form.ShowDialog(this);
    }

    private void OpenStatisticsScreen()
    {
        if (!TryStaffAccess()) return;

        using var form = new ReservationStatisticsForm(_search);
        form.ShowDialog(this);
    }

    private bool TryStaffAccess()
    {
        if (_staffAccess.IsLockedOut(out int secondsLeft))
        {
            MessageBox.Show($"Too many wrong attempts. Please try again in {secondsLeft} seconds.", "Locked");
            return false;
        }

        string? pin = PromptDialog.Show(this, "Staff Access", "Enter the staff PIN:",
                                        maskInput: true, okText: "Enter");
        if (pin == null) return false;

        if (!_staffAccess.TryUnlock(pin))
        {
            MessageBox.Show(
                _staffAccess.IsLockedOut(out int wait)
                    ? $"Too many wrong attempts. Locked for {wait} seconds."
                    : "Incorrect PIN.",
                "Access denied");
            return false;
        }

        return true;
    }
}
