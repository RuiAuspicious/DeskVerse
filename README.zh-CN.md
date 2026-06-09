# DeskVerse

<p>
  <a href="README.md">English</a> | 简体中文
</p>

**DeskVerse** 是一个安静的 Windows 桌面诗句小组件。

中文名：**桌上诗笺**

它会停留在桌面上，显示一句话或一首诗词，根据壁纸自动调整颜色，并在你打开其他软件时自然退到后面。

<p align="center">
  <img src="docs/preview.svg" alt="DeskVerse 预览图" width="820">
</p>

## 功能

- 原生 Windows WinForms 应用，不使用 Electron 或 WebView。
- 低功耗：无持续动画，无高频轮询。
- 单实例运行：重复启动不会打开多个小组件。
- 可切换文本来源：
  - [一言](https://developer.hitokoto.cn/sentence/)
  - [今日诗词](https://www.jinrishici.com/doc/)
- 根据壁纸自动匹配背景、正文、来源文字和边框颜色。
- 自动刷新间隔：15 分钟、30 分钟、1 小时、每天一次，或锁定当前句子。
- 位置预设：顶部居中、左上角、右上角、底部居中。
- 字号预设：小、中、大。
- 支持复制和收藏当前句子。
- 支持开机自启动。
- 通过托盘菜单和右键菜单完成常用操作。

## 系统要求

- Windows 10 或更高版本
- 开发需要 [.NET 9 SDK](https://dotnet.microsoft.com/download)
- 运行 framework-dependent 版本需要 .NET 9 Desktop Runtime

## 下载

从 [Releases](https://github.com/RuiAuspicious/DeskVerse/releases) 下载最新版。

每次发布会生成两个 Windows x64 包：

- `DeskVerse-win-x64-framework-dependent.zip`：体积更小，需要安装 .NET 9 Desktop Runtime。
- `DeskVerse-win-x64-self-contained.zip`：体积更大，不需要单独安装 .NET。

## 从源码运行

```powershell
dotnet run
```

## 构建

```powershell
dotnet build -c Release
```

## 发布

依赖 .NET Runtime 的单文件版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

输出位置：

```text
bin\Release\net9.0-windows\win-x64\publish\DeskVerse.exe
```

自包含单文件版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## 使用

- 双击小组件刷新。
- 右键小组件或托盘图标打开菜单。
- 在 `设置` 中调整刷新间隔、位置和字号。
- 使用 `复制当前句子` 或 `收藏当前句子` 保存喜欢的内容。
- 使用 `开机自启动` 开启或关闭随系统启动。

## 本地数据

DeskVerse 会把设置和收藏保存在当前用户目录下：

```text
%AppData%\DeskVerse\settings.json
%AppData%\DeskVerse\favorites.json
```

今日诗词需要客户端 token。DeskVerse 会把 token 保存在 `settings.json` 中，避免每次启动都重新请求 token。

## 隐私

DeskVerse 不采集遥测，也不会上传你的设置或收藏。

网络请求只会发送到你选择的文本来源：

- 选择 `一言` 时，请求 Hitokoto。
- 选择 `今日诗词` 时，请求 Jinrishici。

壁纸取色只读取本机壁纸文件，不会上传壁纸内容。

## API 说明

一言：

```text
https://v1.hitokoto.cn/?encode=json&max_length=42
```

今日诗词：

```text
https://v2.jinrishici.com/token
https://v2.jinrishici.com/sentence
```

请保持克制的刷新频率。DeskVerse 默认每 30 分钟刷新一次。

## 许可证

MIT
