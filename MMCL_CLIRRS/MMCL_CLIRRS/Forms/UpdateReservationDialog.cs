namespace MMCL_CLIRRS;

public class UpdateReservationDialog : Form
{
    private readonly ComboBox _cmbSpot = new ComboBox();
    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpStart = new DateTimePicker();
    private readonly DateTimePicker _dtpEnd = new DateTimePicker();

    public string SpotOrRoom => _cmbSpot.SelectedItem?.ToString() ?? "";
    public DateTime Date => _dtpDate.Value.Date;
    public TimeSpan StartTime => _dtpStart.Value.TimeOfDay;
    public TimeSpan EndTime => _dtpEnd.Value.TimeOfDay;

    public UpdateReservationDialog(Reservation reservation)
    {
        Text = $"Update Reservation #{reservation.ReservationId} - {reservation.UserName}";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 250);

        // Only seats and rooms that exist in the CLIR layout can be chosen.
        _cmbSpot.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSpot.Items.AddRange(ClirLayout.GetSeats().ToArray());
        _cmbSpot.Items.AddRange(ClirLayout.GetDiscussionRooms().ToArray());
        _cmbSpot.SelectedItem = reservation.SpotOrRoom;

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.Value = reservation.Date;
        ConfigureTimePicker(_dtpStart, reservation.StartTime);
        ConfigureTimePicker(_dtpEnd, reservation.EndTime);

        AddRow("Spot / Room:", _cmbSpot, 20);
        AddRow("Date:", _dtpDate, 60);
        AddRow("Start time:", _dtpStart, 100);
        AddRow("End time:", _dtpEnd, 140);

        Button btnSave = CreateButton("Save", Color.FromArgb(183, 28, 28), Color.White, 140);
        Button btnCancel = CreateButton("Cancel", Color.White, Color.FromArgb(183, 28, 28), 250);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += (s, e) => { if (IsInputValid()) DialogResult = DialogResult.OK; };

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private static void ConfigureTimePicker(DateTimePicker picker, TimeSpan time)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "HH:mm";
        picker.ShowUpDown = true;
        picker.Value = DateTime.Today.Add(time);
    }

    private void AddRow(string label, Control control, int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
        control.Location = new Point(140, y);
        control.Width = 210;
        Controls.Add(control);
    }

    private static Button CreateButton(string text, Color back, Color fore, int x)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 195),
            Size = new Size(100, 36),
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(183, 28, 28);
        return button;
    }

    private bool IsInputValid()
    {
        if (SpotOrRoom.Length == 0)
        {
            MessageBox.Show("Please choose a seat or room.");
            return false;
        }
        if (EndTime <= StartTime)
        {
            MessageBox.Show("End time must be after the start time.");
            return false;
        }
        return true;
    }
}