using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

public sealed class StatBarChart : Control
{
    private static readonly Color[] BarColors =
    {
        Theme.Primary,
        Theme.Success,
        Theme.Info,
        Theme.Gold,
        Theme.Danger,
        Theme.Warning
    };

    private string _title = "";
    private IReadOnlyList<CountItem> _data = Array.Empty<CountItem>();

    public StatBarChart()
    {
        BackColor = Theme.Surface;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
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
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var background = new SolidBrush(Theme.Surface);
        g.FillRectangle(background, ClientRectangle);

        using GraphicsPath card = Theme.RoundedRect(
            new Rectangle(0, 0, Width - 1, Height - 1), 14);
        using var border = new Pen(Theme.Border, 1f);
        g.DrawPath(border, card);

        using var titleFont = Theme.Font(10.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Theme.Ink);
        g.DrawString(_title, titleFont, titleBrush, new PointF(18, 14));

        using var accent = new SolidBrush(Theme.Gold);
        g.FillRectangle(accent, 18, 38, 28, 3);

        if (_data.Count == 0)
        {
            using var emptyFont = Theme.Font(9f);
            using var emptyBrush = new SolidBrush(Theme.Muted);
            using var centered = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("No data available yet.", emptyFont, emptyBrush,
                new RectangleF(0, 56, Width, Height - 56), centered);
            return;
        }

        int max = Math.Max(1, _data.Max(item => item.Count));
        const int top = 56;
        int available = Math.Max(20, Height - top - 16);
        int rowHeight = Math.Max(22, Math.Min(34, available / _data.Count));
        const int labelWidth = 150;
        const int countWidth = 42;
        int barLeft = 18 + labelWidth + 12;
        int barWidth = Math.Max(30, Width - barLeft - countWidth - 22);

        using var labelFont = Theme.Font(8.75f);
        using var valueFont = Theme.Font(8.75f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Theme.Muted);
        using var valueBrush = new SolidBrush(Theme.Ink);
        using var trackBrush = new SolidBrush(Theme.SurfaceAlt);

        for (int i = 0; i < _data.Count; i++)
        {
            CountItem item = _data[i];
            int y = top + i * rowHeight;
            if (y + rowHeight > Height - 4) break;

            var labelRect = new RectangleF(18, y, labelWidth, rowHeight);
            g.DrawString(item.Label, labelFont, labelBrush, labelRect);

            int barY = y + Math.Max(4, (rowHeight - 10) / 2);
            var track = new Rectangle(barLeft, barY, barWidth, 10);
            using GraphicsPath trackPath = Theme.RoundedRect(track, 5);
            g.FillPath(trackBrush, trackPath);

            int fillWidth = Math.Max(8, (int)(barWidth * (item.Count / (double)max)));
            var fill = new Rectangle(barLeft, barY, fillWidth, 10);
            using GraphicsPath fillPath = Theme.RoundedRect(fill, 5);
            using var fillBrush = new SolidBrush(BarColors[i % BarColors.Length]);
            g.FillPath(fillBrush, fillPath);

            var countRect = new RectangleF(Width - countWidth - 18, y, countWidth, rowHeight);
            using var right = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(item.Count.ToString(), valueFont, valueBrush, countRect, right);
        }
    }
}
