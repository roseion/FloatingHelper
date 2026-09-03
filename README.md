# 浮动助手（FloatingHelper）

> Windows 全局划词工具栏：在任何地方选中文字，立即弹出复制 / 打开 / 搜索等操作。类 PopClip，插件化架构。

![平台](https://img.shields.io/badge/platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![许可证](https://img.shields.io/badge/license-MIT-green)

## 功能

- **全局划词**：在浏览器、记事本、Office 等任意可选中文字的地方拖选，自动弹出工具栏
- **智能打开**：选中 URL / 邮箱 / 本地文件路径，用系统默认程序打开（图片、音乐、视频等）
- **复制 / 搜索**：一键复制到剪贴板，或用默认搜索引擎搜索
- **站点直达**：附六个独立安装的外部插件——小红书 / 知乎 / 微博站内搜索，豆包 / 元宝 / DeepSeek 一键提问；各为一个 DLL，复制到 `plugins/` 即可单独安装，不改主程序
- **插件化**：所有动作都是插件，内置插件管理界面，支持加载外部 DLL
- **多显示器 & 高 DPI**：工具栏跟随鼠标所在屏幕，Per-Monitor DPI 精确定位
- **托盘常驻**：开机自启、插件管理、关于信息，单实例运行
- **稳定性**：休眠/锁屏后自动重连钩子，5 秒无操作自动消失，文件日志

## 技术栈

- C# + WPF（.NET 8 LTS）
- 全局低层鼠标钩子（WH_MOUSE_LL）
- UI Automation（TextPattern）选区捕获
- 插件动态加载（AssemblyLoadContext）
- JSON 配置持久化

## 快速开始

### 下载安装

从 [Releases](https://github.com/roseion/FloatingHelper/releases) 下载最新安装包或便携版。

### 从源码构建

```bash
# 克隆
git clone https://github.com/roseion/FloatingHelper.git
cd FloatingHelper

# 构建
dotnet build FloatingHelper.slnx

# 运行
dotnet run --project src/FloatingHelper.App

# 发布单文件便携版
dotnet publish src/FloatingHelper.App -c Release -r win-x64 \
  --self-contained true /p:PublishSingleFile=true -o publish
```

## 项目结构

```
FloatingHelper.slnx
├── src/
│   ├── FloatingHelper.Core/          # 核心：插件接口、配置、选区捕获、工具类
│   ├── FloatingHelper.App/           # WPF 主程序：钩子、工具栏、托盘、设置
│   ├── FloatingHelper.Plugins.Builtin/ # 内置插件：复制/智能打开/搜索
│   ├── FloatingHelper.Plugins.XiaohongshuSearch/ # 外部插件①：小红书搜索
│   ├── FloatingHelper.Plugins.ZhihuSearch/       # 外部插件②：知乎搜索
│   ├── FloatingHelper.Plugins.WeiboSearch/       # 外部插件③：微博搜索
│   ├── FloatingHelper.Plugins.DoubaoAsk/         # 外部插件④：向豆包提问
│   ├── FloatingHelper.Plugins.YuanbaoAsk/        # 外部插件⑤：向元宝提问
│   └── FloatingHelper.Plugins.DeepSeekAsk/       # 外部插件⑥：向 DeepSeek 提问
├── tests/
│   └── FloatingHelper.Core.Tests/    # xUnit 单元测试
└── docs/
    ├── 浮动助手-产品设计文档-MVP.md   # 产品设计文档
    └── 插件设计接口指南.md            # 插件开发指南
```

## 插件开发

所有功能以插件形式提供。开发一个新插件只需实现 `IPlugin` 接口，详见 [插件设计接口指南](docs/插件设计接口指南.md)。

## 配置

配置文件位于 `%AppData%\FloatingHelper\settings.json`，日志位于 `%AppData%\FloatingHelper\logs\`。

## 许可证

[MIT](LICENSE)

## 作者

- 主页：[www.oldgao.com](https://www.oldgao.com)
- QQ：638694
