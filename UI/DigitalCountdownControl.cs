namespace DeskVerse;

internal sealed class DigitalCountdownControl : Control
{
    private static readonly bool[][] DigitSegments =
    [
        [true, true, true, true, true, true, false],
        [false, true, true, false, false, false, false],
        [true, true, false, true, true, false, true],
        [true, true, true, true, false, false, true],
        [false, true, true, false, false, true, true],
        [true, false, true, true, false, true, true],
        [true, false, true, true, true, true, true],
        [true, true, true, false, false, false, false],
        [true, true, true, true, true, true, true],
        [true, true, true, true, false, true, true]
    ];

    private Color textColor = Color.FromArgb(239, 242, 241);
    private Color secondaryTextColor = Color.FromArgb(190, 198, 198);
    private Color inactiveSegmentColor = Color.FromArgb(36, 239, 242, 241);

    public DigitalCountdownControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string TargetTitle { get; set; } = "目标日";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Subtitle { get; set; } = "把重要的日子放在桌面上";

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DateTime TargetDate { get; set; } = DateTime.Today.AddDays(30);

    public void ApplyTheme(DesktopTheme theme)
    {
        textColor = theme.Text;
        secondaryTextColor = theme.SecondaryText;
        inactiveSegmentColor = Color.FromArgb(34, theme.Text);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var days = CountdownCalculator.RemainingDays(DateTime.Today, TargetDate);
        var digitText = Math.Min(days, 99999).ToString();
        var title = $"距离 {TargetTitle} 还有";

        using var titleFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular);
        using var subtitleFont = new Font("Microsoft YaHei UI", 7.6F, FontStyle.Regular);
        var titleRectangle = new Rectangle(0, 8, Width, 18);
        TextRenderer.DrawText(
            e.Graphics,
            title,
            titleFont,
            titleRectangle,
            secondaryTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        DrawDigits(e.Graphics, digitText);

        var subtitleText = string.IsNullOrWhiteSpace(Subtitle) ? TargetDate.ToString("yyyy年M月d日") : Subtitle;
        var subtitleRectangle = new Rectangle(0, Height - 29, Width, 20);
        TextRenderer.DrawText(
            e.Graphics,
            subtitleText,
            subtitleFont,
            subtitleRectangle,
            secondaryTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawDigits(Graphics graphics, string text)
    {
        var digitCount = text.Length;
        var unitGap = 7F;
        var digitWidth = Math.Min(54F, Math.Max(30F, (Width - 126F - unitGap * digitCount) / Math.Max(2, digitCount + 1)));
        var digitHeight = digitWidth * 1.72F;
        var totalWidth = digitCount * digitWidth + Math.Max(0, digitCount - 1) * unitGap + 28F;
        var startX = (Width - totalWidth) / 2F;
        var startY = Math.Max(28F, (Height - digitHeight) / 2F - 1F);

        for (var index = 0; index < text.Length; index++)
        {
            var digit = text[index] - '0';
            DrawDigit(graphics, digit, startX + index * (digitWidth + unitGap), startY, digitWidth, digitHeight);
        }

        using var unitFont = new Font("Microsoft YaHei UI", Math.Max(11F, digitWidth * 0.36F), FontStyle.Regular);
        var unitRectangle = new Rectangle(
            (int)Math.Round(startX + digitCount * (digitWidth + unitGap) + 3),
            (int)Math.Round(startY + digitHeight - 31),
            28,
            24);
        TextRenderer.DrawText(
            graphics,
            "天",
            unitFont,
            unitRectangle,
            secondaryTextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private void DrawDigit(Graphics graphics, int digit, float x, float y, float width, float height)
    {
        var thickness = Math.Max(5F, width * 0.16F);
        var half = height / 2F;
        var segments = DigitSegments[digit];

        DrawSegment(graphics, HorizontalSegment(x, y, width, thickness), segments[0]);
        DrawSegment(graphics, VerticalSegment(x + width - thickness, y, thickness, half), segments[1]);
        DrawSegment(graphics, VerticalSegment(x + width - thickness, y + half, thickness, half), segments[2]);
        DrawSegment(graphics, HorizontalSegment(x, y + height - thickness, width, thickness), segments[3]);
        DrawSegment(graphics, VerticalSegment(x, y + half, thickness, half), segments[4]);
        DrawSegment(graphics, VerticalSegment(x, y, thickness, half), segments[5]);
        DrawSegment(graphics, HorizontalSegment(x, y + half - thickness / 2F, width, thickness), segments[6]);
    }

    private void DrawSegment(Graphics graphics, PointF[] points, bool active)
    {
        using var brush = new SolidBrush(active ? textColor : inactiveSegmentColor);
        graphics.FillPolygon(brush, points);
    }

    private static PointF[] HorizontalSegment(float x, float y, float width, float thickness)
    {
        var bevel = thickness * 0.72F;
        return
        [
            new PointF(x + bevel, y),
            new PointF(x + width - bevel, y),
            new PointF(x + width, y + thickness / 2F),
            new PointF(x + width - bevel, y + thickness),
            new PointF(x + bevel, y + thickness),
            new PointF(x, y + thickness / 2F)
        ];
    }

    private static PointF[] VerticalSegment(float x, float y, float thickness, float height)
    {
        var bevel = thickness * 0.72F;
        return
        [
            new PointF(x + thickness / 2F, y),
            new PointF(x + thickness, y + bevel),
            new PointF(x + thickness, y + height - bevel),
            new PointF(x + thickness / 2F, y + height),
            new PointF(x, y + height - bevel),
            new PointF(x, y + bevel)
        ];
    }
}
