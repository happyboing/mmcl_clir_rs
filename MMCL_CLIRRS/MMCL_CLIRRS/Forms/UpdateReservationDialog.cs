namespace MMCL_CLIRRS;

public class UpdateReservationDialog : Form
{
    private readonly TextBox _txtSpot = new TextBox();
    private readonly CheckBox _chkRoom = new CheckBox();
    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpStart = new DateTimePicker();
    private readonly DateTimePicker _dtpEnd = new DateTimePicker();

    public string SpotOrRoom => _txtSpot.Text.Trim();
    public bool IsDiscussionRoom => _chkRoom.Checked;
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
        ClientSize = new Size(380, 290);

        _txtSpot.Text = reservation.SpotOrRoom;
        _chkRoom.Text = "Discussion room";
        _chkRoom.Checked = reservation.IsDiscussionRoom;

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.Value = reservation.Date;
        ConfigureTimePicker(_dtpStart, reservation.StartTime);
        ConfigureTimePicker(_dtpEnd, reservation.EndTime);

        AddRow("Spot / Room:", _txtSpot, 20);
        AddRow("", _chkRoom, 60);
        AddRow("Date:", _dtpDate, 100);
        AddRow("Start time:", _dtpStart, 140);
        AddRow("End time:", _dtpEnd, 180);

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
        if (label.Length > 0)
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
            Location = new Point(x, 235),
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
            MessageBox.Show("Please enter a spot or room.");
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