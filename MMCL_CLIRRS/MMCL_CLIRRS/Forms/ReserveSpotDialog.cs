namespace MMCL_CLIRRS;

// Reservation form used by the interactive seat map. The spot is already
// chosen from the map, so this dialog only asks for the user's details and
// reservation time before sending the request through the normal service.
public class ReserveSpotDialog : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationManagementService _service;
    private readonly ClirMapSpot _spot;

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

        Text = "Reserve This Spot";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 690);

        Controls.Add(new Label
        {
            Text = "Reserve This Spot",
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Maroon,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        });

        Controls.Add(new Label
        {
            Text = spot.SpotOrRoom,
            Location = new Point(20, 60),
            AutoSize = true,
            ForeColor = Maroon,
            Font = new Font("Segoe UI", 13, FontStyle.Bold)
        });

        Controls.Add(new Label
        {
            Text = $"{spot.TypeLabel}  •  {spot.Area}",
            Location = new Point(20, 88),
            AutoSize = true,
            ForeColor = Color.DimGray
        });

        AddSectionTitle("Your details", 118);
        AddRow("Full name:", _txtName, 146);
        AddRow("ID number:", _txtIdNumber, 184);

        _cmbUserType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbUserType.Items.AddRange(new object[] { "Student", "Faculty" });
        _cmbUserType.SelectedIndex = 0;
        AddRow("I am a:", _cmbUserType, 222);

        _cmbDepartment.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbDepartment.Items.AddRange(AcademicPrograms.Departments);
        AddRow("Department:", _cmbDepartment, 260);
        _cmbDepartment.SelectedIndex = 0;

        _cmbProgram.DropDownStyle = ComboBoxStyle.DropDownList;
        AddRow("Program:", _cmbProgram, 298);
        _cmbDepartment.SelectedIndexChanged += (s, e) => FillPrograms();

        _numYear.Minimum = 1;
        _numYear.Maximum = 6;
        AddRow("Year level:", _numYear, 336);

        AddSectionTitle("Reservation time", 378);

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.MinDate = DateTime.Today;
        _dtpDate.Value = defaultDate.Date < DateTime.Today ? DateTime.Today : defaultDate.Date;
        AddRow("Date:", _dtpDate, 406);

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
        AddRow("Start time:", _dtpStart, 444);
        AddRow("End time:", _dtpEnd, 482);

        _lblAvailability.Location = new Point(20, 526);
        _lblAvailability.Size = new Size(420, 24);
        _lblAvailability.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        Controls.Add(_lblAvailability);

        Controls.Add(new Label
        {
            Text = "Already booked for this spot on this date:",
            Location = new Point(20, 554),
            AutoSize = true,
            ForeColor = Color.DimGray
        });

        _lstBooked.Location = new Point(20, 578);
        _lstBooked.Size = new Size(420, 48);
        Controls.Add(_lstBooked);

        Button btnReserve = MakeButton("Reserve", Maroon, Color.White, 200, 638);
        Button btnClose = MakeButton("Close", Color.White, Maroon, 320, 638);
        btnReserve.Click += btnReserve_Click;
        btnClose.Click += (s, e) => Close();
        AcceptButton = btnReserve;
        CancelButton = btnClose;
        Controls.Add(btnReserve);
        Controls.Add(btnClose);

        FillPrograms();
        ApplyUserTypeRules();
        _cmbUserType.SelectedIndexChanged += (s, e) => ApplyUserTypeRules();
        _dtpDate.ValueChanged += (s, e) => RefreshAvailability();
        _dtpStart.ValueChanged += (s, e) => RefreshAvailability();
        _dtpEnd.ValueChanged += (s, e) => RefreshAvailability();
        RefreshAvailability();
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
            _lstBooked.Items.Add("(nothing yet - the whole day is free)");
        foreach (Reservation r in booked)
            _lstBooked.Items.Add($"{Clock(r.StartTime)} - {Clock(r.EndTime)}");

        if (end <= start)
        {
            _lblAvailability.ForeColor = Color.DarkOrange;
            _lblAvailability.Text = "End time must be after the start time.";
        }
        else if (_service.IsAvailable(_spot.SpotOrRoom, date, start, end))
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
        control.Width = 290;
        Controls.Add(control);
    }

    private static void SetupTimePicker(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = value;
    }

    private static Button MakeButton(string text, Color back, Color fore, int x, int y)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
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
