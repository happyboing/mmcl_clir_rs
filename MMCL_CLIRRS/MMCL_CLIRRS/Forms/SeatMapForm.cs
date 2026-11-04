using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

public class SeatMapForm : Form
{
    private readonly ReservationManagementService _management;
    private readonly ReservationMonitorService? _monitor;
    private readonly SeatMapService _service;
    private readonly Label _lblAlerts = new Label();

    private readonly Panel _mapPanel = new BufferedPanel();
    private readonly Dictionary<string, SeatSpotControl> _seatControls = new();
    private readonly Dictionary<string, SpotSnapshot> _snapshots = new();
    private PieSpotControl? _pieControl;

    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpTime = new DateTimePicker();
    private readonly Label _lblClock = new Label();

    private readonly Label _lblSpotName = new Label();
    private readonly PillLabel _lblStatus = new PillLabel();
    private readonly Label _lblType = new Label();
    private readonly Label _lblTable = new Label();
    private readonly Label _lblArea = new Label();
    private readonly Label _lblSeat = new Label();
    private readonly Label _lblCurrent = new Label();
    private readonly Label _lblReservedBy = new Label();
    private readonly Label _lblSchedule = new Label();
    private readonly ListBox _lstSchedule = new ListBox();
    private readonly AppButton _btnReserveSpot = new AppButton();
    private readonly AppButton _btnCancelSpot = new AppButton();

    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly ToolTip _toolTip = new();

    private ClirMapSpot? _selectedSpot;
    private bool _followingNow = true;
    private bool _updatingPickers;

    public SeatMapForm(ReservationManagementService management,
                       ReservationMonitorService? monitor = null)
    {
        _management = management;
        _monitor = monitor;
        _service = new SeatMapService(management);

        Text = "CLIR Interactive Seat Map";
        Font = new Font("Segoe UI", 10);
        BackColor = Theme.Background;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1320, 690);

        BuildHeader();
        BuildToolbar();
        BuildMap();
        BuildDetailsPanel();

        RestoreSessionOrNow();
        RefreshMap();
        RestoreSelectedSpot();

        _refreshTimer.Interval = 30_000;
        _refreshTimer.Tick += (s, e) =>
        {
            if (_followingNow)
            {
                SetPickersToNow();
                RefreshMap();
            }
            else
            {
                UpdateClockLabel();
            }
        };
        _refreshTimer.Start();

        if (_monitor != null)
        {
            _monitor.ScheduleChanged += OnScheduleChanged;
            _monitor.AlertsChanged += UpdateAlertsLabel;
            UpdateAlertsLabel();
        }

