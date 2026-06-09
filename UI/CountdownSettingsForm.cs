namespace DeskVerse;

internal sealed class CountdownSettingsForm : Form
{
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
        ClientSize = new Size(360, 214);
        Font = new Font("Microsoft YaHei UI", 9F);

        enabledCheckBox = new CheckBox
        {
            Text = "启用电子屏倒计时",
            Checked = settings.CountdownEnabled,
            AutoSize = true
        };

        titleTextBox = new TextBox
        {
            Text = settings.CountdownTitle,
            MaxLength = 24
        };

        targetDatePicker = new DateTimePicker
        {
            Value = settings.EffectiveCountdownTargetDate(),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy 年 M 月 d 日"
        };

        subtitleTextBox = new TextBox
        {
            Text = settings.CountdownSubtitle,
            MaxLength = 48
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16),
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(enabledCheckBox, 0, 0);
        layout.SetColumnSpan(enabledCheckBox, 2);
        layout.Controls.Add(BuildLabel("目标"), 0, 1);
        layout.Controls.Add(titleTextBox, 1, 1);
        layout.Controls.Add(BuildLabel("日期"), 0, 2);
        layout.Controls.Add(targetDatePicker, 1, 2);
        layout.Controls.Add(BuildLabel("小字"), 0, 3);
        layout.Controls.Add(subtitleTextBox, 1, 3);
        var buttons = BuildButtons();
        layout.Controls.Add(buttons, 0, 4);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
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

    private static Label BuildLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };
    }

    private FlowLayoutPanel BuildButtons()
    {
        var okButton = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Width = 82
        };
        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Width = 82
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        panel.Controls.Add(cancelButton);
        panel.Controls.Add(okButton);
        return panel;
    }
}
