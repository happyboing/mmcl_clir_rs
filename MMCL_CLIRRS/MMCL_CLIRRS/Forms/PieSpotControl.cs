using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

// The eight pie-cut seats are one round shape, so they are drawn by one
// control that works out which slice was clicked.
public sealed class PieSpotControl : Control
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

    private const float Radius = 36f;
    private const float PieAreaHeight = 76f;
    private const float SliceAngle = 45f;

    private readonly IReadOnlyList<ClirMapSpot> _slices;
    private readonly Dictionary<string, SpotSnapshot> _snapshots = new();

    private ClirMapSpot? _selectedSpot;
    private int _hoverIndex = -1;

    public event Action<ClirMapSpot>? SpotClicked;
    public event Action<ClirMapSpot>? CancelClicked;

    public PieSpotControl(IReadOnlyList<ClirMapSpot> slices)
    {
        _slices = slices;

        Location = slices[0].Bounds.Location;
        Size = slices[0].Bounds.Size;
        Cursor = Cursors.Hand;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
    }

    private PointF Center => new(Width / 2f, PieAreaHeight / 2f);

    private Rectangle CancelBounds
        => new Rectangle((Width - 60) / 2, (int)PieAreaHeight + 2, 60, 16);

    private bool ShowCancel
        => _selectedSpot != null &&
           _snapshots.TryGetValue(_selectedSpot.SpotOrRoom, out SpotSnapshot? snapshot) &&
           snapshot.Availability == SpotAvailability.Reserved &&
           snapshot.Reservation != null;

    public void UpdateStates(IReadOnlyDictionary<string, SpotSnapshot> snapshots,
                             ClirMapSpot? selectedSpot)
    {
        _selectedSpot = selectedSpot;

        _snapshots.Clear();
        foreach (ClirMapSpot slice in _slices)
        {
            if (snapshots.TryGetValue(slice.SpotOrRoom, out SpotSnapshot? snapshot))
                _snapshots[slice.SpotOrRoom] = snapshot;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        PointF center = Center;

        for (int i = 0; i < _slices.Count; i++)
        {
            ClirMapSpot slice = _slices[i];
            SpotSnapshot snapshot = _snapshots.TryGetValue(slice.SpotOrRoom, out SpotSnapshot? found)
                ? found
                : new SpotSnapshot(SpotAvailability.Available, null);

            (Color fill, Color line, Color text) = snapshot.Availability switch
            {
                SpotAvailability.Reserved => (ReservedFill, ReservedLine, ReservedText),
                SpotAvailability.Occupied => (OccupiedFill, OccupiedLine, OccupiedText),
                _ => (AvailableFill, AvailableLine, AvailableText)
            };

            float startAngle = -90f + i * SliceAngle;
            using GraphicsPath path = CreateSlice(center, startAngle);

            bool highlighted = _selectedSpot?.SpotOrRoom == slice.SpotOrRoom || _hoverIndex == i;
            using var fillBrush = new SolidBrush(fill);
            using var linePen = new Pen(highlighted ? SelectedLine : line,
                highlighted ? 2.6f : 1.5f);

            g.FillPath(fillBrush, path);
            g.DrawPath(linePen, path);

            // Seat number in the middle of the slice.
            double middleAngle = (startAngle + SliceAngle / 2f) * Math.PI / 180.0;
            float labelX = center.X + (float)(21 * Math.Cos(middleAngle));
            float labelY = center.Y + (float)(21 * Math.Sin(middleAngle));

            using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
            using var textBrush = new SolidBrush(text);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(slice.SeatNumber.ToString(), font, textBrush,
                new RectangleF(labelX - 9, labelY - 8, 18, 16), format);
        }

        // A small centre dot makes the pie shape clearer.
        using var dotBrush = new SolidBrush(Color.FromArgb(96, 125, 139));
        g.FillEllipse(dotBrush, center.X - 3, center.Y - 3, 6, 6);

        if (ShowCancel)
        {
            Rectangle bounds = CancelBounds;
            using GraphicsPath buttonPath = RoundedRect(bounds, 5);
            using var fillBrush = new SolidBrush(_hoverIndex == -2
                ? Color.FromArgb(142, 0, 0)
                : Color.FromArgb(183, 28, 28));

            g.FillPath(fillBrush, buttonPath);

            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("Cancel", font, Brushes.White, bounds, format);
        }
    }

    private GraphicsPath CreateSlice(PointF center, float startAngle)
    {
        var path = new GraphicsPath();
        path.AddLine(center, center);
        path.AddArc(center.X - Radius, center.Y - Radius,
            Radius * 2, Radius * 2, startAngle, SliceAngle);
        path.CloseFigure();
        return path;
    }

    private int HitSlice(Point point)
    {
        PointF center = Center;
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;

        if (dx * dx + dy * dy > Radius * Radius)
            return -1;

        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        double fromTop = (angle + 90.0 + 360.0) % 360.0;
        int index = (int)(fromTop / SliceAngle);

        return Math.Min(index, _slices.Count - 1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int oldHover = _hoverIndex;

        if (ShowCancel && CancelBounds.Contains(e.Location))
            _hoverIndex = -2;
        else
            _hoverIndex = HitSlice(e.Location);

        if (_hoverIndex != oldHover)
            Invalidate();

        Cursor = _hoverIndex != -1 ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverIndex = -1;
        Cursor = Cursors.Hand;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (ShowCancel && CancelBounds.Contains(e.Location))
        {
            if (_selectedSpot != null)
                CancelClicked?.Invoke(_selectedSpot);
        }
        else
        {
            int index = HitSlice(e.Location);
            if (index >= 0)
                SpotClicked?.Invoke(_slices[index]);
        }

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