        FormClosed += (s, e) =>
        {
            if (_monitor != null)
            {
                _monitor.ScheduleChanged -= OnScheduleChanged;
                _monitor.AlertsChanged -= UpdateAlertsLabel;
            }

            SaveSessionState();
            _refreshTimer.Dispose();
            _toolTip.Dispose();
        };
    }

    private void BuildHeader()
    {
        var header = Theme.CreateHeader(
            "CLIR interactive seat map",
            "Tap a chair or discussion room to see its details. Reserved spots can be cancelled here.",
            this, "Back to home", height: 68);

        _lblAlerts.AutoSize = false;
        _lblAlerts.Location = new Point(header.Width - 540, 22);
        _lblAlerts.Size = new Size(390, 24);
        _lblAlerts.TextAlign = ContentAlignment.MiddleRight;
        _lblAlerts.ForeColor = Color.FromArgb(232, 210, 214);
        _lblAlerts.Font = Theme.Font(8.75f, FontStyle.Bold);
        _lblAlerts.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _lblAlerts.BackColor = Color.Transparent;
        header.Controls.Add(_lblAlerts);

        Controls.Add(header);
    }

    private void OnScheduleChanged()
    {
        if (IsDisposed) return;
        RefreshMap();
    }

    private void UpdateAlertsLabel()
    {
        if (IsDisposed || _monitor == null) return;

        int count = _monitor.PendingAlertCount;
        _lblAlerts.Text = count == 0
            ? ""
            : $"⚠ {count} unoccupied seat alert(s) – staff notified";
        _lblAlerts.ForeColor = count == 0
            ? Color.FromArgb(232, 210, 214)
            : Theme.Gold;
    }

    private void BuildToolbar()
    {
        var toolbar = new Panel
        {
            Location = new Point(0, 68),
            Size = new Size(ClientSize.Width, 44),
            BackColor = Theme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        toolbar.Controls.Add(new Label
        {
            Text = "Viewing",
            Location = new Point(24, 14),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.75f, FontStyle.Bold),
            BackColor = Color.Transparent
        });

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.Value = DateTime.Today;
        _dtpDate.Location = new Point(82, 7);
        _dtpDate.Width = 130;
        _dtpDate.Font = Theme.Font(9.25f);
        _dtpDate.ValueChanged += (s, e) =>
        {
            if (_updatingPickers) return;
            _followingNow = false;
            RefreshMap();
        };
        toolbar.Controls.Add(_dtpDate);

        toolbar.Controls.Add(new Label
        {
            Text = "at",
            Location = new Point(222, 14),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.75f, FontStyle.Bold),
            BackColor = Color.Transparent
        });

        _dtpTime.Format = DateTimePickerFormat.Custom;
        _dtpTime.CustomFormat = "HH:mm";
        _dtpTime.ShowUpDown = true;
        _dtpTime.Location = new Point(246, 7);
        _dtpTime.Width = 82;
        _dtpTime.Font = Theme.Font(9.25f);
        _dtpTime.ValueChanged += (s, e) =>
        {
            if (_updatingPickers) return;
            _followingNow = false;
            RefreshMap();
        };
        toolbar.Controls.Add(_dtpTime);

        var btnNow = Theme.MakeButton("Now", AppButtonKind.Primary, 72, 34);
        btnNow.Location = new Point(344, 5);
        btnNow.Click += (s, e) =>
        {
            _followingNow = true;
            SetPickersToNow();
            RefreshMap();
        };
        toolbar.Controls.Add(btnNow);

        var btnRefresh = Theme.MakeButton("Refresh", AppButtonKind.Secondary, 92, 34);
        btnRefresh.Location = new Point(426, 5);
        btnRefresh.Click += (s, e) =>
        {
            if (_followingNow) SetPickersToNow();
            RefreshMap();
        };
        toolbar.Controls.Add(btnRefresh);

        _lblClock.Location = new Point(540, 14);
        _lblClock.AutoSize = true;
        _lblClock.Font = Theme.Font(9f, FontStyle.Bold);
        _lblClock.ForeColor = Theme.Muted;
        _lblClock.BackColor = Color.Transparent;
        toolbar.Controls.Add(_lblClock);

        Controls.Add(toolbar);

        Controls.Add(new Panel
        {
            Location = new Point(0, 112),
            Size = new Size(ClientSize.Width, 1),
            BackColor = Theme.Border,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
    }

    private void BuildMap()
    {
        _mapPanel.Location = new Point(24, 118);
        _mapPanel.Size = new Size(ClirMapLayout.MapWidth, ClirMapLayout.MapHeight);
        _mapPanel.BackColor = Color.FromArgb(252, 250, 248);
        _mapPanel.BorderStyle = BorderStyle.None;
        _mapPanel.Paint += MapPanel_Paint;
        Controls.Add(_mapPanel);

        foreach (ClirMapSpot spot in ClirMapLayout.Spots)
        {
            if (spot.Kind == ClirSpotKind.PieSeat) continue;

            var control = new SeatSpotControl(spot);
            control.SpotClicked += SeatControl_SpotClicked;
            control.CancelClicked += SeatControl_CancelClicked;
            _seatControls[spot.SpotOrRoom] = control;
            _mapPanel.Controls.Add(control);

            _toolTip.SetToolTip(control, $"{spot.SpotOrRoom}\n{spot.Area}");
        }

        if (ClirMapLayout.PieSeats.Count > 0)
        {
            _pieControl = new PieSpotControl(ClirMapLayout.PieSeats);
            _pieControl.SpotClicked += spot => SelectSpot(spot);
            _pieControl.CancelClicked += PieControl_CancelClicked;
            _mapPanel.Controls.Add(_pieControl);
            _toolTip.SetToolTip(_pieControl, "Pie seats 1-8\nEight seats around the circular table");
        }
    }

    private void BuildDetailsPanel()
    {
        var panel = new CardPanel
        {
            Location = new Point(940, 118),
            Size = new Size(356, ClirMapLayout.MapHeight),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(18)
        };

        panel.Controls.Add(new Label
        {
            Text = "SELECTED SPOT",
            Location = new Point(18, 16),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Font(8f, FontStyle.Bold),
            BackColor = Color.Transparent
        });

        _lblSpotName.Location = new Point(18, 38);
        _lblSpotName.Size = new Size(320, 32);
        _lblSpotName.Font = Theme.Font(14f, FontStyle.Bold);
        _lblSpotName.ForeColor = Theme.Ink;
        _lblSpotName.BackColor = Color.Transparent;
        panel.Controls.Add(_lblSpotName);

        _lblStatus.Location = new Point(18, 76);
        _lblStatus.Size = new Size(118, 26);
        _lblStatus.Text = "—";
        _lblStatus.FillColor = Theme.Muted;
        panel.Controls.Add(_lblStatus);

        AddField(panel, _lblType, 116);
        AddField(panel, _lblTable, 142);
        AddField(panel, _lblArea, 168);
        AddField(panel, _lblSeat, 194);

        panel.Controls.Add(new Panel
        {
            Location = new Point(18, 226),
            Size = new Size(320, 1),
            BackColor = Theme.Border
        });

        _lblCurrent.Location = new Point(18, 238);
        _lblCurrent.Size = new Size(320, 22);
        _lblCurrent.Font = Theme.Font(10.5f, FontStyle.Bold);
        _lblCurrent.ForeColor = Theme.Ink;
        _lblCurrent.BackColor = Color.Transparent;
        panel.Controls.Add(_lblCurrent);

        _lblReservedBy.Location = new Point(18, 264);
        _lblReservedBy.Size = new Size(320, 20);
        _lblReservedBy.ForeColor = Theme.Muted;
        _lblReservedBy.BackColor = Color.Transparent;
        panel.Controls.Add(_lblReservedBy);

        _lblSchedule.Location = new Point(18, 288);
        _lblSchedule.Size = new Size(320, 38);
        _lblSchedule.ForeColor = Theme.Muted;
        _lblSchedule.BackColor = Color.Transparent;
        panel.Controls.Add(_lblSchedule);

        _btnReserveSpot.Text = "Reserve this spot";
        _btnReserveSpot.Kind = AppButtonKind.Success;
        _btnReserveSpot.Location = new Point(18, 332);
        _btnReserveSpot.Size = new Size(320, 42);
        _btnReserveSpot.Visible = false;
        _btnReserveSpot.Click += BtnReserveSpot_Click;
        panel.Controls.Add(_btnReserveSpot);

        _btnCancelSpot.Text = "Cancel my reservation";
        _btnCancelSpot.Kind = AppButtonKind.Danger;
        _btnCancelSpot.Location = new Point(18, 332);
        _btnCancelSpot.Size = new Size(320, 42);
        _btnCancelSpot.Visible = false;
        _btnCancelSpot.Click += BtnCancelSpot_Click;
        panel.Controls.Add(_btnCancelSpot);

        panel.Controls.Add(new Label
        {
            Text = "Reservations on this date",
            Location = new Point(18, 390),
            AutoSize = true,
            ForeColor = Theme.Ink,
            Font = Theme.Font(9f, FontStyle.Bold),
            BackColor = Color.Transparent
        });

        var scheduleHost = new CardPanel
        {
            Location = new Point(18, 412),
            Size = new Size(320, 92),
            BackColor = Theme.SurfaceAlt,
            BorderColor = Theme.Border,
            Radius = 9,
            Padding = new Padding(9, 7, 9, 7)
        };
        _lstSchedule.Dock = DockStyle.Fill;
        _lstSchedule.Font = Theme.Font(8.75f);
        _lstSchedule.BorderStyle = BorderStyle.None;
        _lstSchedule.BackColor = Theme.SurfaceAlt;
        _lstSchedule.ForeColor = Theme.Ink;
        _lstSchedule.HorizontalScrollbar = true;
        _lstSchedule.IntegralHeight = false;
        scheduleHost.Controls.Add(_lstSchedule);
        panel.Controls.Add(scheduleHost);

        AddLegend(panel, 18, 520, Theme.Success, "Available");
        AddLegend(panel, 122, 520, Theme.Warning, "Reserved");
        AddLegend(panel, 226, 520, Theme.Danger, "Occupied");

        panel.Controls.Add(new Label
        {
            Text = "Select a spot to reserve it, or cancel a reserved spot.",
            Location = new Point(18, 536),
            Size = new Size(320, 16),
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.25f),
            BackColor = Color.Transparent
        });

        Controls.Add(panel);
    }

    private static void AddField(Panel panel, Label label, int y)
    {
        label.Location = new Point(18, y);
        label.AutoSize = true;
        label.ForeColor = Theme.Ink;
        label.Font = Theme.Font(9.25f);
        label.BackColor = Color.Transparent;
        panel.Controls.Add(label);
    }

    private static void AddLegend(Panel panel, int x, int y, Color color, string text)
    {
        panel.Controls.Add(new Panel
        {
            Location = new Point(x, y + 3),
            Size = new Size(12, 12),
            BackColor = color
        });

        panel.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x + 18, y),
            AutoSize = true,
            ForeColor = Theme.Muted,
            Font = Theme.Font(8.25f),
            BackColor = Color.Transparent
        });
    }

    private void MapPanel_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using GraphicsPath floorPath = RoundedRect(
            new Rectangle(4, 4, ClirMapLayout.MapWidth - 10, ClirMapLayout.MapHeight - 10), 38);
        using var floorBrush = new SolidBrush(Color.White);
        using var wallPen = new Pen(Color.FromArgb(84, 84, 84), 3.5f);
        g.FillPath(floorBrush, floorPath);
        g.DrawPath(wallPen, floorPath);

        using var mapBackgroundBrush = new SolidBrush(_mapPanel.BackColor);
        int openWidth = ClirMapLayout.MapWidth - 84;
        g.FillRectangle(floorBrush, new Rectangle(42, 548, openWidth, 5));
        g.FillRectangle(mapBackgroundBrush, new Rectangle(42, 552, openWidth, 6));

        using var tinyFont = new Font("Segoe UI", 6.4f, FontStyle.Bold);
        using var boldFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var furnitureBrush = new SolidBrush(Color.FromArgb(236, 231, 226));
        using var furniturePen = new Pen(Color.FromArgb(165, 154, 148), 2f);
        using var tableBrush = new SolidBrush(Color.FromArgb(247, 242, 237));
        using var tablePen = new Pen(Color.FromArgb(172, 155, 149), 2f);
        using var centerFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        DrawMonitorTable(g);

        for (int i = 0; i < ClirLayout.BeanBagTableCount; i++)
        {
            var cell = new Rectangle(260 + i * 62, 145, 58, 58);
            using GraphicsPath cellPath = RoundedRect(cell, 5);
            g.FillPath(furnitureBrush, cellPath);
            g.DrawPath(furniturePen, cellPath);

            g.DrawString("Table", tinyFont, Brushes.DimGray,
                new RectangleF(cell.X, cell.Bottom - 16, cell.Width, 14), centerFormat);
        }

        int[] radii = { 28, 28, 38, 36, 34 };
        for (int i = 0; i < ClirMapLayout.TableCenters.Count; i++)
        {
            Point center = ClirMapLayout.TableCenters[i];
            var table = new Rectangle(center.X - radii[i], center.Y - radii[i],
                                      radii[i] * 2, radii[i] * 2);
            g.FillEllipse(tableBrush, table);
            g.DrawEllipse(tablePen, table);

            g.DrawString($"Table {i + 1}", boldFont, Brushes.DimGray,
                new RectangleF(table.X, table.Y, table.Width, table.Height), centerFormat);
        }

        using (GraphicsPath wingPath = RoundedRect(new Rectangle(770, 180, 112, 220), 8))
        {
            using var roomFill = new SolidBrush(Color.FromArgb(250, 246, 242));
            using var roomPen = new Pen(Color.FromArgb(172, 155, 149), 2.5f);
            g.FillPath(roomFill, wingPath);
            g.DrawPath(roomPen, wingPath);
        }

        DrawDoor(g, new Rectangle(24, 528, 76, 24), "Exit");
        DrawDoor(g, new Rectangle(790, 528, 88, 24), "Entrance");
    }

    private static void DrawMonitorTable(Graphics g)
    {
        using var fill = new SolidBrush(Color.FromArgb(236, 231, 226));
        using var line = new Pen(Color.FromArgb(165, 154, 148), 2f);
        using var monitorBrush = new SolidBrush(Color.FromArgb(75, 68, 71));
        using var labelFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Theme.Muted);

        var table = new Rectangle(20, 70, 180, 40);
        using GraphicsPath tablePath = RoundedRect(table, 6);
        g.FillPath(fill, tablePath);
        g.DrawPath(line, tablePath);

        for (int i = 0; i < 5; i++)
            g.FillRectangle(monitorBrush, 30 + i * 32, 84, 20, 12);

        g.DrawString("Table with Monitors", labelFont, labelBrush, new PointF(30, 48));
    }

    private static void DrawDoor(Graphics g, Rectangle bounds, string text)
    {
        using GraphicsPath path = RoundedRect(bounds, 2);
        using var fill = new SolidBrush(Color.FromArgb(238, 238, 238));
        using var line = new Pen(Color.FromArgb(70, 70, 70), 1.7f);
        using var detail = new Pen(Color.FromArgb(120, 120, 120), 1.1f);
        using var knob = new SolidBrush(Color.FromArgb(90, 90, 90));

        g.FillPath(fill, path);
        g.DrawPath(line, path);

        g.DrawLine(detail, bounds.Left + 7, bounds.Top + 4,
                           bounds.Left + 21, bounds.Bottom - 4);
        g.FillEllipse(knob, bounds.Right - 12, bounds.Top + bounds.Height / 2 - 2, 4, 4);

        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(text, font, Brushes.DimGray,
            new RectangleF(bounds.X + 22, bounds.Y, bounds.Width - 24, bounds.Height), format);
    }

    private void RefreshMap()
    {
        DateTime date = _dtpDate.Value.Date;
        TimeSpan time = _dtpTime.Value.TimeOfDay;

        foreach (ClirMapSpot spot in ClirMapLayout.Spots)
        {
            SpotSnapshot snapshot = _service.GetSnapshot(spot, date, time);
            _snapshots[spot.SpotOrRoom] = snapshot;

            if (_seatControls.TryGetValue(spot.SpotOrRoom, out SeatSpotControl? control))
            {
                control.UpdateState(snapshot);
                control.IsSelected = _selectedSpot?.SpotOrRoom == spot.SpotOrRoom;
            }
        }

        _pieControl?.UpdateStates(_snapshots, _selectedSpot);
        UpdateDetails();
        UpdateClockLabel();
        SaveSessionState();
    }

    private void SeatControl_SpotClicked(object? sender, EventArgs e)
    {
        if (sender is not SeatSpotControl control) return;
        SelectSpot(control.Spot);
    }

    private void SeatControl_CancelClicked(object? sender, EventArgs e)
    {
        if (sender is not SeatSpotControl control) return;

        if (_snapshots.TryGetValue(control.Spot.SpotOrRoom, out SpotSnapshot? snapshot) &&
            snapshot.Reservation != null)
        {
            SelectSpot(control.Spot);
            BeginCancel(control.Spot, snapshot.Reservation);
        }
    }

    private void SelectSpot(ClirMapSpot spot)
    {
        _selectedSpot = spot;

        foreach (SeatSpotControl control in _seatControls.Values)
            control.IsSelected = control.Spot.SpotOrRoom == spot.SpotOrRoom;

        _pieControl?.UpdateStates(_snapshots, spot);
        UpdateDetails();

        SeatMapSession.SelectedSpot = spot.SpotOrRoom;
        SaveSessionState();
    }

    private void PieControl_CancelClicked(ClirMapSpot spot)
    {
        if (_snapshots.TryGetValue(spot.SpotOrRoom, out SpotSnapshot? snapshot) &&
            snapshot.Reservation != null)
        {
            SelectSpot(spot);
            BeginCancel(spot, snapshot.Reservation);
        }
    }

    private void UpdateDetails()
    {
        if (_selectedSpot == null)
        {
            _lblSpotName.Text = "Tap a spot on the map";
            _lblStatus.Text = "—";
            _lblStatus.FillColor = Theme.Muted;
            _lblType.Text = "Type: —";
            _lblTable.Text = "Table: —";
            _lblArea.Text = "Area: —";
            _lblSeat.Text = "Seat number: —";
            _lblCurrent.Text = "No spot selected yet.";
            _lblReservedBy.Text = "";
            _lblSchedule.Text = "";
            _lblSchedule.ForeColor = Color.DimGray;
            _btnReserveSpot.Visible = false;
            _btnCancelSpot.Visible = false;
            _lstSchedule.Items.Clear();
            _lstSchedule.Items.Add("(select a spot to see its schedule)");
            return;
        }

        ClirMapSpot spot = _selectedSpot;
        SpotSnapshot snapshot = _snapshots.TryGetValue(spot.SpotOrRoom, out SpotSnapshot? found)
            ? found
            : new SpotSnapshot(SpotAvailability.Available, null);

        _lblSpotName.Text = spot.SpotOrRoom;
        _lblType.Text = $"Type: {spot.TypeLabel}";
        _lblTable.Text = spot.Table == "—" ? "Table: —" : $"Table: {spot.Table}";
        _lblArea.Text = $"Area: {spot.Area}";
        _lblSeat.Text = spot.SeatNumber > 0
            ? $"Seat number: {spot.SeatNumber}"
            : "Seat number: —";
        _lblSchedule.ForeColor = Color.DimGray;

        switch (snapshot.Availability)
        {
            case SpotAvailability.Reserved:
                _lblStatus.Text = "RESERVED";
                _lblStatus.FillColor = Theme.Warning;
                _lblCurrent.Text = "Reserved at this time";
                _lblReservedBy.Text = snapshot.Reservation == null
                    ? ""
                    : $"Reserved by {snapshot.Reservation.UserName} ({snapshot.Reservation.UserIdNumber})";
                _lblSchedule.Text = snapshot.Reservation == null
                    ? ""
                    : $"Reservation #{snapshot.Reservation.ReservationId}  •  " +
                      $"{Clock(snapshot.Reservation.StartTime)} - {Clock(snapshot.Reservation.EndTime)}";
                _btnReserveSpot.Visible = false;
                _btnCancelSpot.Visible = true;
                break;

            case SpotAvailability.Occupied:
                _lblStatus.Text = "OCCUPIED";
                _lblStatus.FillColor = Theme.Danger;
                _lblCurrent.Text = "In use at this time";
                _lblReservedBy.Text = snapshot.Reservation == null
                    ? ""
                    : $"Occupied by {snapshot.Reservation.UserName} ({snapshot.Reservation.UserIdNumber})";
                _lblSchedule.Text = snapshot.Reservation == null
                    ? ""
                    : $"Reservation #{snapshot.Reservation.ReservationId}  •  " +
                      $"{Clock(snapshot.Reservation.StartTime)} - {Clock(snapshot.Reservation.EndTime)}";
                _btnReserveSpot.Visible = false;
                _btnCancelSpot.Visible = false;
                break;

            default:
                _lblStatus.Text = "AVAILABLE";
                _lblStatus.FillColor = Theme.Success;
                _lblCurrent.Text = "Available at this time";
                _lblReservedBy.Text = "No reservation is holding this spot.";
                _lblSchedule.Text = "";
                _btnReserveSpot.Visible = true;
                _btnCancelSpot.Visible = false;
                break;
        }

        LoadDaySchedule(spot);
    }

    private void LoadDaySchedule(ClirMapSpot spot)
    {
        _lstSchedule.Items.Clear();

        List<Reservation> schedule = _service.GetDaySchedule(spot.SpotOrRoom, _dtpDate.Value.Date);
        if (schedule.Count == 0)
        {
            _lstSchedule.Items.Add("(no reservations on this date)");
            return;
        }

        foreach (Reservation reservation in schedule)
        {
            string status = reservation.Status == ReservationStatus.Occupied
                ? "Occupied"
                : "Reserved";
            _lstSchedule.Items.Add(
                $"{Clock(reservation.StartTime)}-{Clock(reservation.EndTime)}  {status}  " +
                $"{reservation.UserName}");
        }
    }

    private void BtnReserveSpot_Click(object? sender, EventArgs e)
    {
        if (_selectedSpot == null) return;

        using var dialog = new ReserveSpotDialog(
            _management,
            _selectedSpot,
            _dtpDate.Value.Date,
            _dtpTime.Value.TimeOfDay);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _updatingPickers = true;
        _dtpDate.Value = dialog.ReservationDate;
        _dtpTime.Value = DateTime.Today.Add(dialog.ReservationStart);
        _updatingPickers = false;
        _followingNow = false;
        RefreshMap();
    }

    private void BtnCancelSpot_Click(object? sender, EventArgs e)
    {
        if (_selectedSpot == null) return;

        if (_snapshots.TryGetValue(_selectedSpot.SpotOrRoom, out SpotSnapshot? snapshot) &&
            snapshot.Reservation != null)
        {
            BeginCancel(_selectedSpot, snapshot.Reservation);
        }
    }

    private void BeginCancel(ClirMapSpot spot, Reservation reservation)
    {
        if (reservation.Status != ReservationStatus.Active)
        {
            MessageBox.Show(
                "Only active reservations can be cancelled.",
                "Could not cancel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            RefreshMap();
            return;
        }

        string? enteredId = PromptDialog.Show(
            this,
            "Cancel Reservation",
            $"Enter the ID number used to reserve {spot.SpotOrRoom} to cancel it:",
            okText: "Confirm");

        if (enteredId == null) return;

        OperationResult result = _service.Cancel(reservation.ReservationId, enteredId);

        MessageBox.Show(
            result.Message,
            result.Success ? "Reservation cancelled" : "Could not cancel",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success)
        {
            _selectedSpot = spot;
            RefreshMap();
        }
    }

    private void RestoreSessionOrNow()
    {
        if (SeatMapSession.ViewDate is DateTime date &&
            SeatMapSession.ViewTime is TimeSpan time)
        {
            _updatingPickers = true;
            _dtpDate.Value = date.Date;
            _dtpTime.Value = DateTime.Today.Add(time);
            _updatingPickers = false;
            _followingNow = false;
            return;
        }

        SetPickersToNow();
    }

    private void RestoreSelectedSpot()
    {
        if (SeatMapSession.SelectedSpot is not string spotName) return;

        ClirMapSpot? spot = ClirMapLayout.Spots
            .FirstOrDefault(s => s.SpotOrRoom == spotName);

        if (spot == null)
        {
            SeatMapSession.SelectedSpot = null;
            return;
        }

        SelectSpot(spot);
    }

    private void SaveSessionState()
    {
        SeatMapSession.ViewDate = _dtpDate.Value.Date;
        SeatMapSession.ViewTime = _dtpTime.Value.TimeOfDay;
    }

    private void SetPickersToNow()
    {
        _updatingPickers = true;

        DateTime now = DateTime.Now;
        _dtpDate.Value = now.Date;
        _dtpTime.Value = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        _updatingPickers = false;
    }

    private void UpdateClockLabel()
    {
        string prefix = _followingNow ? "Live" : "Viewing";
        _lblClock.Text = $"{prefix}  •  {_dtpDate.Value:ddd, dd MMM yyyy}  •  {_dtpTime.Value:HH:mm}";
        _lblClock.ForeColor = _followingNow ? Color.FromArgb(46, 125, 50) : Color.DimGray;
    }

    private static string Clock(TimeSpan time) => time.ToString(@"hh\:mm");

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new GraphicsPath();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
