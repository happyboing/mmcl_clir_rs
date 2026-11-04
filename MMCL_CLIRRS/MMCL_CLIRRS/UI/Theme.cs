using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MMCL_CLIRRS;

/// <summary>
/// Central palette and small factory helpers so every screen shares the same visual language.
/// </summary>
public static class Theme
{
    public static readonly Color Primary = Color.FromArgb(198, 40, 40);
    public static readonly Color PrimaryDark = Color.FromArgb(150, 24, 24);
    public static readonly Color PrimaryDeep = Color.FromArgb(92, 12, 12);
    public static readonly Color PrimarySoft = Color.FromArgb(253, 236, 236);
    public static readonly Color Gold = Color.FromArgb(212, 162, 74);
    public static readonly Color Background = Color.FromArgb(247, 244, 241);
    public static readonly Color Surface = Color.White;
    public static readonly Color SurfaceAlt = Color.FromArgb(252, 250, 248);
    public static readonly Color Ink = Color.FromArgb(45, 42, 48);
    public static readonly Color Muted = Color.FromArgb(116, 108, 112);
    public static readonly Color Border = Color.FromArgb(230, 223, 217);
    public static readonly Color Success = Color.FromArgb(41, 122, 92);
    public static readonly Color SuccessSoft = Color.FromArgb(231, 245, 238);
    public static readonly Color Warning = Color.FromArgb(191, 112, 24);
    public static readonly Color WarningSoft = Color.FromArgb(255, 244, 226);
    public static readonly Color Danger = Color.FromArgb(142, 24, 28);
    public static readonly Color DangerSoft = Color.FromArgb(251, 233, 234);
    public static readonly Color Info = Color.FromArgb(48, 95, 146);
    public static readonly Color InfoSoft = Color.FromArgb(232, 240, 249);
    public static readonly Color Selection = Color.FromArgb(250, 228, 228);

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
        => new("Segoe UI", size, style);

