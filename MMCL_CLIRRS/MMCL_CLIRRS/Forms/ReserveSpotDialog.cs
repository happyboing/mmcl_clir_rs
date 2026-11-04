namespace MMCL_CLIRRS;

public class ReserveSpotDialog : Form
{
    private readonly ReservationManagementService _service;
    private readonly ClirMapSpot _spot;
    private readonly AvailabilityService _availability;

    private readonly TextBox _txtName = new TextBox();
    private readonly TextBox _txtIdNumber = new TextBox();
    private readonly ComboBox _cmbUserType = new ComboBox();
    private readonly ComboBox _cmbDepartment = new ComboBox();
    private readonly ComboBox _cmbProgram = new ComboBox();
    private readonly NumericUpDown _numYear = new NumericUpDown();

    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpStart = new DateTimePicker();
    private readonly DateTimePicker _dtpEnd = new DateTimePicker();

    private readonly Label _lblAvailability = new Label();
    private readonly ListBox _lstBooked = new ListBox();

    public DateTime ReservationDate => _dtpDate.Value.Date;
    public TimeSpan ReservationStart => _dtpStart.Value.TimeOfDay;

    public ReserveSpotDialog(ReservationManagementService service,
                             ClirMapSpot spot,
                             DateTime defaultDate,
                             TimeSpan defaultTime)
    {
        _service = service;
        _spot = spot;
        _availability = new AvailabilityService(service);

        Text = "Reserve This Spot";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 670);

        Controls.Add(Theme.CreateHeader(
            "Reserve this spot",
            $"{spot.SpotOrRoom}  •  {spot.TypeLabel}  •  {spot.Area}",
            this, null, false, height: 68));

        var timeCard = BuildCard("Reservation time", "Choose when you would like to use this spot.",
                                 24, 88, 366, 500);
        var detailsCard = BuildCard("Your details", "Who is reserving?",
                                    410, 88, 366, 500);
        Controls.Add(timeCard);
        Controls.Add(detailsCard);

        timeCard.Controls.Add(new Label
        {
            Text = spot.SpotOrRoom,
            Location = new Point(20, 64),
            AutoSize = true,
            Font = Theme.Font(13f, FontStyle.Bold),
            ForeColor = Theme.Primary,
            BackColor = Color.Transparent
        });

        timeCard.Controls.Add(new Label
        {
            Text = $"{spot.TypeLabel}  •  {spot.Area}",
            Location = new Point(21, 92),
            AutoSize = true,
            Font = Theme.Font(8.75f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.MinDate = DateTime.Today;
        _dtpDate.Value = defaultDate.Date < DateTime.Today ? DateTime.Today : defaultDate.Date;

        TimeSpan start = defaultTime;
        if (_dtpDate.Value.Date == DateTime.Today && start <= DateTime.Now.TimeOfDay)
            start = TimeSpan.FromHours(Math.Min(DateTime.Now.Hour + 1, 22));
        start = TimeSpan.FromHours(start.Hours);

        TimeSpan end = start + TimeSpan.FromHours(1);
        if (end > TimeSpan.FromHours(23)) end = TimeSpan.FromHours(23);
        if (end <= start)
        {
            start = TimeSpan.FromHours(22);
            end = TimeSpan.FromHours(23);
        }

        SetupTimePicker(_dtpStart, DateTime.Today.Add(start));
        SetupTimePicker(_dtpEnd, DateTime.Today.Add(end));

        Theme.AddField(timeCard, "Date", _dtpDate, 20, 124, 326);
        Theme.AddField(timeCard, "Start time", _dtpStart, 20, 184, 326);
        Theme.AddField(timeCard, "End time", _dtpEnd, 20, 244, 326);

        _lblAvailability.Location = new Point(20, 312);
        _lblAvailability.Size = new Size(326, 44);
        _lblAvailability.TextAlign = ContentAlignment.MiddleLeft;
        _lblAvailability.Padding = new Padding(10, 0, 6, 0);
        _lblAvailability.BackColor = Theme.SurfaceAlt;
        _lblAvailability.Font = Theme.Font(9.25f, FontStyle.Bold);
        _lblAvailability.ForeColor = Theme.Muted;
        timeCard.Controls.Add(_lblAvailability);

        Theme.AddReadOnlyList(timeCard, "Already booked on this date", _lstBooked,
                              20, 370, 326, 76);

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

        var btnReserve = Theme.MakeButton("Reserve", AppButtonKind.Primary, 130, 40);
        var btnClose = Theme.MakeButton("Close", AppButtonKind.Secondary, 126, 40);
        btnReserve.Location = new Point(526, 602);
        btnClose.Location = new Point(670, 602);
        btnReserve.Click += btnReserve_Click;
        btnClose.Click += (s, e) => Close();
        AcceptButton = btnReserve;
        CancelButton = btnClose;
        Controls.Add(btnReserve);
        Controls.Add(btnClose);

        _cmbDepartment.SelectedIndexChanged += (s, e) => FillPrograms();
        _cmbUserType.SelectedIndexChanged += (s, e) => ApplyUserTypeRules();
        _dtpDate.ValueChanged += (s, e) => RefreshAvailability();
        _dtpStart.ValueChanged += (s, e) => RefreshAvailability();
        _dtpEnd.ValueChanged += (s, e) => RefreshAvailability();

        FillPrograms();
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

    private void ApplyUserTypeRules()
    {
        bool isStudent = _cmbUserType.SelectedIndex == 0;

        if (isStudent) _dtpDate.Value = DateTime.Today;
        _dtpDate.Enabled = !isStudent;
        _numYear.Enabled = isStudent;
    }

    private void RefreshAvailability()
    {
        DateTime date = _dtpDate.Value.Date;
        TimeSpan start = _dtpStart.Value.TimeOfDay;
        TimeSpan end = _dtpEnd.Value.TimeOfDay;

        _lstBooked.Items.Clear();
        List<Reservation> booked = _service.GetBookedSlots(_spot.SpotOrRoom, date);
        if (booked.Count == 0)
            _lstBooked.Items.Add("Nothing yet — the whole day is free.");
        foreach (Reservation r in booked)
            _lstBooked.Items.Add($"{Clock(r.StartTime)} – {Clock(r.EndTime)}");

        if (end <= start)
        {
            _lblAvailability.ForeColor = Theme.Warning;
            _lblAvailability.Text = "End time must be after the start time.";
        }
        else if (_service.IsAvailable(_spot.SpotOrRoom, date, start, end))
        {
            _lblAvailability.ForeColor = Theme.Success;
            _lblAvailability.Text = "Available at this time.";
        }
        else
        {
            TimeWindow? next = _availability.FindNextFreeSlot(
                _spot.SpotOrRoom, date, end - start, start);

            _lblAvailability.ForeColor = Theme.Danger;
            _lblAvailability.Text = next == null
                ? "Not available — no later free slot that day."
                : $"Not available. Next free: {Clock(next.Start)} – {Clock(next.End)}";
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
            SpotOrRoom = _spot.SpotOrRoom,
            IsDiscussionRoom = _spot.IsDiscussionRoom,
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
            DialogResult = DialogResult.OK;
            Close();
        }
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
