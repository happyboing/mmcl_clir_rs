namespace MMCL_CLIRRS;

// Small horizontal bar chart used by the statistics dashboard.
public sealed class StatBarChart : Control
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);
    private static readonly Color BarColor = Color.FromArgb(211, 47, 47);

    private string _title = "";
    private IReadOnlyList<CountItem> _data = Array.Empty<CountItem>();

    public StatBarChart()
    {
        BackColor = Color.White;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint, true);
    }

    public void SetData(string title, IReadOnlyList<CountItem> data)
    {
        _title = title;
        _data = data;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var border = new Pen(Color.FromArgb(224, 224, 224), 1f);
        g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

        using var titleFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8.5f);
        using var valueFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Maroon);
        using var labelBrush = new SolidBrush(Color.FromArgb(55, 71, 79));
        using var valueBrush = new SolidBrush(Color.FromArgb(55, 71, 79));
        using var barBrush = new SolidBrush(BarColor);
        using var emptyBrush = new SolidBrush(Color.FromArgb(238, 238, 238));

        g.DrawString(_title, titleFont, titleBrush, new PointF(12, 10));

        if (_data.Count == 0)
        {
            g.DrawString("(no data)", labelFont, labelBrush, new PointF(12, 42));
            return;
        }

        int max = Math.Max(1, _data.Max(item => item.Count));
        int top = 40;
        int rowHeight = Math.Max(18, Math.Min(30, (Height - top - 12) / _data.Count));
        int labelWidth = 145;
        int countWidth = 40;
        int barLeft = labelWidth + 12;
        int barWidth = Math.Max(20, Width - barLeft - countWidth - 16);

        for (int i = 0; i < _data.Count; i++)
        {
            CountItem item = _data[i];
            int y = top + i * rowHeight;
            if (y + rowHeight > Height - 4) break;

            var labelRect = new RectangleF(12, y, labelWidth, rowHeight);
            var countRect = new RectangleF(Width - countWidth - 12, y, countWidth, rowHeight);
            var barBounds = new Rectangle(barLeft, y + 5,
                Math.Max(2, (int)(barWidth * (item.Count / (double)max))),
                Math.Max(8, rowHeight - 10));

            g.DrawString(item.Label, labelFont, labelBrush,
                new RectangleF(labelRect.X, labelRect.Y, labelRect.Width, labelRect.Height));

            g.FillRectangle(emptyBrush, barLeft, y + 5, barWidth, Math.Max(8, rowHeight - 10));
            g.FillRectangle(barBrush, barBounds);

            using var format = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(item.Count.ToString(), valueFont, valueBrush,
                new RectangleF(countRect.X, countRect.Y, countRect.Width, countRect.Height),
                format);
        }
    }
}
