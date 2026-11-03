using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

// ============================================================
//  Functionality #3: CLIR Interactive Seat Map
//  The entrance-terminal screen. It draws the CLIR floor plan,
//  colours every seat/room by its status, shows the details of
//  the selected place, and lets the user cancel a reserved spot
//  without leaving the map.
// ============================================================
public class SeatMapForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);
    private static readonly Color PageBackground = Color.FromArgb(246, 247, 249);
    private static readonly Color Ink = Color.FromArgb(38, 50, 56);

    private readonly ReservationManagementService _management;
    private readonly SeatMapService _service;

    private readonly Panel _mapPanel = new BufferedPanel();
    private readonly Dictionary<string, SeatSpotControl> _seatControls = new();
    private readonly Dictionary<string, SpotSnapshot> _snapshots = new();
    private PieSpotControl? _pieControl;

    private readonly DateTimePicker _dtpDate = new DateTimePicker();
    private readonly DateTimePicker _dtpTime = new DateTimePicker();
    private readonly Label _lblClock = new Label();

    private readonly Label _lblSpotName = new Label();
    private readonly Label _lblStatus = new Label();
    private readonly Label _lblType = new Label();
    private readonly Label _lblTable = new Label();
    private readonly Label _lblArea = new Label();
    private readonly Label _lblSeat = new Label();
    private readonly Label _lblCurrent = new Label();
    private readonly Label _lblReservedBy = new Label();
    private readonly Label _lblSchedule = new Label();
    private readonly ListBox _lstSchedule = new ListBox();
    private readonly Button _btnReserveSpot = new Button();
    private readonly Button _btnCancelSpot = new Button();

    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly ToolTip _toolTip = new();

    private ClirMapSpot? _selectedSpot;
    private bool _followingNow = true;
    private bool _updatingPickers;

    public SeatMapForm(ReservationManagementService management)
    {
        _management = management;
        _service = new SeatMapService(management);

        Text = "CLIR Interactive Seat Map";
        Font = new Font("Segoe UI", 10);
        BackColor = PageBackground;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1300, 680);

        BuildHeader();
        BuildToolbar();
        BuildMap();
        BuildDetailsPanel();

        SetPickersToNow();
        RefreshMap();

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

        FormClosed += (s, e) =>
        {
            _refreshTimer.Dispose();
            _toolTip.Dispose();
        };
    }

    // =========================================================
    //  Building the screen
    // =========================================================

    private void BuildHeader()
    {
        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(ClientSize.Width, 60),
            BackColor = Maroon,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        header.Controls.Add(new Label
        {
            Text = "CLIR Interactive Seat Map",
            Location = new Point(18, 5),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });

        header.Controls.Add(new Label
        {
            Text = "Tap a chair or discussion room to see its details. A reserved spot can be cancelled right here.",
            Location = new Point(20, 34),
            AutoSize = true,
            ForeColor = Color.FromArgb(255, 235, 235),
            Font = new Font("Segoe UI", 9)
        });

        var btnHome = new Button
        {
            Text = "← Back to Home",
            Location = new Point(ClientSize.Width - 156, 14),
            Size = new Size(140, 32),
            BackColor = Color.White,
            ForeColor = Maroon,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnHome.FlatAppearance.BorderSize = 0;
        btnHome.Click += (s, e) => Close();
        header.Controls.Add(btnHome);

        Controls.Add(header);
    }

    private void BuildToolbar()
    {
        var toolbar = new Panel
        {
            Location = new Point(0, 60),
            Size = new Size(ClientSize.Width, 40),
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        toolbar.Controls.Add(new Label
        {
            Text = "Viewing",
            Location = new Point(18, 13),
            AutoSize = true,
            ForeColor = Ink
        });

        _dtpDate.Format = DateTimePickerFormat.Short;
        _dtpDate.Value = DateTime.Today;
        _dtpDate.Location = new Point(76, 8);
        _dtpDate.Width = 130;
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
            Location = new Point(216, 13),
            AutoSize = true,
            ForeColor = Ink
        });

        _dtpTime.Format = DateTimePickerFormat.Custom;
        _dtpTime.CustomFormat = "HH:mm";
        _dtpTime.ShowUpDown = true;
        _dtpTime.Location = new Point(238, 8);
        _dtpTime.Width = 82;
        _dtpTime.ValueChanged += (s, e) =>
        {
            if (_updatingPickers) return;
            _followingNow = false;
            RefreshMap();
        };
        toolbar.Controls.Add(_dtpTime);

        Button btnNow = MakeToolbarButton("Now", 332, 76);
        btnNow.Click += (s, e) =>
        {
            _followingNow = true;
            SetPickersToNow();
            RefreshMap();
        };
        toolbar.Controls.Add(btnNow);

        Button btnRefresh = MakeToolbarButton("Refresh", 416, 86);
        btnRefresh.Click += (s, e) =>
        {
            if (_followingNow) SetPickersToNow();
            RefreshMap();
        };
        toolbar.Controls.Add(btnRefresh);

        _lblClock.Location = new Point(520, 13);
        _lblClock.AutoSize = true;
        _lblClock.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _lblClock.ForeColor = Color.DimGray;
        toolbar.Controls.Add(_lblClock);

        Controls.Add(toolbar);

        Controls.Add(new Panel
        {
            Location = new Point(0, 100),
            Size = new Size(ClientSize.Width, 1),
            BackColor = Color.FromArgb(224, 224, 224),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
    }

    private void BuildMap()
    {
        _mapPanel.Location = new Point(16, 108);
        _mapPanel.Size = new Size(ClirMapLayout.MapWidth, ClirMapLayout.MapHeight);
        _mapPanel.BackColor = Color.FromArgb(248, 249, 250);
        _mapPanel.BorderStyle = BorderStyle.FixedSingle;
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
        var panel = new Panel
        {
            Location = new Point(932, 108),
            Size = new Size(352, ClirMapLayout.MapHeight),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        panel.Controls.Add(new Label
        {
            Text = "Selected spot",
            Location = new Point(12, 10),
            AutoSize = true,
            ForeColor = Maroon,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        });

        _lblSpotName.Location = new Point(12, 36);
        _lblSpotName.Size = new Size(326, 26);
        _lblSpotName.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        _lblSpotName.ForeColor = Ink;
        panel.Controls.Add(_lblSpotName);

        _lblStatus.Location = new Point(12, 68);
        _lblStatus.Size = new Size(120, 24);
        _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
        _lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _lblStatus.ForeColor = Color.White;
        _lblStatus.BackColor = Color.Gray;
        panel.Controls.Add(_lblStatus);

        AddField(panel, _lblType, 104);
        AddField(panel, _lblTable, 128);
        AddField(panel, _lblArea, 152);
        AddField(panel, _lblSeat, 176);

        panel.Controls.Add(new Panel
        {
            Location = new Point(12, 204),
            Size = new Size(326, 1),
            BackColor = Color.FromArgb(224, 224, 224)
        });

        _lblCurrent.Location = new Point(12, 214);
        _lblCurrent.Size = new Size(326, 20);
        _lblCurrent.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _lblCurrent.ForeColor = Ink;
        panel.Controls.Add(_lblCurrent);

        _lblReservedBy.Location = new Point(12, 238);
        _lblReservedBy.Size = new Size(326, 20);
        _lblReservedBy.ForeColor = Color.DimGray;
        panel.Controls.Add(_lblReservedBy);

        _lblSchedule.Location = new Point(12, 262);
        _lblSchedule.Size = new Size(326, 38);
        _lblSchedule.ForeColor = Color.DimGray;
        panel.Controls.Add(_lblSchedule);

        _btnReserveSpot.Text = "Reserve This Spot";
        _btnReserveSpot.Location = new Point(12, 306);
        _btnReserveSpot.Size = new Size(326, 36);
        _btnReserveSpot.BackColor = Color.SeaGreen;
        _btnReserveSpot.ForeColor = Color.White;
        _btnReserveSpot.FlatStyle = FlatStyle.Flat;
        _btnReserveSpot.Cursor = Cursors.Hand;
        _btnReserveSpot.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _btnReserveSpot.FlatAppearance.BorderColor = Color.SeaGreen;
        _btnReserveSpot.Visible = false;
        _btnReserveSpot.Click += BtnReserveSpot_Click;
        panel.Controls.Add(_btnReserveSpot);

        _btnCancelSpot.Text = "Cancel My Reservation";
        _btnCancelSpot.Location = new Point(12, 306);
        _btnCancelSpot.Size = new Size(326, 36);
        _btnCancelSpot.BackColor = Maroon;
        _btnCancelSpot.ForeColor = Color.White;
        _btnCancelSpot.FlatStyle = FlatStyle.Flat;
        _btnCancelSpot.Cursor = Cursors.Hand;
        _btnCancelSpot.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _btnCancelSpot.FlatAppearance.BorderColor = Maroon;
        _btnCancelSpot.Visible = false;
        _btnCancelSpot.Click += BtnCancelSpot_Click;
        panel.Controls.Add(_btnCancelSpot);

        panel.Controls.Add(new Label
        {
            Text = "Reservations on this date",
            Location = new Point(12, 352),
            AutoSize = true,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        });

        _lstSchedule.Location = new Point(12, 374);
        _lstSchedule.Size = new Size(326, 90);
        _lstSchedule.Font = new Font("Segoe UI", 9);
        _lstSchedule.BorderStyle = BorderStyle.FixedSingle;
        _lstSchedule.HorizontalScrollbar = true;
        _lstSchedule.IntegralHeight = false;
        panel.Controls.Add(_lstSchedule);

        AddLegend(panel, 12, 474, Color.FromArgb(46, 125, 50), "Available");
        AddLegend(panel, 112, 474, Color.FromArgb(239, 108, 0), "Reserved");
        AddLegend(panel, 212, 474, Color.FromArgb(183, 28, 28), "Occupied");

        panel.Controls.Add(new Label
        {
            Text = "Select a spot on the map. Available spots can be reserved here, " +
                   "and reserved spots can be cancelled from this panel.",
            Location = new Point(12, 500),
            Size = new Size(326, 46),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5f)
        });

        Controls.Add(panel);
    }

    private static void AddField(Panel panel, Label label, int y)
    {
        label.Location = new Point(12, y);
        label.AutoSize = true;
        label.ForeColor = Color.FromArgb(55, 71, 79);
        label.Font = new Font("Segoe UI", 9.5f);
        panel.Controls.Add(label);
    }

    private static void AddLegend(Panel panel, int x, int y, Color color, string text)
    {
        panel.Controls.Add(new Panel
        {
            Location = new Point(x, y + 2),
            Size = new Size(14, 14),
            BackColor = color
        });

        panel.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x + 20, y),
            AutoSize = true,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5f)
        });
    }

    private static Button MakeToolbarButton(string text, int x, int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 6),
            Size = new Size(width, 30),
            BackColor = Color.White,
            ForeColor = Maroon,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = Maroon;
        return button;
    }

    // =========================================================
    //  Map painting (the room itself, not the clickable spots)
    // =========================================================

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

        // Leave the bottom edge open where the exit and entrance sit.
        using var mapBackgroundBrush = new SolidBrush(_mapPanel.BackColor);
        int openWidth = ClirMapLayout.MapWidth - 84;
        g.FillRectangle(floorBrush, new Rectangle(42, 548, openWidth, 5));
        g.FillRectangle(mapBackgroundBrush, new Rectangle(42, 552, openWidth, 6));

        using var tinyFont = new Font("Segoe UI", 6.4f, FontStyle.Bold);
        using var boldFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var furnitureBrush = new SolidBrush(Color.FromArgb(224, 224, 224));
        using var furniturePen = new Pen(Color.FromArgb(90, 90, 90), 2f);
        using var tableBrush = new SolidBrush(Color.FromArgb(236, 239, 241));
        using var tablePen = new Pen(Color.FromArgb(96, 125, 139), 2f);
        using var centerFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        // Straight table with monitors in the upper-left corner.
        DrawMonitorTable(g);

        // Five small tables, each with one seat underneath.
        for (int i = 0; i < ClirLayout.BeanBagTableCount; i++)
        {
            var cell = new Rectangle(260 + i * 62, 145, 58, 58);
            using GraphicsPath cellPath = RoundedRect(cell, 5);
            g.FillPath(furnitureBrush, cellPath);
            g.DrawPath(furniturePen, cellPath);

            g.DrawString("Table", tinyFont, Brushes.DimGray,
                new RectangleF(cell.X, cell.Bottom - 16, cell.Width, 14), centerFormat);
        }

        // Round tables with their names.
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

        // The discussion room is the only room in the wing.
        using (GraphicsPath wingPath = RoundedRect(new Rectangle(770, 180, 112, 220), 8))
        {
            using var roomFill = new SolidBrush(Color.FromArgb(245, 245, 245));
            using var roomPen = new Pen(Color.FromArgb(90, 90, 90), 2.5f);
            g.FillPath(roomFill, wingPath);
            g.DrawPath(roomPen, wingPath);
        }

        // Smaller door-style entrance and exit, flush with the bottom edge.
        DrawDoor(g, new Rectangle(24, 528, 76, 24), "Exit");
        DrawDoor(g, new Rectangle(790, 528, 88, 24), "Entrance");
    }

    private static void DrawMonitorTable(Graphics g)
    {
        using var fill = new SolidBrush(Color.FromArgb(224, 224, 224));
        using var line = new Pen(Color.FromArgb(80, 80, 80), 2f);
        using var monitorBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        using var labelFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(120, 120, 120));

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

        // A diagonal leaf and a handle make the block read as a door.
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

    // =========================================================
    //  Status refresh and selection
    // =========================================================

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
            _lblStatus.BackColor = Color.Gray;
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
                _lblStatus.BackColor = Color.FromArgb(239, 108, 0);
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
                _lblStatus.BackColor = Color.FromArgb(183, 28, 28);
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
                _lblStatus.BackColor = Color.FromArgb(46, 125, 50);
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

        // Show the map at the time that was actually reserved.
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

    // =========================================================
    //  Cancel a reserved spot from the terminal
    // =========================================================

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

    // =========================================================
    //  Date / time helpers
    // =========================================================

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
