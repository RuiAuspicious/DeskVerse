namespace DeskVerse;

internal sealed class GlassSettingsForm : Form
{
    private static readonly Color DialogBackground = Color.FromArgb(246, 248, 251);
    private static readonly Color PrimaryText = Color.FromArgb(26, 31, 39);
    private static readonly Color SecondaryText = Color.FromArgb(96, 105, 118);
    private static readonly Color Accent = Color.FromArgb(33, 111, 219);

    private readonly TrackBar intensityTrackBar;
    private readonly Label valueLabel;
    private readonly GlassPreviewPanel previewPanel;
    private readonly DesktopTheme previewTheme;

    public GlassSettingsForm(int glassIntensity, LiquidGlassMaterial currentMaterial)
    {
        Text = "Liquid Glass";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 300);
        MinimumSize = new Size(420, 300);
        Font = new Font("Microsoft YaHei UI", 9.2F);
        BackColor = DialogBackground;
        ForeColor = PrimaryText;

        previewTheme = DesktopTheme.FromWallpaperColor(currentMaterial.Surface);
        previewPanel = new GlassPreviewPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 16)
        };

        valueLabel = new Label
        {
            Dock = DockStyle.Right,
            Width = 58,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = PrimaryText,
            Font = new Font("Microsoft YaHei UI", 10.2F, FontStyle.Bold)
        };

        intensityTrackBar = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            TickStyle = TickStyle.None,
            SmallChange = 1,
            LargeChange = 10,
            Value = LiquidGlassMaterial.NormalizeIntensity(glassIntensity),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 0)
        };
        intensityTrackBar.ValueChanged += (_, _) => UpdatePreview();

        Controls.Add(BuildContent());
        UpdatePreview();
    }

    public int GlassIntensity => intensityTrackBar.Value;

    private Control BuildContent()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(22, 20, 22, 18),
            BackColor = DialogBackground
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        layout.Controls.Add(new Label
        {
            Text = "玻璃外观",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = PrimaryText,
            Font = new Font("Microsoft YaHei UI", 12.4F, FontStyle.Bold)
        }, 0, 0);
        layout.Controls.Add(previewPanel, 0, 1);
        layout.Controls.Add(BuildScaleLabels(), 0, 2);
        layout.Controls.Add(BuildSliderRow(), 0, 3);
        layout.Controls.Add(BuildButtons(), 0, 4);

        return layout;
    }

    private Control BuildScaleLabels()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = DialogBackground
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        panel.Controls.Add(new Label
        {
            Text = "磨砂",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SecondaryText
        }, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = "清透",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = SecondaryText
        }, 1, 0);
        return panel;
    }

    private Control BuildSliderRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = DialogBackground
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
        panel.Controls.Add(intensityTrackBar, 0, 0);
        panel.Controls.Add(valueLabel, 1, 0);
        return panel;
    }

    private Control BuildButtons()
    {
        var okButton = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Width = 92,
            Height = 30,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okButton.FlatAppearance.BorderSize = 0;

        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Width = 92,
            Height = 30,
            FlatStyle = FlatStyle.System
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = DialogBackground
        };
        panel.Controls.Add(cancelButton);
        panel.Controls.Add(okButton);
        return panel;
    }

    private void UpdatePreview()
    {
        valueLabel.Text = $"{intensityTrackBar.Value}%";
        previewPanel.Material = LiquidGlassMaterial.FromTheme(previewTheme, intensityTrackBar.Value);
        previewPanel.Invalidate();
    }

    private sealed class GlassPreviewPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public LiquidGlassMaterial Material { get; set; } = LiquidGlassMaterial.FromTheme(
            DesktopTheme.FromWallpaperColor(Color.FromArgb(38, 43, 52)),
            68);

        public GlassPreviewPanel()
        {
            DoubleBuffered = true;
            BackColor = DialogBackground;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);

            var background = ClientRectangle;
            background.Inflate(-1, -1);
            using var backdrop = new LinearGradientBrush(
                background,
                Color.FromArgb(41, 48, 58),
                Color.FromArgb(169, 190, 202),
                LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRoundedRectangle(backdrop, background, new Size(18, 18));

            using var glowBrush = new SolidBrush(Color.FromArgb(46, 255, 246, 225));
            e.Graphics.FillEllipse(glowBrush, background.Left - 24, background.Bottom - 70, 190, 116);

            var card = new Rectangle(background.Left + 36, background.Top + 52, background.Width - 72, 64);
            using var path = new GraphicsPath();
            var radius = Math.Min(30, (card.Height - 2) / 2);
            path.AddRoundedRectangle(card, new Size(radius, radius));
            using var surface = new LinearGradientBrush(card, Material.SurfaceTop, Material.SurfaceBottom, LinearGradientMode.Vertical);
            e.Graphics.FillPath(surface, path);
            using var shine = new LinearGradientBrush(card, Material.Specular, Color.FromArgb(6, Color.White), LinearGradientMode.Vertical);
            e.Graphics.FillPath(shine, path);
            using var border = new Pen(Material.Border);
            e.Graphics.DrawPath(border, path);
            using var inner = new Pen(Material.InnerBorder);
            var innerCard = card;
            innerCard.Inflate(-1, -1);
            using var innerPath = new GraphicsPath();
            innerPath.AddRoundedRectangle(innerCard, new Size(Math.Max(10, radius - 4), Math.Max(10, radius - 4)));
            e.Graphics.DrawPath(inner, innerPath);

            using var titleFont = new Font("Microsoft YaHei UI", 11.8F, FontStyle.Regular);
            using var metaFont = new Font("Microsoft YaHei UI", 7.6F, FontStyle.Regular);
            TextRenderer.DrawText(
                e.Graphics,
                "身处井隅，心向璀璨。",
                titleFont,
                new Rectangle(card.Left + 18, card.Top + 13, card.Width - 36, 24),
                Material.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(
                e.Graphics,
                "DeskVerse",
                metaFont,
                new Rectangle(card.Left + 18, card.Top + 39, card.Width - 36, 18),
                Material.SecondaryText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
