namespace MMCL_CLIRRS;

public class MakeReservationForm : Form
{
    private readonly ReservationManagementService _service;

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

        Text = "Reserve a Seat or Room";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(800, 670);

        Controls.Add(Theme.CreateHeader(
            "Reserve a seat or room",
            "Students can book for today. Faculty can book for today or a future date.",
            this, null, false, height: 68));

        var detailsCard = BuildCard("Your details", "Who is reserving?", 24, 88, 366, 500);
        var reservationCard = BuildCard("Reservation", "Choose a time and place.", 410, 88, 366, 500);
        Controls.Add(detailsCard);
        Controls.Add(reservationCard);

        _cmbUserType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbUserType.Items.AddRange(new object[] { "Student", "Faculty" });
        _cmbUserType.SelectedIndex = 0;

        _cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbDepartment.Items.AddRange(AcademicPrograms.Departments);
        _cmbDepartment.SelectedIndex = 0;

        _cmbProgram.DropDownStyle = ComboBoxStyle.DropDownList;

        _numYear.Minimum = 1;
        _numYear.Maximum = 6;

        Theme.AddField(detailsCard, "Full name", _txtName, 20, 64, 326);
        Theme.AddField(detailsCard, "ID number", _txtIdNumber, 20, 124, 326);
        Theme.AddField(detailsCard, "I am a", _cmbUserType, 20, 184, 326);
        Theme.AddField(detailsCard, "Department", _cmbDepartment, 20, 244, 326);
        Theme.AddField(detailsCard, "Program", _cmbProgram, 20, 304, 326);
        Theme.AddField(detailsCard, "Year level", _numYear, 20, 364, 326);

        _cmbSpotType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSpotType.Items.AddRange(new object[] { "Seat / Table", "Discussion Room" });
        _cmbSpotType.SelectedIndex = 0;

        _cmbSpot.DropDownStyle = ComboBoxStyle.DropDownList;

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.MinDate = DateTime.Today;
        _dtpDate.Value = DateTime.Today;

        int nextHour = Math.Min(DateTime.Now.Hour + 1, 22);
        SetupTimePicker(_dtpStart, DateTime.Today.AddHours(nextHour));
        SetupTimePicker(_dtpEnd, DateTime.Today.AddHours(nextHour + 1));

        Theme.AddField(reservationCard, "Reserve a", _cmbSpotType, 20, 64, 326);
        Theme.AddField(reservationCard, "Spot / Room", _cmbSpot, 20, 124, 326);
        Theme.AddField(reservationCard, "Date", _dtpDate, 20, 184, 326);
        Theme.AddField(reservationCard, "Start time", _dtpStart, 20, 244, 326);
        Theme.AddField(reservationCard, "End time", _dtpEnd, 20, 304, 326);

        _lblAvailability.Location = new Point(20, 374);
        _lblAvailability.Size = new Size(326, 32);
        _lblAvailability.TextAlign = ContentAlignment.MiddleLeft;
        _lblAvailability.Padding = new Padding(10, 0, 6, 0);
        _lblAvailability.BackColor = Theme.SurfaceAlt;
        _lblAvailability.Font = Theme.Font(9.25f, FontStyle.Bold);
        _lblAvailability.ForeColor = Theme.Muted;
        _lblAvailability.Text = "Choose a time to check availability.";
        reservationCard.Controls.Add(_lblAvailability);

        Theme.AddReadOnlyList(reservationCard, "Already booked on this date", _lstBooked,
                              20, 416, 326, 60);

        var btnReserve = Theme.MakeButton("Reserve", AppButtonKind.Primary, 130, 40);
        var btnClose = Theme.MakeButton("Close", AppButtonKind.Secondary, 126, 40);
        btnReserve.Location = new Point(506, 602);
        btnClose.Location = new Point(650, 602);
        btnReserve.Click += btnReserve_Click;
        btnClose.Click += (s, e) => Close();
        AcceptButton = btnReserve;
        CancelButton = btnClose;
        Controls.Add(btnReserve);
        Controls.Add(btnClose);

        _cmbDepartment.SelectedIndexChanged += (s, e) => FillPrograms();
        _cmbUserType.SelectedIndexChanged += (s, e) => ApplyUserTypeRules();
        _cmbSpotType.SelectedIndexChanged += (s, e) => FillSpotList();
        _cmbSpot.SelectedIndexChanged += (s, e) => RefreshAvailability();
        _dtpDate.ValueChanged += (s, e) => RefreshAvailability();
        _dtpStart.ValueChanged += (s, e) => RefreshAvailability();
        _dtpEnd.ValueChanged += (s, e) => RefreshAvailability();

        FillPrograms();
        FillSpotList();
        ApplyUserTypeRules();
        RefreshAvailability();
    }

    private static CardPanel BuildCard(string title, string subtitle, int x, int y, int width, int height)
    {
        var card = new CardPanel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };

        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(20, 16),
            AutoSize = true,
            Font = Theme.Font(12f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        card.Controls.Add(new Label
        {
            Text = subtitle,
            Location = new Point(21, 42),
            AutoSize = true,
            Font = Theme.Font(8.5f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        return card;
    }

    private void FillPrograms()
    {
        _cmbProgram.Items.Clear();

        if (_cmbDepartment.SelectedItem is not string department) return;

        foreach (string program in AcademicPrograms.GetPrograms(department))
            _cmbProgram.Items.Add(program);

        if (_cmbProgram.Items.Count > 0)
            _cmbProgram.SelectedIndex = 0;
    }

    private void FillSpotList()
    {
        _cmbSpot.Items.Clear();

        List<string> spots = _cmbSpotType.SelectedIndex == 0
            ? ClirLayout.GetSeats()
            : ClirLayout.GetDiscussionRooms();

        _cmbSpot.Items.AddRange(spots.ToArray());
        _cmbSpot.SelectedIndex = 0;
    }

    private void ApplyUserTypeRules()
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;

        if (isStudent) _dtpDate.Value = DateTime.Today;
        _dtpDate.Enabled = !isStudent;
        _numYear.Enabled = isStudent;
    }

    private void RefreshAvailability()
    {
        if (_cmbSpot.SelectedItem is not string spot) return;

        DateTime date = _dtpDate.Value.Date;
        TimeSpan start = _dtpStart.Value.TimeOfDay;
        TimeSpan end = _dtpEnd.Value.TimeOfDay;

        _lstBooked.Items.Clear();
        List<Reservation> booked = _service.GetBookedSlots(spot, date);
        if (booked.Count == 0)
            _lstBooked.Items.Add("Nothing yet — the whole day is free.");
        foreach (Reservation r in booked)
            _lstBooked.Items.Add($"{Clock(r.StartTime)} – {Clock(r.EndTime)}");

        if (end <= start)
        {
            _lblAvailability.ForeColor = Theme.Warning;
            _lblAvailability.Text = "End time must be after the start time.";
        }
        else if (_service.IsAvailable(spot, date, start, end))
        {
            _lblAvailability.ForeColor = Theme.Success;
            _lblAvailability.Text = "Available at this time.";
        }
        else
        {
            _lblAvailability.ForeColor = Theme.Danger;
            _lblAvailability.Text = "Not available — already reserved at this time.";
        }
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
            Program = _cmbProgram.Text.Trim(),
            YearLevel = isStudent ? (int)_numYear.Value : 0,
            SpotOrRoom = _cmbSpot.Text,
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
        }
        RefreshAvailability();
    }

    private static void SetupTimePicker(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = value;
    }

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
