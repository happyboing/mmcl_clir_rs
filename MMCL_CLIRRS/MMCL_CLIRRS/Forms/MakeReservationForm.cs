namespace MMCL_CLIRRS;

// Screen for reserving a seat or discussion room (Functionality #1).
// Built in code (no designer) so every control is visible and easy to follow.
public class MakeReservationForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationManagementService _service;

    // ----- the controls (text boxes, drop-downs, buttons...) -----
    private readonly TextBox _txtName = new TextBox();
    private readonly TextBox _txtIdNumber = new TextBox();
    private readonly ComboBox _cmbUserType = new ComboBox();
    private readonly ComboBox _cmbDepartment = new ComboBox();
    private readonly ComboBox _cmbProgram = new ComboBox();
    private readonly NumericUpDown _numYear = new NumericUpDown();

    private readonly ComboBox _cmbSpotType = new ComboBox();
    private readonly ComboBox _cmbSpot = new ComboBox();
    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpStart = new DateTimePicker();
    private readonly DateTimePicker _dtpEnd = new DateTimePicker();

    private readonly Label _lblAvailability = new Label();
    private readonly ListBox _lstBooked = new ListBox();

    public MakeReservationForm(ReservationManagementService service)
    {
        _service = service;

        // ----- the window itself -----
        Text = "Reserve a Seat or Room";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(450, 720);

        // ----- red title bar -----
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

        // ----- section 1: who is reserving -----
        AddSectionTitle("Your details", 62);
        AddRow("Full name:", _txtName, 92);
        AddRow("ID number:", _txtIdNumber, 130);

        _cmbUserType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbUserType.Items.AddRange(new object[] { "Student", "Faculty" });
        _cmbUserType.SelectedIndex = 0;
        AddRow("I am a:", _cmbUserType, 168);

        _cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbDepartment.Items.AddRange(AcademicPrograms.Departments);
        AddRow("Department:", _cmbDepartment, 206);
        _cmbDepartment.SelectedIndex = 0;

        _cmbProgram.DropDownStyle = ComboBoxStyle.DropDownList;
        AddRow("Program:", _cmbProgram, 244);
        _cmbDepartment.SelectedIndexChanged += (s, e) => FillPrograms();

        _numYear.Minimum = 1;
        _numYear.Maximum = 6;
        AddRow("Year level:", _numYear, 282);

        // ----- section 2: what they want to reserve -----
        AddSectionTitle("Reservation   (students: today only | faculty: today or later)", 326);

        _cmbSpotType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSpotType.Items.AddRange(new object[] { "Seat / Table", "Discussion Room" });
        _cmbSpotType.SelectedIndex = 0;
        AddRow("Reserve a:", _cmbSpotType, 356);

        _cmbSpot.DropDownStyle = ComboBoxStyle.DropDownList;
        AddRow("Spot / Room:", _cmbSpot, 394);

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.MinDate = DateTime.Today;      // cannot pick the past
        _dtpDate.Value = DateTime.Today;
        AddRow("Date:", _dtpDate, 432);

        int nextHour = Math.Min(DateTime.Now.Hour + 1, 22);
        SetupTimePicker(_dtpStart, DateTime.Today.AddHours(nextHour));
        SetupTimePicker(_dtpEnd, DateTime.Today.AddHours(nextHour + 1));
        AddRow("Start time:", _dtpStart, 470);
        AddRow("End time:", _dtpEnd, 508);

        // ----- live availability message + list of already-booked times -----
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

        // ----- buttons -----
        Button btnReserve = MakeButton("Reserve", Maroon, Color.White, 200);
        Button btnClose = MakeButton("Close", Color.White, Maroon, 320);
        btnReserve.Click += btnReserve_Click;
        btnClose.Click += (s, e) => Close();
        AcceptButton = btnReserve;
        CancelButton = btnClose;
        Controls.Add(btnReserve);
        Controls.Add(btnClose);

        // ----- fill the spot list, then start listening for changes -----
        FillPrograms();
        FillSpotList();
        ApplyUserTypeRules();

        _cmbUserType.SelectedIndexChanged += (s, e) => ApplyUserTypeRules();
        _cmbSpotType.SelectedIndexChanged += (s, e) => FillSpotList();
        _cmbSpot.SelectedIndexChanged += (s, e) => RefreshAvailability();
        _dtpDate.ValueChanged += (s, e) => RefreshAvailability();
        _dtpStart.ValueChanged += (s, e) => RefreshAvailability();
        _dtpEnd.ValueChanged += (s, e) => RefreshAvailability();

        RefreshAvailability();
    }

    // =========================================================
    //  What happens when things change or get clicked
    // =========================================================

    private void FillPrograms()
    {
        _cmbProgram.Items.Clear();

        if (_cmbDepartment.SelectedItem is not string department) return;

        foreach (string program in AcademicPrograms.GetPrograms(department))
            _cmbProgram.Items.Add(program);

        if (_cmbProgram.Items.Count > 0)
            _cmbProgram.SelectedIndex = 0;
    }

    // Seat / Table -> show seats.  Discussion Room -> show rooms.
    private void FillSpotList()
    {
        _cmbSpot.Items.Clear();

        List<string> spots = _cmbSpotType.SelectedIndex == 0
            ? ClirLayout.GetSeats()
            : ClirLayout.GetDiscussionRooms();

        _cmbSpot.Items.AddRange(spots.ToArray());
        _cmbSpot.SelectedIndex = 0;      // this also triggers RefreshAvailability
    }

    // Students: date is locked to today, year level is needed.
    // Faculty: can pick upcoming dates, year level does not apply.
    private void ApplyUserTypeRules()
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;

        if (isStudent) _dtpDate.Value = DateTime.Today;
        _dtpDate.Enabled = !isStudent;
        _numYear.Enabled = isStudent;
    }

    // Shows green "Available" or red "Not available" while the user picks the time.
    private void RefreshAvailability()
    {
        if (_cmbSpot.SelectedItem is not string spot) return;

        DateTime date = _dtpDate.Value.Date;
        TimeSpan start = _dtpStart.Value.TimeOfDay;
        TimeSpan end = _dtpEnd.Value.TimeOfDay;

        // List the times that are already taken (earliest first).
        _lstBooked.Items.Clear();
        List<Reservation> booked = _service.GetBookedSlots(spot, date);
        if (booked.Count == 0)
            _lstBooked.Items.Add("(nothing yet - the whole day is free)");
        foreach (Reservation r in booked)
            _lstBooked.Items.Add($"{Clock(r.StartTime)} - {Clock(r.EndTime)}");

        if (end <= start)
        {
            _lblAvailability.ForeColor = Color.DarkOrange;
            _lblAvailability.Text = "End time must be after the start time.";
        }
        else if (_service.IsAvailable(spot, date, start, end))
        {
            _lblAvailability.ForeColor = Color.SeaGreen;
            _lblAvailability.Text = "Available at this time.";
        }
        else
        {
            _lblAvailability.ForeColor = Maroon;
            _lblAvailability.Text = "Not available - already reserved at this time.";
        }
    }

    private void btnReserve_Click(object? sender, EventArgs e)
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;

        // 1. Collect everything the user typed into a request.
        var request = new ReservationRequest
        {
            UserName = _txtName.Text.Trim(),
            UserIdNumber = _txtIdNumber.Text.Trim(),
            UserType = isStudent ? UserType.Student : UserType.Faculty,
            Department = _cmbDepartment.Text.Trim(),
            Program = _cmbProgram.Text.Trim(),
            YearLevel = isStudent ? (int)_numYear.Value : 0,
            SpotOrRoom = _cmbSpot.Text,
            IsDiscussionRoom = _cmbSpotType.SelectedIndex == 1,
            Date = _dtpDate.Value.Date,
            StartTime = _dtpStart.Value.TimeOfDay,
            EndTime = _dtpEnd.Value.TimeOfDay
        };

        // 2. Hand it to the service. It checks the rules and uses the queue.
        OperationResult result = _service.SubmitRequest(request);

        // 3. Show the answer.
        MessageBox.Show(result.Message,
            result.Success ? "Reservation confirmed" : "Could not reserve",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success)
        {
            _txtName.Clear();
            _txtIdNumber.Clear();
        }
        RefreshAvailability();
    }

    // =========================================================
    //  Small helpers that build the screen
    // =========================================================

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

    private static void SetupTimePicker(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";      // 24-hour clock, e.g. 14:00
        picker.ShowUpDown = true;           // little up/down arrows instead of a calendar
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

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
