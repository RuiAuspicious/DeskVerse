namespace DeskVerse;

internal sealed class HitokotoWidgetForm : Form
{
    private const int WidgetMaxWidth = 760;
    private const int WidgetMinWidth = 380;
    private const int WidgetMinHeight = 74;
    private const int WidgetMaxHeight = 142;
    private const int TopOffset = 18;
    private const int CornerRadius = 18;
    private const int AnimationFrameMs = 16;
    private const int StartupAnimationMs = 220;
    private const int RefreshFadeMs = 150;
    private const int FeedbackMs = 900;

    private readonly Label sentenceLabel;
    private readonly Label metaLabel;
    private readonly NotifyIcon trayIcon;
    private readonly System.Windows.Forms.Timer refreshTimer;
    private readonly System.Windows.Forms.Timer themeTimer;
    private readonly List<ToolStripMenuItem> sourceMenuItems = [];
    private readonly List<ToolStripMenuItem> refreshMenuItems = [];
    private readonly List<ToolStripMenuItem> positionMenuItems = [];
    private readonly List<ToolStripMenuItem> fontSizeMenuItems = [];

    private AppSettings settings;
    private SentenceSource selectedSource;
    private HitokotoSentence? lastSentence;
    private Color borderColor = Color.FromArgb(130, 255, 255, 255);
    private Color baseBorderColor = Color.FromArgb(130, 255, 255, 255);
    private double themeOpacity = 0.86;
    private bool isRefreshing;
    private int feedbackVersion;
    private CancellationTokenSource? windowAnimationCancellation;

    public HitokotoWidgetForm()
    {
        Text = "DeskVerse";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(246, 242, 232);
        ForeColor = Color.FromArgb(32, 37, 47);
        Opacity = 0;
        Width = WidgetMaxWidth;
        Height = WidgetMinHeight;
        MinimumSize = new Size(WidgetMinWidth, WidgetMinHeight);
        DoubleBuffered = true;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        settings = AppSettings.Load();
        selectedSource = settings.Source;

        sentenceLabel = new Label
        {
            Text = "正在寻找一句话...",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = false,
            BackColor = Color.Transparent,
            Font = BuildSentenceFont(settings.FontSize),
            Padding = new Padding(12, 0, 12, 0),
            UseMnemonic = false,
            FlatStyle = FlatStyle.Flat
        };

        metaLabel = new Label
        {
            Text = "Hitokoto",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(95, 107, 103),
            Font = BuildMetaFont(settings.FontSize),
            Padding = new Padding(12, 0, 12, 0),
            UseMnemonic = false,
            FlatStyle = FlatStyle.Flat
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(26, 12, 26, 10),
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
        layout.Controls.Add(sentenceLabel, 0, 0);
        layout.Controls.Add(metaLabel, 0, 1);
        Controls.Add(layout);

        var contextMenu = BuildContextMenu();
        ContextMenuStrip = contextMenu;
        sentenceLabel.ContextMenuStrip = contextMenu;
        metaLabel.ContextMenuStrip = contextMenu;
        DoubleClick += async (_, _) => await RefreshSentenceAsync();

        trayIcon = CreateTrayIcon();
        refreshTimer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1000, settings.RefreshMinutes * 60 * 1000)
        };
        refreshTimer.Tick += async (_, _) => await RefreshSentenceAsync();
        themeTimer = new System.Windows.Forms.Timer
        {
            Interval = (int)TimeSpan.FromMinutes(10).TotalMilliseconds
        };
        themeTimer.Tick += (_, _) => ApplyWallpaperTheme();

        Load += async (_, _) =>
        {
            ApplyWallpaperTheme();
            PositionAtDesktopTop();
            ApplyRefreshTimerSetting();
            themeTimer.Start();
            await PlayStartupAnimationAsync();
            await RefreshSentenceAsync();
        };
        Resize += (_, _) => UpdateRoundedRegion();
        KeyDown += (_, args) =>
        {
            if (args.Control && args.KeyCode == Keys.Q)
            {
                Application.Exit();
            }
        };

        SystemEvents.DisplaySettingsChanged += HandleDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += HandleUserPreferenceChanged;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            const int wsExNoActivate = 0x08000000;
            const int csDropShadow = 0x00020000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExToolWindow | wsExNoActivate;
            parameters.ClassStyle |= csDropShadow;
            return parameters;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var rectangle = ClientRectangle;
        rectangle.Inflate(-1, -1);

