using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace DeskVerse;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\DeskVerse.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new HitokotoWidgetForm());
    }
}

internal sealed class HitokotoWidgetForm : Form
{
    private static readonly Uri ApiBaseUri = new("https://v1.hitokoto.cn/");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private const int WidgetMaxWidth = 760;
    private const int WidgetMinWidth = 380;
    private const int WidgetMinHeight = 74;
    private const int WidgetMaxHeight = 142;
    private const int TopOffset = 18;
    private const int CornerRadius = 18;

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
    private bool isRefreshing;

    public HitokotoWidgetForm()
    {
        Text = "DeskVerse";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(246, 242, 232);
        ForeColor = Color.FromArgb(32, 37, 47);
        Opacity = 0.88;
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
            SystemEvents.DisplaySettingsChanged -= HandleDisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= HandleUserPreferenceChanged;
        }

        base.Dispose(disposing);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = RequestTimeout
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("deskverse/0.1");
        return client;
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
        return Icon.FromHandle(bitmap.GetHicon());
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
    }

    private void SaveCurrentFavorite()
    {
        if (lastSentence is null)
        {
            return;
        }

        FavoritesStore.Add(lastSentence);
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
            lastSentence = sentence;
            sentenceLabel.Text = sentence.Text;
            metaLabel.Text = FormatMeta(sentence);
            ResizeToFitSentence();
        }
        catch
        {
            metaLabel.Text = lastSentence is null
                ? "网络暂时不可用"
                : $"{FormatMeta(lastSentence)} · 网络暂时不可用";
            ResizeToFitSentence();
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
        borderColor = theme.Border;
        Opacity = theme.Opacity;
        Invalidate();
    }

    private void PositionAtDesktopTop()
    {
        var workArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
        ResizeToFitSentence();
        TopMost = false;
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

    private static class HitokotoClient
    {
        public static async Task<HitokotoSentence> GetSentenceAsync()
        {
            var url = BuildApiUrl();
            using var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var payload = await JsonSerializer.DeserializeAsync<HitokotoPayload>(stream, JsonOptions);

            if (payload?.Hitokoto is null)
            {
                throw new InvalidOperationException("Hitokoto API returned an empty sentence.");
            }

            return new HitokotoSentence(
                payload.Hitokoto,
                payload.From ?? "",
                payload.FromWho ?? "",
                payload.Uuid is null ? "https://hitokoto.cn" : $"https://hitokoto.cn?uuid={payload.Uuid}",
                SentenceSource.Hitokoto
            );
        }

        private static Uri BuildApiUrl()
        {
            var builder = new UriBuilder(ApiBaseUri);
            var parameters = new List<string>
            {
                "encode=json",
                "max_length=42"
            };

            foreach (var category in new[] { "a", "d", "e", "f", "i", "k" })
            {
                parameters.Add($"c={category}");
            }

            builder.Query = string.Join("&", parameters);
            return builder.Uri;
        }
    }

    private static class JinrishiciClient
    {
        private static readonly Uri TokenUri = new("https://v2.jinrishici.com/token");
        private static readonly Uri SentenceUri = new("https://v2.jinrishici.com/sentence");

        public static async Task<HitokotoSentence> GetSentenceAsync()
        {
            var token = await GetTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get, SentenceUri);
            request.Headers.Add("X-User-Token", token);

            using var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var payload = await JsonSerializer.DeserializeAsync<JinrishiciPayload>(stream, JsonOptions);

            if (payload?.Status != "success" || payload.Data?.Content is null)
            {
                throw new InvalidOperationException(payload?.ErrMessage ?? "Jinrishici API returned an empty sentence.");
            }

            var origin = payload.Data.Origin;
            var from = origin?.Title ?? "";
            var fromWho = string.Join(" · ", new[] { origin?.Dynasty, origin?.Author }.Where(value => !string.IsNullOrWhiteSpace(value)));

            return new HitokotoSentence(
                payload.Data.Content,
                from,
                fromWho,
                "https://www.jinrishici.com/",
                SentenceSource.Jinrishici
            );
        }

        private static async Task<string> GetTokenAsync()
        {
            var settings = AppSettings.Load();
            if (!string.IsNullOrWhiteSpace(settings.JinrishiciToken))
            {
                return settings.JinrishiciToken;
            }

            using var response = await HttpClient.GetAsync(TokenUri);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            var payload = await JsonSerializer.DeserializeAsync<JinrishiciTokenPayload>(stream, JsonOptions);
            if (payload?.Status != "success" || string.IsNullOrWhiteSpace(payload.Data))
            {
                throw new InvalidOperationException("Jinrishici API did not return a token.");
            }

            AppSettings.Save(settings with { JinrishiciToken = payload.Data });
            return payload.Data;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

internal enum SentenceSource
{
    Hitokoto,
    Jinrishici
}

internal enum WidgetPosition
{
    TopCenter,
    TopLeft,
    TopRight,
    BottomCenter
}

internal enum FontSizeMode
{
    Small,
    Medium,
    Large
}

internal sealed record HitokotoSentence(string Text, string From, string FromWho, string Link, SentenceSource Source);

internal sealed record AppSettings(
    SentenceSource Source = SentenceSource.Hitokoto,
    string? JinrishiciToken = null,
    int RefreshMinutes = 30,
    WidgetPosition Position = WidgetPosition.TopCenter,
    FontSizeMode FontSize = FontSizeMode.Medium)
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeskVerse");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}

internal sealed record FavoriteSentence(
    string Text,
    string From,
    string FromWho,
    string Link,
    SentenceSource Source,
    DateTimeOffset SavedAt)
{
    public static FavoriteSentence FromSentence(HitokotoSentence sentence)
    {
        return new FavoriteSentence(
            sentence.Text,
            sentence.From,
            sentence.FromWho,
            sentence.Link,
            sentence.Source,
            DateTimeOffset.Now);
    }
}

internal static class FavoritesStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DeskVerse");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "favorites.json");

    public static void Add(HitokotoSentence sentence)
    {
        var favorites = Load();
        if (favorites.Any(item =>
                item.Text == sentence.Text &&
                item.From == sentence.From &&
                item.FromWho == sentence.FromWho &&
                item.Source == sentence.Source))
        {
            return;
        }

        favorites.Insert(0, FavoriteSentence.FromSentence(sentence));
        Save(favorites);
    }

    public static void OpenFavoritesFile()
    {
        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(FilePath))
        {
            Save([]);
        }

        Process.Start(new ProcessStartInfo(FilePath)
        {
            UseShellExecute = true
        });
    }

    private static List<FavoriteSentence> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<FavoriteSentence>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void Save(List<FavoriteSentence> favorites)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DeskVerse";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName) as string;
        return string.Equals(value, Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(AppName, Application.ExecutablePath);
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}

internal sealed record DesktopTheme(Color Background, Color Text, Color SecondaryText, Color Border, double Opacity)
{
    public static DesktopTheme FromWallpaperColor(Color color)
    {
        var muted = Muted(color);
        var luminance = RelativeLuminance(muted);
        var isDark = luminance < 0.48;

        var background = isDark
            ? Blend(muted, Color.FromArgb(48, 50, 56), 0.2)
            : Blend(muted, Color.FromArgb(244, 247, 246), 0.36);
        var text = isDark
            ? Color.FromArgb(239, 242, 241)
            : Color.FromArgb(26, 31, 39);
        var secondary = isDark
            ? Color.FromArgb(190, 198, 198)
            : Color.FromArgb(82, 94, 94);
        var border = isDark
            ? Color.FromArgb(64, 255, 255, 255)
            : Color.FromArgb(72, 32, 37, 47);

        return new DesktopTheme(background, text, secondary, border, 0.86);
    }

    private static Color Muted(Color color)
    {
        var gray = (color.R + color.G + color.B) / 3;
        return Color.FromArgb(
            BlendChannel(color.R, gray, 0.34),
            BlendChannel(color.G, gray, 0.34),
            BlendChannel(color.B, gray, 0.34));
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        return Color.FromArgb(
            BlendChannel(first.R, second.R, amount),
            BlendChannel(first.G, second.G, amount),
            BlendChannel(first.B, second.B, amount));
    }

    private static int BlendChannel(int first, int second, double amount)
    {
        return (int)Math.Round(first + (second - first) * amount);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Convert(int value)
        {
            var channel = value / 255.0;
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return Convert(color.R) * 0.2126 + Convert(color.G) * 0.7152 + Convert(color.B) * 0.0722;
    }
}

internal static class WallpaperThemeReader
{
    private const int SpiGetDeskWallpaper = 0x0073;
    private const int MaxPath = 260;

    public static Color? TryReadTopCenterColor()
    {
        var path = GetWallpaperPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var image = new Bitmap(path);
            if (image.Width <= 0 || image.Height <= 0)
            {
                return null;
            }

            var sample = GetTopCenterSampleArea(image);
            return AverageColor(image, sample);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetWallpaperPath()
    {
        var builder = new StringBuilder(MaxPath);
        return SystemParametersInfo(SpiGetDeskWallpaper, builder.Capacity, builder, 0)
            ? builder.ToString()
            : null;
    }

    private static Rectangle GetTopCenterSampleArea(Bitmap image)
    {
        var width = Math.Max(1, (int)(image.Width * 0.58));
        var height = Math.Max(1, (int)(image.Height * 0.16));
        var x = Math.Max(0, (image.Width - width) / 2);
        var y = Math.Max(0, (int)(image.Height * 0.03));
        return new Rectangle(x, y, Math.Min(width, image.Width - x), Math.Min(height, image.Height - y));
    }

    private static Color AverageColor(Bitmap image, Rectangle area)
    {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;
        var step = Math.Max(1, Math.Min(area.Width, area.Height) / 72);

        for (var y = area.Top; y < area.Bottom; y += step)
        {
            for (var x = area.Left; x < area.Right; x += step)
            {
                var pixel = image.GetPixel(x, y);
                red += pixel.R;
                green += pixel.G;
                blue += pixel.B;
                count++;
            }
        }

        return count == 0
            ? Color.FromArgb(38, 43, 52)
            : Color.FromArgb((int)(red / count), (int)(green / count), (int)(blue / count));
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(int action, int parameter, StringBuilder value, int winIni);
}

internal sealed record HitokotoPayload(
    [property: JsonPropertyName("hitokoto")] string? Hitokoto,
    [property: JsonPropertyName("from")] string? From,
    [property: JsonPropertyName("from_who")] string? FromWho,
    [property: JsonPropertyName("uuid")] string? Uuid);

internal sealed record JinrishiciTokenPayload(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] string? Data);

internal sealed record JinrishiciPayload(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] JinrishiciData? Data,
    [property: JsonPropertyName("errMessage")] string? ErrMessage);

internal sealed record JinrishiciData(
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("origin")] JinrishiciOrigin? Origin);

internal sealed record JinrishiciOrigin(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("dynasty")] string? Dynasty,
    [property: JsonPropertyName("author")] string? Author);