    public static Label Label(string text, float size = 9.5f, FontStyle style = FontStyle.Regular,
                              Color? color = null)
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = Font(size, style),
            ForeColor = color ?? Ink,
            BackColor = Color.Transparent
        };

    public static Label Caption(string text)
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = Font(8.5f, FontStyle.Bold),
            ForeColor = Muted,
            BackColor = Color.Transparent
        };

    public static void StyleForm(Form form)
    {
        form.Font = Font(9.5f);
        form.BackColor = Background;
        form.ForeColor = Ink;
        form.StartPosition = FormStartPosition.CenterParent;
        form.AutoScaleMode = AutoScaleMode.None;
    }

    public static Panel Divider(int y, int width, int x = 0, Color? color = null)
        => new()
        {
            Location = new Point(x, y),
            Size = new Size(width, 1),
            BackColor = color ?? Border
        };

    public static AppButton MakeButton(string text, AppButtonKind kind, int width, int height = 40)
        => new()
        {
            Text = text,
            Kind = kind,
            Size = new Size(width, height)
        };

    public static Panel CreateHeader(string title, string subtitle, Form form,
                                     string? closeText = "Close", bool showClose = true,
                                     int height = 88)
    {
        int titleY = height >= 80 ? 15 : 5;
        int subtitleY = height >= 80 ? 52 : 44;
        int closeY = Math.Max(8, (height - 34) / 2);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = height,
            BackColor = Primary
        };
        header.Paint += (s, e) =>
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var gradient = new LinearGradientBrush(
                header.ClientRectangle, PrimaryDark, Primary, LinearGradientMode.Horizontal);
            g.FillRectangle(gradient, header.ClientRectangle);

            using var glow = new SolidBrush(Color.FromArgb(24, 255, 255, 255));
            g.FillEllipse(glow, header.Width - 170, -110, 260, 260);
            g.FillEllipse(glow, header.Width - 40, 34, 90, 90);

            using var accent = new SolidBrush(Gold);
            g.FillRectangle(accent, 0, header.Height - 3, header.Width, 3);
        };

        header.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(28, titleY),
            AutoSize = true,
            Font = Font(17.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        });

        header.Controls.Add(new Label
        {
            Text = subtitle,
            Location = new Point(30, subtitleY),
            AutoSize = true,
            Font = Font(9.25f),
            ForeColor = Color.FromArgb(245, 205, 205),
            BackColor = Color.Transparent
        });

        if (showClose && closeText != null)
        {
            var btnClose = new AppButton
            {
                Text = closeText,
                Kind = AppButtonKind.Quiet,
                Size = new Size(116, 34),
                Location = new Point(header.Width - 140, closeY),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.Click += (s, e) => form.Close();
            header.Controls.Add(btnClose);
        }

        return header;
    }

    public static FieldShell AddField(Control parent, string label, Control field,
                                      int x, int y, int width, int fieldHeight = 40)
    {
        var caption = Caption(label);
        caption.Location = new Point(x, y);

        var shell = new FieldShell(field, fieldHeight)
        {
            Location = new Point(x, y + 18),
            Width = width
        };

        parent.Controls.Add(caption);
        parent.Controls.Add(shell);
        return shell;
    }

    public static Panel AddReadOnlyList(Control parent, string label, ListBox list,
                                        int x, int y, int width, int height)
    {
        var caption = Caption(label);
        caption.Location = new Point(x, y);
        parent.Controls.Add(caption);

        var host = new CardPanel
        {
            Location = new Point(x, y + 18),
            Size = new Size(width, height),
            BackColor = SurfaceAlt,
            BorderColor = Border,
            Radius = 9,
            Padding = new Padding(10, 7, 10, 7)
        };

        list.Dock = DockStyle.Fill;
        list.BorderStyle = BorderStyle.None;
        list.BackColor = SurfaceAlt;
        list.Font = Font(9.25f);
        list.IntegralHeight = false;
        host.Controls.Add(list);
        parent.Controls.Add(host);
        return host;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Color.FromArgb(239, 234, 230);
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.RowTemplate.Height = 36;
        grid.ColumnHeadersHeight = 40;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = Font(8.75f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Ink;
        grid.DefaultCellStyle.SelectionBackColor = Selection;
        grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;
        grid.DefaultCellStyle.Font = Font(9.25f);
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 250, 249);

        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
    }

    public static void StyleList(ListBox list)
    {
        list.BorderStyle = BorderStyle.None;
        list.BackColor = Surface;
        list.ForeColor = Ink;
        list.Font = Font(9.25f);
        list.IntegralHeight = false;
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(2, radius * 2);
        var path = new GraphicsPath();

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        var arc = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public enum AppButtonKind
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    Success,
    Quiet
}

public sealed class AppButton : Button
{
    private bool _hover;
    private bool _pressed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AppButtonKind Kind { get; set; } = AppButtonKind.Primary;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 10;

    public AppButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint, true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Font = Theme.Font(9.5f, FontStyle.Bold);
        Height = 40;
        TabStop = true;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (Width <= 0 || Height <= 0) return;