        using var path = new GraphicsPath();
        path.AddRoundedRectangle(rectangle, new Size(CornerRadius, CornerRadius));

        using var baseBrush = new SolidBrush(BackColor);
        e.Graphics.FillPath(baseBrush, path);

        using var highlightBrush = new LinearGradientBrush(
            rectangle,
            Color.FromArgb(38, Color.White),
            Color.FromArgb(4, Color.White),
            LinearGradientMode.Vertical);
        e.Graphics.FillPath(highlightBrush, path);

        using var topPen = new Pen(Color.FromArgb(58, Color.White), 1);
        e.Graphics.DrawArc(topPen, rectangle.Left + 1, rectangle.Top + 1, CornerRadius, CornerRadius, 180, 70);
        e.Graphics.DrawLine(topPen, rectangle.Left + CornerRadius / 2, rectangle.Top + 1, rectangle.Right - CornerRadius / 2, rectangle.Top + 1);

        using var pen = new Pen(borderColor);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateRoundedRegion();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refreshTimer.Dispose();
            themeTimer.Dispose();
            trayIcon.Dispose();
            windowAnimationCancellation?.Cancel();
            windowAnimationCancellation?.Dispose();
            SystemEvents.DisplaySettingsChanged -= HandleDisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= HandleUserPreferenceChanged;
        }

        base.Dispose(disposing);
    }

    private NotifyIcon CreateTrayIcon()
    {
        var icon = BuildTrayIcon();

        return new NotifyIcon
        {
            Text = "DeskVerse",
            Icon = icon,
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("刷新一句话", null, async (_, _) => await RefreshSentenceAsync());
        menu.Items.Add("复制当前句子", null, (_, _) => CopyCurrentSentence());
        menu.Items.Add("收藏当前句子", null, (_, _) => SaveCurrentFavorite());
        menu.Items.Add("打开收藏文件", null, (_, _) => FavoritesStore.OpenFavoritesFile());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildSourceMenuItem(SentenceSource.Hitokoto));
        menu.Items.Add(BuildSourceMenuItem(SentenceSource.Jinrishici));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildSettingsMenu());
        menu.Items.Add("打开来源", null, (_, _) => OpenSourceLink());
        menu.Items.Add("重新匹配壁纸颜色", null, (_, _) => ApplyWallpaperTheme());
        menu.Items.Add(BuildStartupMenuItem());
        menu.Items.Add("显示/隐藏", null, (_, _) =>
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                PositionAtDesktopTop();
            }
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Application.Exit());
        return menu;
    }

    private ToolStripMenuItem BuildSettingsMenu()
    {
        var settingsMenu = new ToolStripMenuItem("设置");
        var refreshMenu = new ToolStripMenuItem("自动刷新");
        refreshMenu.DropDownItems.Add(BuildRefreshMenuItem("15 分钟", 15));
        refreshMenu.DropDownItems.Add(BuildRefreshMenuItem("30 分钟", 30));
        refreshMenu.DropDownItems.Add(BuildRefreshMenuItem("1 小时", 60));
        refreshMenu.DropDownItems.Add(BuildRefreshMenuItem("每天一次", 24 * 60));
        refreshMenu.DropDownItems.Add(BuildRefreshMenuItem("锁定当前句子", 0));

        var positionMenu = new ToolStripMenuItem("位置");
        positionMenu.DropDownItems.Add(BuildPositionMenuItem("顶部居中", WidgetPosition.TopCenter));
        positionMenu.DropDownItems.Add(BuildPositionMenuItem("左上角", WidgetPosition.TopLeft));
        positionMenu.DropDownItems.Add(BuildPositionMenuItem("右上角", WidgetPosition.TopRight));
        positionMenu.DropDownItems.Add(BuildPositionMenuItem("底部居中", WidgetPosition.BottomCenter));

        var fontMenu = new ToolStripMenuItem("字号");
        fontMenu.DropDownItems.Add(BuildFontSizeMenuItem("小", FontSizeMode.Small));
        fontMenu.DropDownItems.Add(BuildFontSizeMenuItem("中", FontSizeMode.Medium));
        fontMenu.DropDownItems.Add(BuildFontSizeMenuItem("大", FontSizeMode.Large));

        settingsMenu.DropDownItems.Add(refreshMenu);
        settingsMenu.DropDownItems.Add(positionMenu);
        settingsMenu.DropDownItems.Add(fontMenu);
        return settingsMenu;
    }

    private ToolStripMenuItem BuildStartupMenuItem()
    {
        var item = new ToolStripMenuItem("开机自启动")
        {
            Checked = StartupManager.IsEnabled()
        };
        item.Click += (_, _) =>
        {
            var enable = !StartupManager.IsEnabled();
            StartupManager.SetEnabled(enable);
            item.Checked = enable;
        };
        return item;
    }

    private ToolStripMenuItem BuildSourceMenuItem(SentenceSource source)
    {
        var item = new ToolStripMenuItem(GetSourceMenuText(source))
        {
            Checked = selectedSource == source,
            CheckOnClick = false,
            Tag = source
        };
        item.Click += async (_, _) => await ChangeSourceAsync(source);
        sourceMenuItems.Add(item);
        return item;
    }

    private ToolStripMenuItem BuildRefreshMenuItem(string text, int minutes)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = settings.RefreshMinutes == minutes,
            Tag = minutes
        };
        item.Click += (_, _) => ChangeRefreshInterval(minutes);
        refreshMenuItems.Add(item);
        return item;
    }

    private ToolStripMenuItem BuildPositionMenuItem(string text, WidgetPosition position)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = settings.Position == position,
            Tag = position
        };
        item.Click += (_, _) => ChangePosition(position);
        positionMenuItems.Add(item);
        return item;
    }

    private ToolStripMenuItem BuildFontSizeMenuItem(string text, FontSizeMode mode)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = settings.FontSize == mode,
            Tag = mode
        };
        item.Click += (_, _) => ChangeFontSize(mode);
        fontSizeMenuItems.Add(item);
        return item;
    }

    private static Icon BuildTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var background = new SolidBrush(Color.FromArgb(32, 37, 47));
        graphics.FillRoundedRectangle(background, new Rectangle(0, 0, 31, 31), new Size(8, 8));
        using var line = new Pen(Color.FromArgb(245, 240, 230), 3);
        graphics.DrawLine(line, 8, 11, 24, 11);
        graphics.DrawLine(line, 8, 16, 21, 16);
        graphics.DrawLine(line, 8, 21, 17, 21);

        var handle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private async Task ChangeSourceAsync(SentenceSource source)
    {
        if (selectedSource == source)
        {
            return;
        }

        selectedSource = source;
        settings = settings with { Source = selectedSource };
        AppSettings.Save(settings);
        UpdateSourceMenuChecks();
        sentenceLabel.Text = selectedSource == SentenceSource.Jinrishici
            ? "正在加载今日诗词..."
            : "正在寻找一句话...";
        metaLabel.Text = GetSourceDisplayName(selectedSource);
        ResizeToFitSentence();
        await RefreshSentenceAsync();
    }

    private void ChangeRefreshInterval(int minutes)
    {
        settings = settings with { RefreshMinutes = minutes };
        AppSettings.Save(settings);
        ApplyRefreshTimerSetting();
        UpdateRefreshMenuChecks();
    }

    private void ChangePosition(WidgetPosition position)
    {
        settings = settings with { Position = position };
        AppSettings.Save(settings);
        PositionAtDesktopTop();
        UpdatePositionMenuChecks();
    }

    private void ChangeFontSize(FontSizeMode mode)
    {
        settings = settings with { FontSize = mode };
        AppSettings.Save(settings);
        sentenceLabel.Font = BuildSentenceFont(mode);
        metaLabel.Font = BuildMetaFont(mode);
        ResizeToFitSentence();
        UpdateFontSizeMenuChecks();
    }

    private void ApplyRefreshTimerSetting()
    {
        refreshTimer.Stop();
        if (settings.RefreshMinutes <= 0)
        {
            return;
        }

        refreshTimer.Interval = Math.Max(1000, settings.RefreshMinutes * 60 * 1000);
        refreshTimer.Start();
    }

    private void UpdateSourceMenuChecks()
    {
        foreach (var item in sourceMenuItems)
        {
            if (item.Tag is SentenceSource source)
            {
                item.Checked = selectedSource == source;
            }
        }
    }

    private void UpdateRefreshMenuChecks()
    {
        foreach (var item in refreshMenuItems)
        {
            if (item.Tag is int minutes)
            {
                item.Checked = settings.RefreshMinutes == minutes;
            }
        }
    }

    private void UpdatePositionMenuChecks()
    {
        foreach (var item in positionMenuItems)
        {
            if (item.Tag is WidgetPosition position)
            {
                item.Checked = settings.Position == position;
            }
        }
    }

    private void UpdateFontSizeMenuChecks()
    {
        foreach (var item in fontSizeMenuItems)
        {
            if (item.Tag is FontSizeMode mode)
            {
                item.Checked = settings.FontSize == mode;
            }
        }
    }

    private void CopyCurrentSentence()
    {
        if (lastSentence is null)
        {
            return;
        }

        Clipboard.SetText(FormatShareText(lastSentence));
        ShowFeedback("已复制");
    }

    private void SaveCurrentFavorite()
    {
        if (lastSentence is null)
        {
            return;
        }

        FavoritesStore.Add(lastSentence);
        ShowFeedback("已收藏");
    }

    private async Task RefreshSentenceAsync()
    {
        if (isRefreshing) return;

        isRefreshing = true;

        try
        {
            var sentence = selectedSource == SentenceSource.Jinrishici
                ? await JinrishiciClient.GetSentenceAsync()
                : await HitokotoClient.GetSentenceAsync();
            await AnimateOpacityAsync(Math.Max(0.35, themeOpacity - 0.22), RefreshFadeMs);
            feedbackVersion++;
            lastSentence = sentence;
            sentenceLabel.Text = sentence.Text;
            metaLabel.Text = FormatMeta(sentence);
            ResizeToFitSentence();
            await AnimateOpacityAsync(themeOpacity, RefreshFadeMs);
        }
        catch (Exception exception)
        {
            AppLogger.Log(exception, $"Failed to refresh sentence from {selectedSource}");
            metaLabel.Text = lastSentence is null
                ? "网络暂时不可用"
                : $"{FormatMeta(lastSentence)} · 网络暂时不可用";
            feedbackVersion++;
            ResizeToFitSentence();
            await AnimateOpacityAsync(themeOpacity, RefreshFadeMs);
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void OpenSourceLink()
    {
        var url = lastSentence?.Link ?? "https://hitokoto.cn";
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private static string FormatMeta(HitokotoSentence sentence)
    {
        var source = string.IsNullOrWhiteSpace(sentence.From)
            ? GetSourceDisplayName(sentence.Source)
            : $"《{sentence.From}》";
        var author = string.IsNullOrWhiteSpace(sentence.FromWho) ? "" : $" · {sentence.FromWho}";
        return $"{source}{author}";
    }

    private static string FormatShareText(HitokotoSentence sentence)
    {
        return $"{sentence.Text}\r\n{FormatMeta(sentence)}";
    }

    private static Font BuildSentenceFont(FontSizeMode mode)
    {
        var size = mode switch
        {
            FontSizeMode.Small => 12.8F,
            FontSizeMode.Large => 15.8F,
            _ => 14.2F
        };
        return new Font("Microsoft YaHei UI", size, FontStyle.Regular);
    }

    private static Font BuildMetaFont(FontSizeMode mode)
    {
        var size = mode switch
        {
            FontSizeMode.Small => 7.8F,
            FontSizeMode.Large => 8.8F,
            _ => 8.2F
        };
        return new Font("Microsoft YaHei UI", size, FontStyle.Regular);
    }

    private static string GetSourceDisplayName(SentenceSource source)
    {
        return source == SentenceSource.Jinrishici ? "今日诗词" : "一言";
    }

    private static string GetSourceMenuText(SentenceSource source)
    {
        return source == SentenceSource.Jinrishici ? "来源：今日诗词" : "来源：一言";
    }

    private void ApplyWallpaperTheme()
    {
        var wallpaperColor = WallpaperThemeReader.TryReadTopCenterColor() ?? Color.FromArgb(38, 43, 52);
        var theme = DesktopTheme.FromWallpaperColor(wallpaperColor);

        BackColor = theme.Background;
        ForeColor = theme.Text;
        sentenceLabel.ForeColor = theme.Text;
        metaLabel.ForeColor = theme.SecondaryText;
        baseBorderColor = theme.Border;
        borderColor = baseBorderColor;
        themeOpacity = theme.Opacity;
        if (!isRefreshing && Visible)
        {
            Opacity = themeOpacity;
        }
        Invalidate();
    }

    private void PositionAtDesktopTop()
    {
        ResizeToFitSentence();
        TopMost = false;
    }

    private async Task PlayStartupAnimationAsync()
    {
        var targetTop = Top;
        Top = targetTop - 8;
        Opacity = 0;

        await AnimateWindowAsync(
            StartupAnimationMs,
            progress =>
            {
                var eased = EaseOutCubic(progress);
                Top = targetTop - (int)Math.Round((1 - eased) * 8);
                Opacity = themeOpacity * eased;
            });

        Top = targetTop;
        Opacity = themeOpacity;
    }

    private Task AnimateOpacityAsync(double targetOpacity, int durationMs)
    {
        var startOpacity = Opacity;
        return AnimateWindowAsync(
            durationMs,
            progress =>
            {
                var eased = EaseOutCubic(progress);
                Opacity = startOpacity + (targetOpacity - startOpacity) * eased;
            });
    }

    private Task AnimateWindowAsync(int durationMs, Action<double> applyFrame)
    {
        windowAnimationCancellation?.Cancel();
        windowAnimationCancellation?.Dispose();
        windowAnimationCancellation = new CancellationTokenSource();
        var cancellationToken = windowAnimationCancellation.Token;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var elapsed = 0;
        var timer = new System.Windows.Forms.Timer
        {
            Interval = AnimationFrameMs
        };

        timer.Tick += (_, _) =>
        {
            if (cancellationToken.IsCancellationRequested || IsDisposed)
            {
                timer.Stop();
                timer.Dispose();
                completion.TrySetCanceled(cancellationToken);
                return;
            }

            elapsed += timer.Interval;
            var progress = Math.Clamp(elapsed / (double)durationMs, 0, 1);
            applyFrame(progress);

            if (progress >= 1)
            {
                timer.Stop();
                timer.Dispose();
                completion.TrySetResult();
            }
        };

        applyFrame(0);
        timer.Start();
        return completion.Task;
    }

    private async void ShowFeedback(string text)
    {
        var version = ++feedbackVersion;
        var previousMeta = metaLabel.Text;
        var highlight = ControlPaint.Light(baseBorderColor, 0.65F);

        metaLabel.Text = text;
        borderColor = Color.FromArgb(170, highlight);
        Invalidate();

        await Task.Delay(FeedbackMs);
        if (version != feedbackVersion || IsDisposed)
        {
            return;
        }

        metaLabel.Text = previousMeta;
        borderColor = baseBorderColor;
        Invalidate();
    }

    internal static double EaseOutCubic(double progress)
    {
        var normalized = Math.Clamp(progress, 0, 1);
        return 1 - Math.Pow(1 - normalized, 3);
    }

    private void ResizeToFitSentence()
    {
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        var maxWidth = Math.Min(WidgetMaxWidth, Math.Max(WidgetMinWidth, workArea.Width - 96));
        var sentenceWidth = TextRenderer.MeasureText(
            sentenceLabel.Text,
            sentenceLabel.Font,
            new Size(int.MaxValue, WidgetMaxHeight),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        var metaWidth = TextRenderer.MeasureText(
            metaLabel.Text,
            metaLabel.Font,
            new Size(int.MaxValue, 24),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        var desiredWidth = Math.Max(sentenceWidth + 104, metaWidth + 88);
        Width = Math.Clamp(desiredWidth, WidgetMinWidth, maxWidth);

        var textWidth = Math.Max(120, ClientSize.Width - 76);
        var measured = TextRenderer.MeasureText(
            sentenceLabel.Text,
            sentenceLabel.Font,
            new Size(textWidth, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        var desiredHeight = measured.Height + 50;
        Height = Math.Clamp(desiredHeight, WidgetMinHeight, WidgetMaxHeight);
        (Left, Top) = settings.Position switch
        {
            WidgetPosition.TopLeft => (workArea.Left + TopOffset, workArea.Top + TopOffset),
            WidgetPosition.TopRight => (workArea.Right - Width - TopOffset, workArea.Top + TopOffset),
            WidgetPosition.BottomCenter => (workArea.Left + (workArea.Width - Width) / 2, workArea.Bottom - Height - TopOffset),
            _ => (workArea.Left + (workArea.Width - Width) / 2, workArea.Top + TopOffset)
        };
        UpdateRoundedRegion();
    }

    private void UpdateRoundedRegion()
    {
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(ClientRectangle, new Size(CornerRadius, CornerRadius));
        Region = new Region(path);
        Invalidate();
    }

    private void HandleDisplaySettingsChanged(object? sender, EventArgs e)
    {
        ApplyWallpaperTheme();
        PositionAtDesktopTop();
    }

    private void HandleUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.Desktop or UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            ApplyWallpaperTheme();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint icon);
}
