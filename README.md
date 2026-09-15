# Vibecore Hub

[中文](README.md) · [English](README_EN.md) · [日本語](README_JA.md)

Vibecore Hub 是一款原生 Windows 模块化桌面工具栏。顶部小条常驻桌面，功能模块按需向下展开。

<p align="center">
  <img src="docs/images/vibecore-hub-dark.png" width="300" alt="Vibecore Hub 深色主题">
  &nbsp;&nbsp;
  <img src="docs/images/vibecore-hub-light.png" width="300" alt="Vibecore Hub 浅色主题">
</p>

> 当前版本：1.2.1 · Windows 10/11 · x64

轻巧、便携且可自由组合。适合把常用程序、文件、目录、便笺和音频设备集中在一个漂亮的小窗口中。

## 当前模块

- 快捷方式库：应用、文件、文件夹、文本和收纳分组
- 便笺：紧凑卡片、独立查看窗口和多列布局
- 全局搜索：搜索快捷方式、目录和便笺
- 文件夹目录：常用目录快速打开
- 音频切换：保存输入/输出预设，一键统一默认播放与通信设备
- 电脑配置：不同电脑分别保存整套数据和窗口布局

## 技术结构

- Windows WPF
- .NET 8
- 原生 Core Audio 接口
- 每项功能位于独立模块目录，通过 `IHubModule` 注册
- 无 WebView、无第三方运行时依赖

## 本地编译

```powershell
dotnet restore desktop\VibecoreHub.Desktop.csproj -r win-x64
dotnet build desktop\VibecoreHub.Desktop.csproj -c Release -r win-x64
```

## 发布绿色版

```powershell
dotnet publish desktop\VibecoreHub.Desktop.csproj -c Release -r win-x64 --self-contained true -o "release\Vibecore Hub" -p:PublishReadyToRun=false
```

程序数据保存在运行目录的 `data` 文件夹中。该目录已被 `.gitignore` 排除，提交代码前不要将个人数据加入仓库。

## 下载

可在仓库的 [Releases](https://github.com/VibecoreStudio/Vibecore-Hub/releases) 页面下载绿色版。解压后直接运行 `VibecoreHub.exe` 即可。

## 数据与隐私

- 无需安装，设置和内容保存在程序旁的 `data` 文件夹
- 不上传个人数据，换电脑时可连同整个文件夹一起复制
- 不需要注册账号

## 项目状态

项目正在持续完善中，欢迎提交 Issue 分享问题、建议或新的轻量功能创意。