        using GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), Radius);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        (Color back, Color fore, Color border) = Palette();

        if (!Enabled)
        {
            back = Color.FromArgb(236, 232, 229);
            fore = Color.FromArgb(155, 148, 145);
            border = back;
        }
        else if (_pressed)
        {
            back = Darken(back, 18);
            border = back;
        }
        else if (_hover)
        {
            back = Kind == AppButtonKind.Ghost
                ? Theme.PrimarySoft
                : Darken(back, 8);
            border = Kind == AppButtonKind.Secondary ? Theme.Primary : back;
        }

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = Theme.RoundedRect(bounds, Radius);
        using var fill = new SolidBrush(back);
        g.FillPath(fill, path);

        if (border.A > 0)
        {
            using var pen = new Pen(border, 1.1f);
            g.DrawPath(pen, path);
        }

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding;
        TextRenderer.DrawText(g, Text, Font, bounds, fore, flags);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(bounds, -4, -4);
            ControlPaint.DrawFocusRectangle(g, focus, fore, back);
        }
    }

    private (Color back, Color fore, Color border) Palette() => Kind switch
    {
        AppButtonKind.Secondary => (Theme.Surface, Theme.Primary, Theme.Border),
        AppButtonKind.Ghost => (Color.Transparent, Theme.Primary, Color.Transparent),
        AppButtonKind.Danger => (Theme.Danger, Color.White, Theme.Danger),
        AppButtonKind.Success => (Theme.Success, Color.White, Theme.Success),
        AppButtonKind.Quiet => (Color.FromArgb(168, 30, 30), Color.White,
                                Color.FromArgb(120, 255, 255, 255)),
        _ => (Theme.Primary, Color.White, Theme.Primary)
    };

    private static Color Darken(Color color, int amount)
        => Color.FromArgb(color.A,
                          Math.Max(0, color.R - amount),
                          Math.Max(0, color.G - amount),
                          Math.Max(0, color.B - amount));

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }
}

public class CardPanel : Panel
{
    private int _radius = 14;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius
    {
        get => _radius;
        set { _radius = value; UpdateRegion(); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Theme.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DrawBorder { get; set; } = true;

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint, true);
        BackColor = Theme.Surface;
        Padding = new Padding(20);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0) return;

        using GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), Radius);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        if (DrawBorder)
        {
            using GraphicsPath path = Theme.RoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var pen = new Pen(BorderColor, 1f);
            e.Graphics.DrawPath(pen, path);
        }
    }
}

public sealed class FieldShell : Panel
{
    private readonly Control _field;
    private bool _focused;

    public Control Field => _field;

    public FieldShell(Control field, int height = 40)
    {
        _field = field;
        Height = height;
        BackColor = Theme.Surface;
        Padding = new Padding(12, 6, 12, 6);

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint, true);

        switch (field)
        {
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.None;
                textBox.BackColor = Theme.Surface;
                textBox.ForeColor = Theme.Ink;
                textBox.Font = Theme.Font(10f);
                break;
            case ComboBox comboBox:
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.BackColor = Theme.Surface;
                comboBox.ForeColor = Theme.Ink;
                comboBox.Font = Theme.Font(9.75f);
                break;
            case NumericUpDown numeric:
                numeric.BorderStyle = BorderStyle.None;
                numeric.BackColor = Theme.Surface;
                numeric.ForeColor = Theme.Ink;
                numeric.Font = Theme.Font(9.75f);
                break;
            case DateTimePicker picker:
                picker.Font = Theme.Font(9.75f);
                picker.CalendarForeColor = Theme.Ink;
                break;
        }

        field.Dock = DockStyle.Fill;
        field.Enter += (s, e) => { _focused = true; Invalidate(); };
        field.Leave += (s, e) => { _focused = false; Invalidate(); };
        field.BackColorChanged += (s, e) => Invalidate();

        Controls.Add(field);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width <= 0 || Height <= 0) return;

        using GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), 9);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Theme.Surface);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        using GraphicsPath path = Theme.RoundedRect(
            new Rectangle(0, 0, Width - 1, Height - 1), 9);
        using var pen = new Pen(_focused ? Theme.Primary : Theme.Border, _focused ? 1.6f : 1f);
        e.Graphics.DrawPath(pen, path);
    }
}

public sealed class PillLabel : Control
{
    private Color _fill = Theme.Success;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => _fill;
        set { _fill = value; Invalidate(); }
    }

    public PillLabel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);
        Font = Theme.Font(8.25f, FontStyle.Bold);
        ForeColor = Color.White;
        BackColor = Theme.Surface;
        Size = new Size(110, 24);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = Theme.RoundedRect(
            new Rectangle(0, 0, Width - 1, Height - 1), Math.Min(12, Height / 2));
        using var fill = new SolidBrush(_fill);
        e.Graphics.FillPath(fill, path);

        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

