namespace MMCL_CLIRRS;

// A simple start screen so every functionality can be opened from one place.
// When your groupmates finish theirs, they just add one more button here.
public class MainMenuForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationManagementService _management;
    private readonly ReservationSearchService _search;

    // CHANGE THIS PIN before the demo.
    private readonly StaffAccessControl _staffAccess = new StaffAccessControl("2468");

    public MainMenuForm(ReservationManagementService management, ReservationSearchService search)
    {
        _management = management;
        _search = search;

        Text = "MMCL CLIRRS";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 360);

        Controls.Add(new Label
        {
            Text = "MMCL CLIR Reservation System",
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Maroon,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });

        Button btnMap = MakeMenuButton("Interactive Seat Map", 100);
        Button btnStaff = MakeMenuButton("Staff: Search and Manage Reservations", 170);
        Button btnStats = MakeMenuButton("Staff: Reservation Statistics and Reports", 240);
        btnStats.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        btnMap.Click += (s, e) => { using var f = new SeatMapForm(_management); f.ShowDialog(this); };
        btnStaff.Click += (s, e) => OpenStaffScreen();
        btnStats.Click += (s, e) => OpenStatisticsScreen();

        Controls.Add(btnMap);
        Controls.Add(btnStaff);
        Controls.Add(btnStats);
    }

    // Students never see staff screens: they open only after the correct PIN.
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
        if (pin == null) return false;   // cancelled

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

    private static Button MakeMenuButton(string text, int y)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(50, y),
            Size = new Size(320, 52),
            BackColor = Color.White,
            ForeColor = Maroon,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = Maroon;
        button.FlatAppearance.BorderSize = 2;
        return button;
    }
}