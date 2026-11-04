using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

public sealed class SeatSpotControl : Control
{
    private static readonly Color AvailableFill = Color.FromArgb(238, 247, 242);
    private static readonly Color AvailableLine = Theme.Success;
    private static readonly Color AvailableText = Color.FromArgb(30, 92, 69);

    private static readonly Color ReservedFill = Color.FromArgb(255, 246, 232);
    private static readonly Color ReservedLine = Theme.Warning;
    private static readonly Color ReservedText = Color.FromArgb(156, 88, 17);

    private static readonly Color OccupiedFill = Color.FromArgb(252, 238, 240);
    private static readonly Color OccupiedLine = Theme.Danger;
    private static readonly Color OccupiedText = Theme.Danger;

    private static readonly Color SelectedLine = Theme.Info;

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
    {
        get
        {
            int width = Math.Min(40, Width - 10);
            return new Rectangle((Width - width) / 2, 0, width, 24);
        }
    }

    private Rectangle BeanBagBounds
    {
        get
        {
            int width = Math.Min(46, Width - 10);
            int height = Math.Min(38, Height - 20);
            return new Rectangle((Width - width) / 2, 0, width, height);
        }
    }

    private Rectangle FurnitureBodyBounds
        => new Rectangle(0, 0, Width - 1, Height - 18);

    private Rectangle BodyBounds => Spot.Kind switch
    {
        ClirSpotKind.Seat or ClirSpotKind.MonitorSeat => ChairBounds,
        ClirSpotKind.BeanBag => BeanBagBounds,
        ClirSpotKind.Couch or ClirSpotKind.Table => FurnitureBodyBounds,
        ClirSpotKind.FloorArea or ClirSpotKind.DiscussionRoom => FurnitureBodyBounds,
        _ => ClientRectangle
    };

    private Rectangle CancelBounds
    {
        get
        {
            int width = Spot.Kind switch
            {
                ClirSpotKind.Seat or ClirSpotKind.MonitorSeat => Math.Min(36, Width - 6),
                ClirSpotKind.BeanBag => Math.Min(46, Width - 6),
                _ => Width - 8
            };

            return new Rectangle((Width - width) / 2, Height - 16, width, 14);
        }
    }

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

        switch (Spot.Kind)
        {
            case ClirSpotKind.Seat:
            case ClirSpotKind.MonitorSeat:
                DrawSeat(e.Graphics, fill, line, text);
                break;
            case ClirSpotKind.BeanBag:
                DrawBeanBag(e.Graphics, fill, line, text);
                break;
            case ClirSpotKind.Couch:
                DrawCouch(e.Graphics, fill, line, text);
                break;
            case ClirSpotKind.Table:
                DrawTable(e.Graphics, fill, line, text);
                break;
            case ClirSpotKind.FloorArea:
                DrawFloorArea(e.Graphics, fill, line, text);
                break;
            case ClirSpotKind.DiscussionRoom:
                DrawRoom(e.Graphics, fill, line, text);
                break;
        }

        if (ShowCancel)
            DrawCancelButton(e.Graphics);

        if (IsSelected)
        {
            using var selectedPen = new Pen(SelectedLine, 2.5f);
            using GraphicsPath selectedPath = RoundedRect(
                new Rectangle(1, 1, Width - 3, Height - 3), 6);
            e.Graphics.DrawPath(selectedPen, selectedPath);
        }
    }

    private void DrawSeat(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = ChairBounds;
        using GraphicsPath path = RoundedRect(body, 6);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);

        using var backPen = new Pen(Color.FromArgb(120, line), 1.2f);
        g.DrawLine(backPen, body.Left + 6, body.Top + 5, body.Right - 6, body.Top + 5);

        using var numberFont = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(Spot.SeatNumber == 0 ? "•" : Spot.SeatNumber.ToString(),
            numberFont, textBrush,
            new RectangleF(body.Left, body.Top + 4, body.Width, body.Height - 4), format);
    }

    private void DrawBeanBag(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = BeanBagBounds;
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);

        g.FillEllipse(fillBrush, body);
        g.DrawEllipse(linePen, body);

        using var font = new Font("Segoe UI", 6.5f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("Bean Bag", font, textBrush,
            new RectangleF(body.X, body.Y, body.Width, body.Height), format);
    }

    private void DrawCouch(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = FurnitureBodyBounds;
        using GraphicsPath path = RoundedRect(body, 7);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);
        using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);

        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        if (Height > Width * 2)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(Width / 2f, body.Height / 2f);
            g.RotateTransform(-90);
            g.DrawString("Couch", font, textBrush,
                new RectangleF(-body.Height / 2f, -12f, body.Height, 24f), format);
            g.Restore(state);
        }
        else
        {
            g.DrawString("Couch", font, textBrush,
                new RectangleF(body.X, body.Y, body.Width, body.Height), format);
        }
    }

    private void DrawTable(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = FurnitureBodyBounds;
        using GraphicsPath path = RoundedRect(body, 6);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);
        using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);
        g.DrawString("Table", font, textBrush,
            new RectangleF(body.X, body.Y, body.Width, body.Height), format);
    }

    private void DrawFloorArea(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = FurnitureBodyBounds;
        using GraphicsPath path = RoundedRect(body, 2);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 2.5f : 2f);
        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);
        g.DrawString(Spot.SpotOrRoom, font, textBrush,
            new RectangleF(body.X, body.Y, body.Width, body.Height), format);
    }

    private void DrawRoom(Graphics g, Color fill, Color line, Color text)
    {
        Rectangle body = FurnitureBodyBounds;
        using GraphicsPath path = RoundedRect(body, 8);
        using var fillBrush = new SolidBrush(fill);
        using var linePen = new Pen(_hoverSpot ? SelectedLine : line, _hoverSpot ? 3f : 2f);

        g.FillPath(fillBrush, path);
        g.DrawPath(linePen, path);

        string[] words = Spot.SpotOrRoom.Split(' ');
        string number = words[^1];

        using var titleFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var numberFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var textBrush = new SolidBrush(text);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        float centerY = body.Top + body.Height / 2f;
        g.DrawString("Discussion", titleFont, textBrush,
            new RectangleF(body.Left, centerY - 28, body.Width, 14), format);
        g.DrawString($"Room {number}", numberFont, textBrush,
            new RectangleF(body.Left, centerY - 8, body.Width, 22), format);
    }

    private void DrawCancelButton(Graphics g)
    {
        Rectangle bounds = CancelBounds;
        using GraphicsPath path = RoundedRect(bounds, 5);
        using var fillBrush = new SolidBrush(_hoverCancel
            ? Theme.PrimaryDark
            : Theme.Danger);

        g.FillPath(fillBrush, path);

        float fontSize = bounds.Width < 30 ? 5.2f
            : bounds.Width < 42 ? 6f
            : 7.5f;

        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("Cancel", font, Brushes.White, bounds, format);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        bool overCancel = ShowCancel && CancelBounds.Contains(e.Location);
        bool overSpot = !overCancel && BodyBounds.Contains(e.Location);

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
        else if (BodyBounds.Contains(e.Location))
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
