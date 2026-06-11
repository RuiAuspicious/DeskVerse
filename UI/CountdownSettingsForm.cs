namespace DeskVerse;

internal sealed class CountdownSettingsForm : Form
{
    private static readonly Color DialogBackground = Color.FromArgb(246, 248, 251);
    private static readonly Color PanelBackground = Color.FromArgb(255, 255, 255);
    private static readonly Color PrimaryText = Color.FromArgb(31, 35, 40);
    private static readonly Color SecondaryText = Color.FromArgb(96, 105, 118);
    private static readonly Color Accent = Color.FromArgb(33, 111, 219);

    private readonly CheckBox enabledCheckBox;
    private readonly TextBox titleTextBox;
    private readonly DateTimePicker targetDatePicker;
    private readonly TextBox subtitleTextBox;

    public CountdownSettingsForm(AppSettings settings)
    {
        Text = "电子屏倒计时";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 360);
        MinimumSize = new Size(480, 360);
        Font = new Font("Microsoft YaHei UI", 9.2F);
        BackColor = DialogBackground;
        ForeColor = PrimaryText;

        enabledCheckBox = new CheckBox
        {
            Text = "启用电子屏倒计时",
            Checked = settings.CountdownEnabled,
            AutoSize = true,
            ForeColor = PrimaryText,
            Margin = new Padding(0, 0, 0, 10)
        };

        titleTextBox = new TextBox
        {
            Text = settings.CountdownTitle,
            MaxLength = 24,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        targetDatePicker = new DateTimePicker
        {
            Value = settings.EffectiveCountdownTargetDate(),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy 年 M 月 d 日",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        subtitleTextBox = new TextBox
        {
            Text = settings.CountdownSubtitle,
            MaxLength = 48,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        Controls.Add(BuildContent());
    }

    public AppSettings BuildSettings(AppSettings settings)
    {
        return settings with
        {
            CountdownEnabled = enabledCheckBox.Checked,
            CountdownTitle = string.IsNullOrWhiteSpace(titleTextBox.Text) ? "目标日" : titleTextBox.Text.Trim(),
            CountdownTargetDate = targetDatePicker.Value.Date,
            CountdownSubtitle = string.IsNullOrWhiteSpace(subtitleTextBox.Text) ? "把重要的日子放在桌面上" : subtitleTextBox.Text.Trim()
        };
    }

    private Control BuildContent()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(22, 20, 22, 18),
            BackColor = DialogBackground
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

        layout.Controls.Add(BuildHeader(), 0, 0);
        layout.Controls.Add(enabledCheckBox, 0, 1);
        layout.Controls.Add(BuildFieldsPanel(), 0, 2);
        layout.Controls.Add(BuildButtons(), 0, 3);

        return layout;
    }

    private static Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = DialogBackground,
            Margin = Padding.Empty
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        panel.Controls.Add(new Label
        {
            Text = "电子屏倒计时",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 13.2F, FontStyle.Bold),
            ForeColor = PrimaryText,
            AutoSize = false
        }, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = "在桌面句子和重要日倒计时之间自动轮播。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = SecondaryText,
            AutoSize = false
        }, 0, 1);
        return panel;
    }

    private Control BuildFieldsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = PanelBackground,
            Padding = new Padding(18, 16, 18, 10),
            Margin = Padding.Empty
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        panel.Controls.Add(BuildField("目标", titleTextBox), 0, 0);
        panel.Controls.Add(BuildField("日期", targetDatePicker), 0, 1);
        panel.Controls.Add(BuildField("小字", subtitleTextBox), 0, 2);
        return new RoundedPanel(panel, PanelBackground, Color.FromArgb(226, 232, 240));
    }

    private static Control BuildField(string label, Control input)
    {
        var field = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = PanelBackground,
            Margin = new Padding(0, 0, 0, 8)
        };
        field.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        field.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        field.Controls.Add(BuildLabel(label), 0, 0);
        field.Controls.Add(input, 0, 1);
        return field;
    }

    private static Label BuildLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            AutoSize = false,
            ForeColor = SecondaryText,
            Font = new Font("Microsoft YaHei UI", 8.6F, FontStyle.Regular)
        };
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
            Padding = new Padding(0, 10, 0, 0),
            BackColor = DialogBackground
        };
        panel.Controls.Add(cancelButton);
        panel.Controls.Add(okButton);
        return panel;
    }

    private sealed class RoundedPanel : Panel
    {
        private readonly Color fillColor;
        private readonly Color strokeColor;

        public RoundedPanel(Control content, Color fillColor, Color strokeColor)
        {
            this.fillColor = fillColor;
            this.strokeColor = strokeColor;
            Dock = DockStyle.Fill;
            Padding = new Padding(1);
            Margin = Padding.Empty;
            BackColor = DialogBackground;
            DoubleBuffered = true;
            Controls.Add(content);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using var path = new GraphicsPath();
            var rectangle = ClientRectangle;
            rectangle.Inflate(-1, -1);
            path.AddRoundedRectangle(rectangle, new Size(12, 12));
            Region?.Dispose();
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rectangle = ClientRectangle;
            rectangle.Inflate(-1, -1);
            using var path = new GraphicsPath();
            path.AddRoundedRectangle(rectangle, new Size(12, 12));
            using var brush = new SolidBrush(fillColor);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(strokeColor);
            e.Graphics.DrawPath(pen, path);
        }
    }
}
