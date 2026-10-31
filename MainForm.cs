namespace MMCL_CLIRRS;

public class MainForm : Form
{
    private const int TableCount = 6;
    private const int SeatsPerTable = 4;
    private const int RoomCount = 3;

    private static readonly Color AvailableColor = Color.FromArgb(120, 200, 120);
    private static readonly Color ReservedColor = Color.FromArgb(220, 100, 100);
    private static readonly Color SelectedColor = Color.FromArgb(90, 150, 230);

    private readonly ReservationService _service;
    private readonly Dictionary<string, Button> _spotButtons = new();
    private string? _selectedSpot;

    // user detail inputs
    private readonly TextBox txtName = new() { Width = 220 };
    private readonly TextBox txtId = new() { Width = 220 };
    private readonly ComboBox cmbType = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtDept = new() { Width = 220 };
    private readonly TextBox txtProgram = new() { Width = 220 };
    private readonly NumericUpDown numYear = new() { Width = 220, Minimum = 1, Maximum = 6, Value = 1 };
    private readonly DateTimePicker dtpDate = new() { Width = 220, Format = DateTimePickerFormat.Short };
    private readonly ComboBox cmbTime = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label lblSelected = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Text = "Selected: (none)" };
    private readonly Label lblStatus = new() { AutoSize = true };
    private readonly Button btnReserve = new() { Text = "Reserve", Width = 105, Height = 34, Enabled = false };
    private readonly Button btnCancel = new() { Text = "Cancel Reservation", Width = 110, Height = 34, Enabled = false };

    public MainForm()
    {
        _service = new ReservationService(new InMemoryReservationRepository());
        // TODO: replace with the group's real repository, e.g.
        // _service = new ReservationService(new LinkedListReservationRepository());

        Text = "MMCL CLIRRS - Seat and Room Reservation";
        ClientSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 560);

        BuildLayout();
        BuildMap();

        cmbType.Items.AddRange(new object[] { UserType.Student, UserType.Faculty });
        cmbType.SelectedIndex = 0;

        for (var t = new TimeSpan(8, 0, 0); t <= new TimeSpan(19, 30, 0); t += TimeSpan.FromMinutes(30))
            cmbTime.Items.Add(t);
        cmbTime.Format += (s, e) => { if (e.ListItem is TimeSpan ts) e.Value = DateTime.Today.Add(ts).ToString("hh:mm tt"); };
        cmbTime.SelectedIndex = 0;

        dtpDate.MinDate = DateTime.Today;
        dtpDate.Value = DateTime.Today;

        dtpDate.ValueChanged += (s, e) => RefreshMap();
        cmbTime.SelectedIndexChanged += (s, e) => RefreshMap();
        btnReserve.Click += (s, e) => OnReserve();
        btnCancel.Click += (s, e) => OnCancel();

        RefreshMap();
    }

    private DateTime SelectedDate => dtpDate.Value.Date;
    private TimeSpan SelectedTime => (TimeSpan)cmbTime.SelectedItem!;

    // ---------- UI construction ----------

    private readonly Panel mapPanel = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

    private void BuildLayout()
    {
        var right = new Panel { Dock = DockStyle.Right, Width = 340, Padding = new Padding(12), BackColor = Color.WhiteSmoke };

        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        void AddRow(string label, Control c)
        {
            grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 8) });
            c.Margin = new Padding(0, 4, 0, 4);
            grid.Controls.Add(c);
        }
        AddRow("Name", txtName);
        AddRow("ID Number", txtId);
        AddRow("User Type", cmbType);
        AddRow("Department", txtDept);
        AddRow("Program", txtProgram);
        AddRow("Year Level", numYear);
        AddRow("Date", dtpDate);
        AddRow("Time", cmbTime);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(0, 6, 0, 0) };
        buttons.Controls.Add(btnReserve);
        buttons.Controls.Add(btnCancel);

        var info = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(0, 8, 0, 0) };
        lblStatus.MaximumSize = new Size(310, 0);
        info.Controls.Add(lblStatus);
        lblSelected.Dock = DockStyle.Top;

        // docking order: last added is laid out first -> add bottom-to-top
        right.Controls.Add(info);
        right.Controls.Add(buttons);
        right.Controls.Add(lblSelected);
        right.Controls.Add(grid);

        var legend = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 6, 0, 0), BackColor = Color.White };
        legend.Controls.Add(LegendItem(AvailableColor, "Available"));
        legend.Controls.Add(LegendItem(ReservedColor, "Reserved"));
        legend.Controls.Add(LegendItem(SelectedColor, "Selected"));

        Controls.Add(mapPanel);
        Controls.Add(legend);
        Controls.Add(right);
    }

    private static Control LegendItem(Color color, string text)
    {
        var p = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 0, 14, 0) };
        p.Controls.Add(new Panel { Width = 16, Height = 16, BackColor = color });
        p.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new Padding(2, 1, 0, 0) });
        return p;
    }

    private void BuildMap()
    {
        // Study tables with seats
        const int cols = 3;
        for (int t = 1; t <= TableCount; t++)
        {
            var box = new GroupBox
            {
                Text = $"Table {t}",
                Size = new Size(170, 120),
                Left = 15 + ((t - 1) % cols) * 185,
                Top = 15 + ((t - 1) / cols) * 135
            };
            for (int s = 1; s <= SeatsPerTable; s++)
            {
                string name = $"Table {t} - Seat {s}";
                var btn = MakeSpotButton(name, $"Seat {s}", 70, 36);
                btn.Left = 10 + ((s - 1) % 2) * 78;
                btn.Top = 24 + ((s - 1) / 2) * 44;
                box.Controls.Add(btn);
            }
            mapPanel.Controls.Add(box);
        }

        // Discussion rooms (separate from regular seats)
        var rooms = new GroupBox { Text = "Discussion Rooms", Size = new Size(540, 80), Left = 15, Top = 15 + 2 * 135 };
        for (int r = 1; r <= RoomCount; r++)
        {
            string name = $"Discussion Room {r}";
            var btn = MakeSpotButton(name, $"Room {r}", 160, 40);
            btn.Left = 10 + (r - 1) * 172;
            btn.Top = 26;
            rooms.Controls.Add(btn);
        }
        mapPanel.Controls.Add(rooms);
    }

    private Button MakeSpotButton(string spotName, string caption, int w, int h)
    {
        var btn = new Button { Text = caption, Size = new Size(w, h), FlatStyle = FlatStyle.Flat, Tag = spotName };
        btn.Click += (s, e) => { _selectedSpot = spotName; RefreshMap(); };
        _spotButtons[spotName] = btn;
        return btn;
    }

    // ---------- Behaviour ----------

    private static bool IsRoom(string spot) => spot.StartsWith("Discussion Room");

    private void RefreshMap()
    {
        foreach (var (spot, btn) in _spotButtons)
        {
            bool free = _service.IsAvailable(spot, SelectedDate, SelectedTime);
            btn.BackColor = spot == _selectedSpot ? SelectedColor : (free ? AvailableColor : ReservedColor);
        }

        if (_selectedSpot == null)
        {
            lblSelected.Text = "Selected: (none)";
            btnReserve.Enabled = btnCancel.Enabled = false;
            return;
        }

        bool selectedFree = _service.IsAvailable(_selectedSpot, SelectedDate, SelectedTime);
        lblSelected.Text = $"Selected: {_selectedSpot} ({(selectedFree ? "Available" : "Reserved")})";
        btnReserve.Enabled = selectedFree;
        btnCancel.Enabled = !selectedFree;
    }

    private void OnReserve()
    {
        if (_selectedSpot == null) return;

        var result = _service.TryReserve(
            txtName.Text, txtId.Text, (UserType)cmbType.SelectedItem!,
            txtDept.Text, txtProgram.Text, (int)numYear.Value,
            _selectedSpot, IsRoom(_selectedSpot),
            SelectedDate, SelectedTime);

        lblStatus.ForeColor = result.Success ? Color.DarkGreen : Color.Firebrick;
        lblStatus.Text = result.Message;
        if (result.Success) ClearUserFields();
        RefreshMap();
    }

    private void OnCancel()
    {
        if (_selectedSpot == null) return;

        string? entered = PromptForText("Cancel Reservation", "Enter your student/faculty number:");
        if (entered == null) return;

        var result = _service.TryCancel(_selectedSpot, SelectedDate, SelectedTime, entered);
        lblStatus.ForeColor = result.Success ? Color.DarkGreen : Color.Firebrick;
        lblStatus.Text = result.Message;
        RefreshMap();
    }

    private void ClearUserFields()
    {
        txtName.Clear(); txtId.Clear(); txtDept.Clear(); txtProgram.Clear();
        numYear.Value = 1; cmbType.SelectedIndex = 0;
    }

    private string? PromptForText(string title, string label)
    {
        using var f = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(320, 110),
            MaximizeBox = false,
            MinimizeBox = false
        };
        var lbl = new Label { Text = label, Left = 12, Top = 12, AutoSize = true };
        var txt = new TextBox { Left = 12, Top = 36, Width = 296 };
        var ok = new Button { Text = "OK", Left = 152, Top = 70, DialogResult = DialogResult.OK };
        var back = new Button { Text = "Back", Left = 233, Top = 70, DialogResult = DialogResult.Cancel };
        f.Controls.AddRange(new Control[] { lbl, txt, ok, back });
        f.AcceptButton = ok;
        f.CancelButton = back;
        return f.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
    }
}