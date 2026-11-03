namespace MMCL_CLIRRS;

// Reservation entry screen. The list of locations is refreshed periodically
// so a spot reserved by another user disappears from this user's choices.
public class MakeReservationForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationManagementService _service;
    private readonly System.Windows.Forms.Timer _availabilityTimer = new();
    private readonly List<string> _allSpots = new();
    private bool _updatingSpotOptions;
    private Button? _btnReserve;

    private readonly TextBox _txtName = new();
    private readonly TextBox _txtIdNumber = new();
    private readonly ComboBox _cmbUserType = new();
    private readonly ComboBox _cmbDepartment = new();
    private readonly TextBox _txtProgram = new();
    private readonly NumericUpDown _numYear = new();

    private readonly ComboBox _cmbSpotType = new();
    private readonly ComboBox _cmbSpot = new();
    private readonly DateTimePicker _dtpDate = new();
    private readonly DateTimePicker _dtpStart = new();
    private readonly DateTimePicker _dtpEnd = new();

    private readonly Label _lblAvailability = new();
    private readonly ListBox _lstBooked = new();

    public MakeReservationForm(ReservationManagementService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        Text = "Reserve a Seat or Room";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(450, 720);

        Controls.Add(new Label
        {
            Text = "Reserve a Seat or Room",
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Maroon,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        });

        // User information.
        AddSectionTitle("Your details", 62);
        AddRow("Full name:", _txtName, 92);
        AddRow("ID number:", _txtIdNumber, 130);

        _cmbUserType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbUserType.Items.AddRange(new object[] { "Student", "Faculty" });
        _cmbUserType.SelectedIndex = 0;
        AddRow("I am a:", _cmbUserType, 168);

        _cmbDepartment.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbDepartment.Items.AddRange(new object[]
        {
            "School of Information Technology",
            "School of Engineering",
            "School of Architecture, Industrial Design and the Built Environment",
            "School of Management",
            "School of Health Sciences"
        });
        AddRow("Department:", _cmbDepartment, 206);
        AddRow("Program:", _txtProgram, 244);

        _numYear.Minimum = 1;
        _numYear.Maximum = 6;
        AddRow("Year level:", _numYear, 282);

        // Reservation preferences.
        AddSectionTitle("Reservation   (students: today only | faculty: today or later)", 326);

        _cmbSpotType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSpotType.Items.AddRange(new object[] { "Seat / Table", "Discussion Room" });
        _cmbSpotType.SelectedIndex = 0;
        AddRow("Reserve a:", _cmbSpotType, 356);

        _cmbSpot.DropDownStyle = ComboBoxStyle.DropDownList;
        AddRow("Available spot:", _cmbSpot, 394);

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.MinDate = DateTime.Today;
        _dtpDate.Value = DateTime.Today;
        AddRow("Date:", _dtpDate, 432);

        DateTime defaultStart = GetDefaultStartTime();
        DateTime defaultEnd = defaultStart.AddHours(1);
        if (defaultEnd.Date != DateTime.Today)
            defaultEnd = DateTime.Today.AddHours(23).AddMinutes(59);

        SetupTimePicker(_dtpStart, defaultStart);
        SetupTimePicker(_dtpEnd, defaultEnd);
        AddRow("Start time:", _dtpStart, 470);
        AddRow("End time:", _dtpEnd, 508);

        // Live availability indicator and the other reservations for the
        // selected location on the chosen date.
        _lblAvailability.Location = new Point(20, 552);
        _lblAvailability.Size = new Size(410, 26);
        _lblAvailability.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        Controls.Add(_lblAvailability);

        Controls.Add(new Label
        {
            Text = "Already booked for this spot on this date:",
            Location = new Point(20, 584),
            AutoSize = true,
            ForeColor = Color.DimGray
        });
        _lstBooked.Location = new Point(20, 608);
        _lstBooked.Size = new Size(410, 52);
        Controls.Add(_lstBooked);

        Button btnReserve = MakeButton("Reserve", Maroon, Color.White, 200);
        Button btnClose = MakeButton("Close", Color.White, Maroon, 320);
        _btnReserve = btnReserve;
        btnReserve.Click += btnReserve_Click;
        btnClose.Click += (s, e) => Close();
        AcceptButton = btnReserve;
        CancelButton = btnClose;
        Controls.Add(btnReserve);
        Controls.Add(btnClose);

        FillSpotList();
        ApplyUserTypeRules();

        _cmbUserType.SelectedIndexChanged += (s, e) => ApplyUserTypeRules();
        _cmbSpotType.SelectedIndexChanged += (s, e) => FillSpotList();
        _cmbSpot.SelectedIndexChanged += (s, e) =>
        {
            if (!_updatingSpotOptions)
                RefreshAvailability();
        };
        _dtpDate.ValueChanged += (s, e) => RefreshAvailableSpotOptions();
        _dtpStart.ValueChanged += (s, e) => RefreshAvailableSpotOptions();
        _dtpEnd.ValueChanged += (s, e) => RefreshAvailableSpotOptions();

        // Availability is read from the current reservation index every time.
        // Polling refreshes the options when another user makes/cancels a booking.
        _availabilityTimer.Interval = 2000;
        _availabilityTimer.Tick += (s, e) => RefreshAvailableSpotOptions();
        _availabilityTimer.Start();
        FormClosed += (s, e) =>
        {
            _availabilityTimer.Stop();
            _availabilityTimer.Dispose();
        };

        RefreshAvailableSpotOptions();
    }

    private void FillSpotList()
    {
        _allSpots.Clear();
        _allSpots.AddRange(_cmbSpotType.SelectedIndex == 0
            ? ClirLayout.GetSeats()
            : ClirLayout.GetDiscussionRooms());

        RefreshAvailableSpotOptions();
    }

    // Students are restricted to the current date. Faculty may select today
    // or a later date. The service validates the rule again at submission.
    private void ApplyUserTypeRules()
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;
        if (isStudent && _dtpDate.Value.Date != DateTime.Today)
            _dtpDate.Value = DateTime.Today;

        _dtpDate.Enabled = !isStudent;
        _numYear.Enabled = isStudent;
        RefreshAvailableSpotOptions();
    }

    private void RefreshAvailableSpotOptions()
    {
        if (_updatingSpotOptions || _allSpots.Count == 0 || IsDisposed)
            return;

        // Keep the picker aligned with the current day if the application was
        // left open overnight. Assign Value before MinDate to avoid an invalid
        // DateTimePicker range.
        if (_dtpDate.MinDate.Date < DateTime.Today)
        {
            _updatingSpotOptions = true;
            try
            {
                if (_dtpDate.Value.Date < DateTime.Today)
                    _dtpDate.Value = DateTime.Today;
                _dtpDate.MinDate = DateTime.Today;
            }
            finally
            {
                _updatingSpotOptions = false;
            }
        }

        // Process overdue no-shows before rebuilding the list so released
        // seats appear as soon as their grace period has ended.
        _service.ProcessDueReservations();

        string? previousSelection = _cmbSpot.SelectedItem as string;
        DateTime date = _dtpDate.Value.Date;
        TimeSpan start = _dtpStart.Value.TimeOfDay;
        TimeSpan end = _dtpEnd.Value.TimeOfDay;

        // While the time range is incomplete/invalid, keep all locations in
        // the list so the user can correct the time. For a valid future range,
        // only show locations with no overlapping Active or Occupied booking.
        bool validRange = end > start;
        bool dateIsCurrentOrFuture = date.Date >= DateTime.Today;
        bool startIsFuture = date.Date > DateTime.Today || start >= DateTime.Now.TimeOfDay;
        List<string> available = validRange && dateIsCurrentOrFuture && startIsFuture
            ? _allSpots.Where(spot => _service.IsAvailable(spot, date, start, end)).ToList()
            : new List<string>(_allSpots);

        _updatingSpotOptions = true;
        try
        {
            _cmbSpot.BeginUpdate();
            _cmbSpot.Items.Clear();
            _cmbSpot.Items.AddRange(available.Cast<object>().ToArray());

            if (previousSelection != null && available.Contains(previousSelection))
                _cmbSpot.SelectedItem = previousSelection;
            else if (_cmbSpot.Items.Count > 0)
                _cmbSpot.SelectedIndex = 0;
            else
                _cmbSpot.SelectedIndex = -1;
        }
        finally
        {
            _cmbSpot.EndUpdate();
            _updatingSpotOptions = false;
        }

        RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        _lstBooked.Items.Clear();
        DateTime date = _dtpDate.Value.Date;
        TimeSpan start = _dtpStart.Value.TimeOfDay;
        TimeSpan end = _dtpEnd.Value.TimeOfDay;

        if (date.Date < DateTime.Today)
        {
            SetAvailability("That date has already passed. Choose today or a future date.", Color.DarkOrange, false);
            return;
        }

        if (end <= start)
        {
            SetAvailability("End time must be after the start time.", Color.DarkOrange, false);
            return;
        }

        if (date == DateTime.Today && start < DateTime.Now.TimeOfDay)
        {
            SetAvailability("That start time has already passed. Choose a later time.", Color.DarkOrange, false);
            return;
        }

        if (_cmbSpot.SelectedItem is not string spot)
        {
            _lstBooked.Items.Add("No spaces are available for the selected time.");
            SetAvailability("No available seats / rooms for this date and time.", Maroon, false);
            return;
        }

        List<Reservation> booked = _service.GetBookedSlots(spot, date);
        if (booked.Count == 0)
            _lstBooked.Items.Add("No other reservations for this spot on this date.");
        else
        {
            foreach (Reservation reservation in booked)
            {
                _lstBooked.Items.Add(
                    $"{Clock(reservation.StartTime)} - {Clock(reservation.EndTime)} ({reservation.Status})");
            }
        }

        bool available = _service.IsAvailable(spot, date, start, end);
        SetAvailability(
            available ? "Available at this time." : "Not available - already reserved at this time.",
            available ? Color.SeaGreen : Maroon,
            available);
    }

    private void SetAvailability(string message, Color color, bool canReserve)
    {
        _lblAvailability.ForeColor = color;
        _lblAvailability.Text = message;
        if (_btnReserve != null)
            _btnReserve.Enabled = canReserve;
    }

    private void btnReserve_Click(object? sender, EventArgs e)
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;

        var request = new ReservationRequest
        {
            UserName = _txtName.Text.Trim(),
            UserIdNumber = _txtIdNumber.Text.Trim(),
            UserType = isStudent ? UserType.Student : UserType.Faculty,
            Department = _cmbDepartment.Text.Trim(),
            Program = _txtProgram.Text.Trim(),
            YearLevel = isStudent ? (int)_numYear.Value : 0,
            SpotOrRoom = _cmbSpot.SelectedItem?.ToString() ?? string.Empty,
            IsDiscussionRoom = _cmbSpotType.SelectedIndex == 1,
            Date = _dtpDate.Value.Date,
            StartTime = _dtpStart.Value.TimeOfDay,
            EndTime = _dtpEnd.Value.TimeOfDay
        };

        OperationResult result = _service.SubmitRequest(request);
        MessageBox.Show(result.Message,
            result.Success ? "Reservation confirmed" : "Could not reserve",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success)
        {
            _txtName.Clear();
            _txtIdNumber.Clear();
            _txtProgram.Clear();
        }

        // Re-query after submission so the reserved location is removed from
        // the available options immediately, without waiting for the timer.
        RefreshAvailableSpotOptions();
    }

    private void AddSectionTitle(string text, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            Location = new Point(20, y),
            AutoSize = true,
            ForeColor = Maroon,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        });
    }

    private void AddRow(string label, Control control, int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
        control.Location = new Point(150, y);
        control.Width = 280;
        Controls.Add(control);
    }

    private static DateTime GetDefaultStartTime()
    {
        DateTime candidate = DateTime.Now.AddMinutes(30);
        candidate = new DateTime(candidate.Year, candidate.Month, candidate.Day,
                                 candidate.Hour, candidate.Minute, 0);
        if (candidate <= DateTime.Now)
            candidate = candidate.AddMinutes(1);

        // This application stores same-day time ranges. If there is no longer
        // a future half-hour slot today, leave a late-day value for the user to
        // change (the service will still reject a start time in the past).
        if (candidate.Date != DateTime.Today)
            candidate = DateTime.Today.AddHours(23);
        return candidate;
    }

    private static void SetupTimePicker(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = value;
    }

    private static Button MakeButton(string text, Color back, Color fore, int x)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 672),
            Size = new Size(110, 36),
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = Maroon;
        return button;
    }

    private static string Clock(TimeSpan time) => time.ToString(@"hh\:mm");
}
