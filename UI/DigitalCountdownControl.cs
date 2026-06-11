namespace DeskVerse;

internal sealed class DigitalCountdownControl : Control
{
    internal readonly record struct CountdownDigitLayout(
        float DigitWidth,
        float DigitHeight,
        float StartX,
        float StartY,
        RectangleF DigitBounds,
        Rectangle TitleRectangle,
        Rectangle SubtitleRectangle);

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
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(BackColor);

        var days = CountdownCalculator.RemainingDays(DateTime.Today, TargetDate);
        var digitText = Math.Min(days, 99999).ToString();
        var title = $"距离 {TargetTitle} 还有";
        var layout = CalculateLayout(ClientSize, digitText.Length);

        using var titleFont = new Font("Microsoft YaHei UI", 8.4F, FontStyle.Regular);
        using var subtitleFont = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Regular);
        TextRenderer.DrawText(
            e.Graphics,
            title,
            titleFont,
            layout.TitleRectangle,
            secondaryTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        DrawDigits(e.Graphics, digitText, layout);

        var subtitleText = string.IsNullOrWhiteSpace(Subtitle) ? TargetDate.ToString("yyyy年M月d日") : Subtitle;
        TextRenderer.DrawText(
            e.Graphics,
            subtitleText,
            subtitleFont,
            layout.SubtitleRectangle,
            secondaryTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    internal static CountdownDigitLayout CalculateLayout(Size size, int digitCount)
    {
        digitCount = Math.Clamp(digitCount, 1, 5);
        var titleRectangle = new Rectangle(0, 8, size.Width, 19);
        var subtitleRectangle = new Rectangle(0, Math.Max(0, size.Height - 26), size.Width, 18);
        var topBand = titleRectangle.Bottom + 7F;
        var bottomBand = subtitleRectangle.Top - 7F;
        var availableHeight = Math.Max(58F, bottomBand - topBand);
        var unitGap = 7F;
        var digitWidthByHeight = availableHeight / 1.72F;
        var digitWidthByWidth = (size.Width - 126F - unitGap * digitCount) / Math.Max(2, digitCount + 1);
        var digitWidth = Math.Min(56F, Math.Max(28F, Math.Min(digitWidthByHeight, digitWidthByWidth)));
        var digitHeight = digitWidth * 1.72F;
        var totalWidth = digitCount * digitWidth + Math.Max(0, digitCount - 1) * unitGap + 28F;
        var startX = (size.Width - totalWidth) / 2F;
        var startY = topBand + Math.Max(0F, (availableHeight - digitHeight) / 2F);
        var digitBounds = new RectangleF(startX, startY, totalWidth, digitHeight);

        return new CountdownDigitLayout(
            digitWidth,
            digitHeight,
            startX,
            startY,
            digitBounds,
            titleRectangle,
            subtitleRectangle);
    }

    private void DrawDigits(Graphics graphics, string text, CountdownDigitLayout layout)
    {
        var unitGap = 7F;
        for (var index = 0; index < text.Length; index++)
        {
            var digit = text[index] - '0';
            DrawDigit(
                graphics,
                digit,
                layout.StartX + index * (layout.DigitWidth + unitGap),
                layout.StartY,
                layout.DigitWidth,
                layout.DigitHeight);
        }

        using var unitFont = new Font("Microsoft YaHei UI", Math.Max(10.5F, layout.DigitWidth * 0.35F), FontStyle.Regular);
        var unitRectangle = new Rectangle(
            (int)Math.Round(layout.StartX + text.Length * (layout.DigitWidth + unitGap) + 3),
            (int)Math.Round(layout.StartY + layout.DigitHeight - 31),
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
        if (active)
        {
            using var shadowBrush = new SolidBrush(Color.FromArgb(48, textColor));
            var shadowPoints = points.Select(point => new PointF(point.X, point.Y + 1.3F)).ToArray();
            graphics.FillPolygon(shadowBrush, shadowPoints);
        }

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
