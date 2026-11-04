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
        Text = $"Update Reservation #{reservation.ReservationId}";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 508);

        Controls.Add(Theme.CreateHeader(
            $"Update reservation #{reservation.ReservationId}",
            $"{reservation.UserName}  •  {reservation.UserIdNumber}",
            this, null, false));

        var card = new CardPanel
        {
            Location = new Point(24, 106),
            Size = new Size(472, 308),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };
        Controls.Add(card);

        card.Controls.Add(new Label
        {
            Text = "Reservation details",
            Location = new Point(20, 15),
            AutoSize = true,
            Font = Theme.Font(11.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        _cmbSpot.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSpot.Items.AddRange(ClirLayout.GetSeats().ToArray());
        _cmbSpot.Items.AddRange(ClirLayout.GetDiscussionRooms().ToArray());
        _cmbSpot.SelectedItem = reservation.SpotOrRoom;

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.Value = reservation.Date;
        ConfigureTimePicker(_dtpStart, reservation.StartTime);
        ConfigureTimePicker(_dtpEnd, reservation.EndTime);

        Theme.AddField(card, "Spot / Room", _cmbSpot, 20, 56, 432);
        Theme.AddField(card, "Date", _dtpDate, 20, 116, 432);
        Theme.AddField(card, "Start time", _dtpStart, 20, 176, 208);
        Theme.AddField(card, "End time", _dtpEnd, 244, 176, 208);

        var btnSave = Theme.MakeButton("Save changes", AppButtonKind.Primary, 132, 40);
        btnSave.Location = new Point(364, 442);
        btnSave.Click += (s, e) => { if (IsInputValid()) DialogResult = DialogResult.OK; };

        var btnCancel = Theme.MakeButton("Cancel", AppButtonKind.Secondary, 118, 40);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnCancel.Location = new Point(234, 442);

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
