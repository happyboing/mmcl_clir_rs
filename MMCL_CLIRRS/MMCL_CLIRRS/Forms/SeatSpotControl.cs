using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

// One clickable seat or discussion room on the interactive map.
// A reserved spot draws its own "Cancel" button directly below it.
public sealed class SeatSpotControl : Control
{
    private static readonly Color AvailableFill = Color.FromArgb(232, 245, 233);
    private static readonly Color AvailableLine = Color.FromArgb(46, 125, 50);
    private static readonly Color AvailableText = Color.FromArgb(27, 94, 32);

    private static readonly Color ReservedFill = Color.FromArgb(255, 243, 224);
    private static readonly Color ReservedLine = Color.FromArgb(239, 108, 0);
    private static readonly Color ReservedText = Color.FromArgb(216, 67, 21);

    private static readonly Color OccupiedFill = Color.FromArgb(255, 235, 238);
    private static readonly Color OccupiedLine = Color.FromArgb(183, 28, 28);
    private static readonly Color OccupiedText = Color.FromArgb(183, 28, 28);

    private static readonly Color SelectedLine = Color.FromArgb(21, 101, 192);

    private bool _hoverSpot;
    private bool _hoverCancel;
    private bool _isSelected;

    public ClirMapSpot Spot { get; }
    public SpotAvailability Availability { get; private set; } = SpotAvailability.Available;
    public Reservation? Reservation { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            Invalidate();
        }
    }

    public event EventHandler? SpotClicked;
    public event EventHandler? CancelClicked;

    public SeatSpotControl(ClirMapSpot spot)
    {
        Spot = spot;

        Location = spot.Bounds.Location;
        Size = spot.Bounds.Size;
        Cursor = Cursors.Hand;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
    }

    private bool ShowCancel
        => Availability == SpotAvailability.Reserved && Reservation != null;

    private Rectangle ChairBounds
        => new Rectangle((Width - 28) / 2, 0, 28, 24);

    private Rectangle RoomBodyBounds
        => new Rectangle(0, 0, Width - 1, Height - 25);

    private Rectangle CancelBounds
        => Spot.Kind == ClirSpotKind.Seat
            ? new Rectangle((Width - 36) / 2, Height - 16, 36, 14)
            : new Rectangle(4, Height - 17, Width - 8, 15);

    public void UpdateState(SpotSnapshot snapshot)
    {
        Availability = snapshot.Availability;
        Reservation = snapshot.Reservation;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        (Color fill, Color line, Color text) = Availability switch
        {
            SpotAvailability.Reserved => (ReservedFill, ReservedLine, ReservedText),
            SpotAvailability.Occupied => (OccupiedFill, OccupiedLine, OccupiedText),
            _ => (AvailableFill, AvailableLine, AvailableText)
        };

        if (Spot.Kind == ClirSpotKind.Seat)
            DrawSeat(e.Graphics, fill, line, text);
        else
            DrawRoom(e.Graphics, fill, line, text);

        if (ShowCancel)
            DrawCancelButton(e.Graphics);

        if (IsSelected)
        {
            using var selectedPen = new Pen(SelectedLine, 2.5f);
            e.Graphics.DrawRectangle(selectedPen,
                new Rectangle(1, 1, Width - 3, Height - 3));
        }
    }

    private void DrawSeat(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = ChairBounds;
        using GraphicsPath path = RoundedRect(body, 7);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);

        // A small backrest makes the shape read as a chair.
        using var backPen = new Pen(Color.FromArgb(120, line), 1.2f);
        g.DrawLine(backPen, body.Left + 6, body.Top + 5, body.Right - 6, body.Top + 5);

        using var numberFont = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(Spot.SeatNumber.ToString(), numberFont, textBrush,
            new RectangleF(body.Left, body.Top + 4, body.Width, body.Height - 4), format);
    }

    private void DrawRoom(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = RoomBodyBounds;
        using GraphicsPath path = RoundedRect(body, 8);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 3f : 2f);

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);

        string[] words = Spot.SpotOrRoom.Split(' ');
        string number = words[^1];

        using var titleFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        using var numberFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        var format = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("DISCUSSION", titleFont, textBrush,
            new RectangleF(body.Left, body.Top + 4, body.Width, 12), format);
        g.DrawString($"Room {number}", numberFont, textBrush,
            new RectangleF(body.Left, body.Top + 16, body.Width, 18), format);
    }

    private void DrawCancelButton(Graphics g)
    {
        Rectangle bounds = CancelBounds;
        using GraphicsPath path = RoundedRect(bounds, 6);
        using var fillBrush = new SolidBrush(_hoverCancel
            ? Color.FromArgb(142, 0, 0)
            : Color.FromArgb(183, 28, 28));

        g.FillPath(fillBrush, path);

        using var font = new Font("Segoe UI",
            Spot.Kind == ClirSpotKind.Seat ? 7f : 8f, FontStyle.Bold);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("Cancel", font, Brushes.White, bounds, format);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        bool overCancel = ShowCancel && CancelBounds.Contains(e.Location);
        bool overSpot = !overCancel &&
                        (Spot.Kind == ClirSpotKind.Seat
                            ? ChairBounds.Contains(e.Location)
                            : RoomBodyBounds.Contains(e.Location));

        if (overCancel != _hoverCancel || overSpot != _hoverSpot)
        {
            _hoverCancel = overCancel;
            _hoverSpot = overSpot;
            Cursor = overCancel || overSpot ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverCancel = false;
        _hoverSpot = false;
        Cursor = Cursors.Hand;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (ShowCancel && CancelBounds.Contains(e.Location))
            CancelClicked?.Invoke(this, EventArgs.Empty);
        else if (Spot.Kind == ClirSpotKind.Seat
                    ? ChairBounds.Contains(e.Location)
                    : RoomBodyBounds.Contains(e.Location))
            SpotClicked?.Invoke(this, EventArgs.Empty);

        base.OnMouseClick(e);
    }

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
}
