# DeskVerse

**DeskVerse** is a quiet desktop verse widget for Windows.

中文名：**桌上诗笺**

It stays on your desktop, shows a short sentence or poem, adapts its colors from your wallpaper, and gets out of the way when you open other apps.

DeskVerse 是一个低功耗 Windows 桌面诗句小组件。它可以显示一言或今日诗词，根据壁纸自动取色，支持收藏、复制、开机自启动和简单的显示设置。

## Features

- Native Windows WinForms app, no Electron or WebView.
- Low power usage: no continuous animation, no high-frequency polling.
- Single instance: launching it repeatedly will not open duplicate widgets.
- Switchable text sources:
  - [Hitokoto 一言](https://developer.hitokoto.cn/sentence/)
  - [Jinrishici 今日诗词](https://www.jinrishici.com/doc/)
- Wallpaper-aware theme colors for background, text, metadata, and border.
- Auto refresh interval: 15 minutes, 30 minutes, 1 hour, daily, or locked.
- Position presets: top center, top left, top right, bottom center.
- Font size presets: small, medium, large.
- Copy and favorite the current sentence.
- Optional startup on login.
- Tray and right-click menus for all common actions.

## Requirements

- Windows 10 or later
- [.NET 9 SDK](https://dotnet.microsoft.com/download) for development
- .NET 9 Desktop Runtime for framework-dependent published builds

## Run From Source

```powershell
dotnet run
```

## Build

```powershell
dotnet build -c Release
```

## Publish

Framework-dependent single-file build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

Output:

```text
bin\Release\net9.0-windows\win-x64\publish\DeskVerse.exe
```

Self-contained single-file build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Usage

- Double-click the widget to refresh.
- Right-click the widget or tray icon to open the menu.
- Use `Settings` to change refresh interval, position, and font size.
- Use `Copy current sentence` or `Favorite current sentence` to keep a line you like.
- Use `Startup on login` to enable or disable auto start.

## Local Data

DeskVerse stores local settings and favorites under:

```text
%AppData%\DeskVerse\settings.json
%AppData%\DeskVerse\favorites.json
```

Jinrishici requires a client token. DeskVerse stores that token in `settings.json` so it does not request a new token on every launch.

## API Notes

Hitokoto:

```text
https://v1.hitokoto.cn/?encode=json&max_length=42
```

Jinrishici:

```text
https://v2.jinrishici.com/token
https://v2.jinrishici.com/sentence
```

Please keep refresh intervals respectful. DeskVerse defaults to 30 minutes.

## License

MIT
