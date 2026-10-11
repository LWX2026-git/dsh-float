# dsh-float

> 基于 WebView2 的 DeepSeek 增强桌面客户端  
> 支持 AI 生成图片、视频、音乐、PDF、文件、项目，自动继续生成，悬浮工具栏，小窗模式，内置 DSH 服务。

[![Version](https://img.shields.io/badge/version-0.7.32--beta-blue)](https://github.com/lwx2026-git/dsh-float)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)]()

---

## ✨ 功能特性

- **双模式聊天**  
  - 聊天模式：直接嵌入 DeepSeek 官方聊天页面  
  - 任务模式：一键启动本地 DSH 服务（`npx @deepseek-ai/dsh web`），自动打开端口 3080
- **AI 生成增强**  
  自动识别聊天回复中的特殊代码块，并调用相应生成器：
  - `photocode` → SVG 图片 → PNG
  - `videocode` → 多帧 SVG → MP4（需 FFmpeg）
  - `musiccode` → JSON 乐谱 → WAV
  - `pdfcode` → JSON 文档 → PDF
  - `filecode` → 单个文件保存
  - `projectcode` → 多文件项目目录
- **自动继续生成**  
  检测到回复被截断时，自动点击「继续生成」按钮
- **悬浮工具栏**  
  可拖拽的桌面悬浮栏，也可切换为“注入网页”模式，直接在页面顶部显示工具按钮
- **小窗模式**  
  一键将主窗口切换为置顶迷你窗口，方便边工作边聊天
- **后台自动更新**  
  独立后台进程定时检查更新，开机自启（默认写入注册表），不影响主程序启动速度
- **依赖自动安装**  
  安装包自动检测并安装 Node.js v24.21.0 和 FFmpeg，也可跳过手动安装
- **开源免费**  
  MIT 许可证，欢迎自由使用、修改和分发

---

## 📸 界面预览

> 截图待补充，欢迎贡献。

---

## 🚀 快速开始

### 使用安装包（推荐）

1. 下载 `dsh-float-0.7.32-setup.exe`
2. 双击运行，按向导完成安装
3. 安装程序会自动：
   - 检测 Node.js（如未安装，可自动下载 v24.21.0）
   - 检测 FFmpeg（如未安装，可自动下载便携版）
   - 写入开机自启注册表（后台更新检查）
4. 安装完成后启动 `dsh-float`

### 手动构建

#### 环境要求

| 依赖 | 说明 |
|------|------|
| Windows 10/11 | 仅支持 Windows |
| .NET Framework 4.7.2+ | C# 5 兼容 |
| Visual Studio 2019+ | 编译 C# 项目 |
| WebView2 Runtime | [下载](https://go.microsoft.com/fwlink/p/?LinkId=2124703) |
| Inno Setup 6.7.1+ | 编译安装包（可选） |
| Node.js（可选） | 任务模式需要 |
| FFmpeg（可选） | 视频合成需要 |

#### 构建步骤

1. 克隆仓库
   ```bash
   git clone https://github.com/lwx2026-git/dsh-float.git
   cd dsh-float
   ```
2. 使用 Visual Studio 打开解决方案，还原 NuGet 包
3. 编译 `dsh-float.exe`
4. 将以下文件复制到输出目录：
   - `injected.js`
   - `favicon.ico`
   - `splash.png`
   - `dsh_wait.html`
   - `dsh_busy.html`
   - WebView2 SDK 相关 DLL
5. （可选）使用 Inno Setup 编译 `dsh-float.iss` 生成安装包

---

## 🧭 使用说明

### 主界面

- **聊天**：嵌入 DeepSeek 聊天页面
- **任务**：启动本地 DSH 服务并加载
- **小窗**：切换迷你置顶窗口
- **工具**：生成图片/视频/音乐/PDF/文件/项目，打开输出目录、日志、反馈、设置

### 悬浮工具栏

- 默认显示在右下角，可拖动到任意位置
- 点击 **🛠 工具** → **设置** 可切换为“注入网页”模式
- 注入模式下，工具栏会显示在网页顶部，可直接读取输入框内容并发送

### 生成流程

1. 在聊天框输入你的需求（或点击生成按钮输入）
2. 程序发送提示词，等待 AI 回复
3. 自动识别回复中的代码块并生成对应文件
4. 生成完成后弹出提示，文件保存在输出目录

### 快捷键

- `Alt + Space`：隐藏 / 唤回主窗口（可在设置中关闭）

---

## ⚙️ 配置说明

配置文件为程序目录下的 `config.json`，主要字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ServerUrl` | string | 更新服务器地址 |
| `AutoCheckUpdate` | bool | 是否自动检查更新 |
| `AutoClickContinue` | bool | 回复截断时自动点击继续生成 |
| `VideoFps` | int | 视频合成帧率（5~30） |
| `OutputDir` | string | 生成文件的输出目录 |
| `ToolbarMode` | bool | `true`=注入网页，`false`=悬浮栏 |
| `WindowTopmost` | bool | 窗口置顶 |
| `MiniW` / `MiniH` | int | 小窗尺寸 |
| `ToastDuration` | int | Toast 提示时长（毫秒） |
| `AutoStart` | bool | 是否开机自启（默认已写入注册表） |

修改后重启程序生效。

---

## 📦 依赖

- **WebView2 Runtime**（必需）  
  Win10/11 通常已内置，如缺失请[手动安装](https://go.microsoft.com/fwlink/p/?LinkId=2124703)
- **Node.js v24.21.0**（可选）  
  任务模式需要，安装包可自动安装
- **FFmpeg**（可选）  
  视频合成需要，安装包可自动下载便携版

---

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

请确保代码风格一致，并附上必要的测试说明。

---

## 📄 开源协议

本项目采用 **MIT** 许可证。详见 [LICENSE](LICENSE) 文件。

---

## 🙏 致谢

- [DeepSeek](https://deepseek.com)
- [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)
- [FFmpeg](https://ffmpeg.org/)
- [Node.js](https://nodejs.org/)
- 所有贡献者和使用者

---

## ⚠️ 免责声明

本项目为非官方客户端，与 DeepSeek 官方无关。  
使用本软件产生的任何后果由用户自行承担。  
请遵守相关服务条款和法律法规。

---

## 📬 联系

- 问题反馈：[GitHub Issues](https://github.com/lwx2026-git/dsh-float/issues)
- 项目地址：[https://github.com/lwx2026-git/dsh-float](https://github.com/lwx2026-git/dsh-float)

---

**dsh-float** · v0.7.32 beta · Made with ❤️