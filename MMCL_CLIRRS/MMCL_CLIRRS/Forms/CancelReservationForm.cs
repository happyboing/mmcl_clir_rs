namespace MMCL_CLIRRS;

public class CancelReservationForm : Form
{
    private readonly ReservationManagementService _service;

    private readonly TextBox _txtIdNumber = new TextBox();
    private readonly Label _lblInfo = new Label();
    private readonly ListView _list = new ListView();

    public CancelReservationForm(ReservationManagementService service)
    {
        _service = service;

        Text = "Cancel My Reservation";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 600);

        Controls.Add(Theme.CreateHeader(
            "Cancel my reservation",
            "Find your active booking, review the details, then cancel it.",
            this, null, false));

        var searchCard = new CardPanel
        {
            Location = new Point(24, 106),
            Size = new Size(652, 112),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };
        Controls.Add(searchCard);

        searchCard.Controls.Add(new Label
        {
            Text = "Find your reservation",
            Location = new Point(20, 15),
            AutoSize = true,
            Font = Theme.Font(11.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        Theme.AddField(searchCard, "ID number", _txtIdNumber, 20, 44, 500, 40);

        var btnFind = Theme.MakeButton("Find", AppButtonKind.Primary, 96, 40);
        btnFind.Location = new Point(536, 62);
        btnFind.Click += btnFind_Click;
        searchCard.Controls.Add(btnFind);

        var resultsCard = new CardPanel
        {
            Location = new Point(24, 234),
            Size = new Size(652, 268),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border
        };
        Controls.Add(resultsCard);

        resultsCard.Controls.Add(new Label
        {
            Text = "Your active reservations",
            Location = new Point(20, 15),
            AutoSize = true,
            Font = Theme.Font(11.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        _lblInfo.Location = new Point(20, 42);
        _lblInfo.Size = new Size(612, 20);
        _lblInfo.Font = Theme.Font(8.75f);
        _lblInfo.ForeColor = Theme.Muted;
        _lblInfo.Text = "Enter your ID number to load your active bookings.";
        resultsCard.Controls.Add(_lblInfo);

        _list.Location = new Point(20, 68);
        _list.Size = new Size(612, 180);
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.GridLines = false;
        _list.BorderStyle = BorderStyle.None;
        _list.BackColor = Theme.Surface;
        _list.ForeColor = Theme.Ink;
        _list.Font = Theme.Font(9.25f);
        _list.Columns.Add("ID", 70);
        _list.Columns.Add("Spot / Room", 240);
        _list.Columns.Add("Date", 180);
        _list.Columns.Add("Time", 120);
        resultsCard.Controls.Add(_list);

        var btnCancel = Theme.MakeButton("Cancel Reservation", AppButtonKind.Danger, 190, 40);
        btnCancel.Location = new Point(356, 522);
        btnCancel.Click += btnCancel_Click;

        var btnClose = Theme.MakeButton("Close", AppButtonKind.Secondary, 120, 40);
        btnClose.Location = new Point(556, 522);
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
            item.SubItems.Add($"{Clock(r.StartTime)} – {Clock(r.EndTime)}");
            item.Tag = r;
            _list.Items.Add(item);
        }

        if (mine.Count == 0)
        {
            _lblInfo.ForeColor = Theme.Muted;
            _lblInfo.Text = "No active reservations were found for that ID number.";
        }
        else
        {
            _lblInfo.ForeColor = Theme.Success;
            _lblInfo.Text = $"{mine.Count} active reservation(s). Select one, then press Cancel Reservation.";
        }
    }

    private void btnCancel_Click(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].Tag is not Reservation r)
        {
            MessageBox.Show("Please select a reservation from the list first.");
            return;
        }

        DialogResult answer = MessageBox.Show(
            $"Cancel {r.SpotOrRoom} on {r.Date:dd MMM yyyy}, {Clock(r.StartTime)} – {Clock(r.EndTime)}?",
            "Please confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        OperationResult result = _service.CancelReservation(r.ReservationId, _txtIdNumber.Text);

        MessageBox.Show(result.Message,
            result.Success ? "Cancelled" : "Could not cancel",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        LoadReservations();
    }

    private static string Clock(TimeSpan t) => t.ToString(@"hh\:mm");
}
