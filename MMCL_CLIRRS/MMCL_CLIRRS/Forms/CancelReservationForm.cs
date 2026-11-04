namespace MMCL_CLIRRS;

public class CancelReservationForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);

    private readonly ReservationManagementService _service;

    private readonly TextBox _txtIdNumber = new TextBox();
    private readonly Label _lblInfo = new Label();
    private readonly ListView _list = new ListView();

    public CancelReservationForm(ReservationManagementService service)
    {
        _service = service;

        Text = "Cancel My Reservation";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 430);

        Controls.Add(new Label
        {
            Text = "Cancel My Reservation",
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Maroon,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        });

        Controls.Add(new Label { Text = "Enter your ID number:", Location = new Point(20, 68), AutoSize = true });
        _txtIdNumber.Location = new Point(20, 94);
        _txtIdNumber.Width = 280;
        Controls.Add(_txtIdNumber);

        Button btnFind = MakeButton("Find", Maroon, Color.White, 315, 90, 90);
        btnFind.Click += btnFind_Click;
        Controls.Add(btnFind);

        _lblInfo.Location = new Point(20, 135);
        _lblInfo.Size = new Size(480, 22);
        _lblInfo.ForeColor = Color.DimGray;
        _lblInfo.Text = "Your active reservations will appear below.";
        Controls.Add(_lblInfo);

        _list.Location = new Point(20, 162);
        _list.Size = new Size(480, 180);
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.GridLines = true;
        _list.Columns.Add("ID", 60);
        _list.Columns.Add("Spot / Room", 170);
        _list.Columns.Add("Date", 120);
        _list.Columns.Add("Time", 110);
        Controls.Add(_list);

        Button btnCancel = MakeButton("Cancel Reservation", Maroon, Color.White, 215, 365, 175);
        Button btnClose = MakeButton("Close", Color.White, Maroon, 400, 365, 100);
        btnCancel.Click += btnCancel_Click;
        btnClose.Click += (s, e) => Close();
        Controls.Add(btnCancel);
        Controls.Add(btnClose);

        AcceptButton = btnFind;
        CancelButton = btnClose;
    }

    private void btnFind_Click(object? sender, EventArgs e)
    {
        if (_txtIdNumber.Text.Trim().Length == 0)
        {
            MessageBox.Show("Please enter your ID number first.");
            return;
        }
        LoadReservations();
    }

    private void LoadReservations()
    {
        _list.Items.Clear();

        List<Reservation> mine = _service.GetActiveReservationsOf(_txtIdNumber.Text);
        foreach (Reservation r in mine)
        {
            var item = new ListViewItem(r.ReservationId.ToString());
            item.SubItems.Add(r.SpotOrRoom);
            item.SubItems.Add(r.Date.ToString("ddd, dd MMM yyyy"));
            item.SubItems.Add($"{Clock(r.StartTime)} - {Clock(r.EndTime)}");
            item.Tag = r;
            _list.Items.Add(item);
        }

        _lblInfo.Text = mine.Count == 0
            ? "No active reservations were found for that ID number."
            : $"{mine.Count} active reservation(s). Select one, then press Cancel Reservation.";
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].Tag is not Reservation r)
        {
            MessageBox.Show("Please select a reservation from the list first.");
            return;
        }

        DialogResult answer = MessageBox.Show(
            $"Cancel {r.SpotOrRoom} on {r.Date:dd MMM yyyy}, {Clock(r.StartTime)} - {Clock(r.EndTime)}?",
            "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        OperationResult result = _service.CancelReservation(r.ReservationId, _txtIdNumber.Text);

        MessageBox.Show(result.Message,
            result.Success ? "Cancelled" : "Could not cancel",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        LoadReservations();
    }

    private static Button MakeButton(string text, Color back, Color fore, int x, int y, int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 36),
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