public sealed class MenuCard : Control
{
    private string _title = "";
    private string _subtitle = "";
    private string _badge = "";
    private string _icon = "\uE8A7";
    private Color _accent = Theme.Primary;
    private bool _hover;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Badge
    {
        get => _badge;
        set { _badge = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Icon
    {
        get => _icon;
        set { _icon = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Accent
    {
        get => _accent;
        set { _accent = value; Invalidate(); }
    }

    public MenuCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
        Font = Theme.Font(9.5f);
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using GraphicsPath card = Theme.RoundedRect(bounds, 14);

        using var fill = new SolidBrush(_hover ? Theme.PrimarySoft : Theme.Surface);
        g.FillPath(fill, card);

        using var border = new Pen(_hover ? _accent : Theme.Border, _hover ? 1.5f : 1f);
        g.DrawPath(border, card);

        if (_hover)
        {
            using var accent = new SolidBrush(_accent);
            using GraphicsPath strip = Theme.RoundedRect(new Rectangle(0, 16, 4, Height - 32), 2);
            g.FillPath(accent, strip);
        }

        var iconBounds = new Rectangle(20, (Height - 46) / 2, 46, 46);
        using GraphicsPath iconCard = Theme.RoundedRect(iconBounds, 12);
        using var iconFill = new SolidBrush(Blend(_accent, Theme.Surface, 0.86f));
        g.FillPath(iconFill, iconCard);

        using var iconFont = new Font("Segoe MDL2 Assets", 15f);
        using var iconBrush = new SolidBrush(_accent);
        using var iconFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(_icon, iconFont, iconBrush,
            new RectangleF(iconBounds.X, iconBounds.Y, iconBounds.Width, iconBounds.Height),
            iconFormat);

        int textLeft = 84;
        int textWidth = Width - textLeft - 46;
        if (_badge.Length > 0) textWidth -= 46;

        using var titleFont = Theme.Font(11f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Theme.Ink);
        g.DrawString(_title, titleFont, titleBrush,
            new RectangleF(textLeft, 21, textWidth, 24));

        using var subFont = Theme.Font(8.6f);
        using var subBrush = new SolidBrush(Theme.Muted);
        using var subFormat = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(_subtitle, subFont, subBrush,
            new RectangleF(textLeft, 48, Math.Max(20, Width - textLeft - 60), 22), subFormat);

        if (_badge.Length > 0)
        {
            int badgeWidth = Math.Max(26, 16 + _badge.Length * 8);
            var badgeBounds = new Rectangle(Width - badgeWidth - 42,
                                            (Height - 24) / 2, badgeWidth, 24);
            using GraphicsPath pill = Theme.RoundedRect(badgeBounds, 12);
            using var badgeFill = new SolidBrush(_accent);
            g.FillPath(badgeFill, pill);

            TextRenderer.DrawText(g, _badge, Theme.Font(8f, FontStyle.Bold), badgeBounds,
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        using var chevronFont = Theme.Font(15f);
        using var chevronBrush = new SolidBrush(_hover ? _accent : Theme.Muted);
        g.DrawString("›", chevronFont, chevronBrush,
            new RectangleF(Width - 34, (Height - 28) / 2f, 20, 28));
    }

    private static Color Blend(Color foreground, Color background, float backgroundAmount)
    {
        float foregroundAmount = 1f - backgroundAmount;
        return Color.FromArgb(
            (int)(foreground.R * foregroundAmount + background.R * backgroundAmount),
            (int)(foreground.G * foregroundAmount + background.G * backgroundAmount),
            (int)(foreground.B * foregroundAmount + background.B * backgroundAmount));
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }
}
