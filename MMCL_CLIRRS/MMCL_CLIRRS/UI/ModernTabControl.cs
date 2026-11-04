namespace MMCL_CLIRRS;

public sealed class ModernTabControl : TabControl
{
    public ModernTabControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(136, 40);
        Appearance = TabAppearance.Normal;
        Font = Theme.Font(9.5f, FontStyle.Bold);
        Padding = new Point(0, 0);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        Rectangle bounds = GetTabRect(e.Index);
        bool selected = SelectedIndex == e.Index;

        using var background = new SolidBrush(selected ? Theme.Surface : Theme.SurfaceAlt);
        e.Graphics.FillRectangle(background, bounds);

        using var border = new Pen(Theme.Border, 1f);
        e.Graphics.DrawLine(border, bounds.Left, bounds.Bottom - 1,
                            bounds.Right, bounds.Bottom - 1);

        var textBounds = new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height - 4);
        TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, textBounds,
            selected ? Theme.Primary : Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if (selected)
        {
            using var accent = new SolidBrush(Theme.Primary);
            e.Graphics.FillRectangle(accent, bounds.Left + 14, bounds.Bottom - 3,
                                     bounds.Width - 28, 3);
        }
    }
}
