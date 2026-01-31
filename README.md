# 凝視者4.0 (Gazer4.0)

一款功能强大的图片查看器，支持相机抖动效果、脉冲效果和播放列表管理。

## 功能特性

### 图片浏览
- 支持多种图片格式：JPG、PNG、BMP、GIF、TIFF
- 拖放加载图片
- 鼠标滚轮缩放
- 鼠标拖拽平移
- 快捷键导航（左右箭头切换图片）

### 相机效果
- **抖动效果**：模拟手持相机的不稳定感，可调节强度和频率
- **脉冲效果**：周期性画面偏移效果，可调节间隔、阻尼和强度
- 效果可单独启用/禁用

### 播放列表
- 创建和管理图片播放列表
- 支持保存/加载播放列表文件（.gzpl格式）
- 自动播放功能（可设置间隔和随机性）
- 随机播放模式
- 记住每张图片的缩放和平移状态

### 个性化设置
- 自定义背景颜色
- 窗口自动调整大小
- 无边框模式
- 多语言支持（中文、日文、英文）
- 通知提示开关

## 系统要求

- Windows 10/11
- .NET 9.0 Windows Desktop Runtime

## 安装说明

### 方法一：下载安装包（推荐）
1. 前往 [Releases](https://github.com/billye220670/Gazer4.0/releases) 页面
2. 下载最新版本的 `Gazer4.0-setup.exe`
3. 运行安装程序按照向导完成安装

### 方法二：便携版本
1. 前往 [Releases](https://github.com/billye220670/Gazer4.0/releases) 页面
2. 下载 `Gazer4.0-portable.zip`
3. 解压到任意目录
4. 直接运行 `Gazer4.0.exe`

## 使用方法

### 基本操作
- **打开图片**：拖放图片到窗口，或使用快捷键 Ctrl+O
- **缩放**：鼠标滚轮
- **平移**：鼠标左键拖拽
- **重置视图**：按 R 键或点击重置按钮
- **切换图片**：左右箭头键

### 播放列表操作
- **添加图片**：拖放多张图片到窗口
- **切换图片**：播放列表按钮或快捷键
- **自动播放**：勾选自动播放复选框
- **保存播放列表**：Ctrl+S
- **加载播放列表**：Ctrl+L 或双击 .gzpl 文件

### 效果调节
1. 打开设置面板
2. 找到"抖动设置"和"脉冲设置"
3. 调节各项参数
4. 效果实时预览

### 快捷键
| 快捷键 | 功能 |
|--------|------|
| ← / → | 上一张/下一张图片 |
| Ctrl+O | 打开图片 |
| Ctrl+S | 保存播放列表 |
| Ctrl+L | 加载播放列表 |
| R | 重置视图 |
| Space | 播放/暂停自动播放 |
| F | 全屏模式 |
| Esc | 退出全屏 |

## 从源码构建

### 环境要求
- Visual Studio 2022 或 .NET SDK 9.0
- Git

### 构建步骤
1. 克隆仓库：
```bash
git clone https://github.com/billye220670/CorineKit_Gazer.git
cd CorineKit_Gazer
```

2. 使用 Visual Studio 打开 `Gazer4.0.sln`
3. 选择 Release 配置
4. 选择目标框架 net9.0-windows
5. 生成解决方案

或者使用命令行：
```bash
dotnet build Gazer4.0/Gazer4.0.csproj -c Release -r win-x64 --self-contained
```

6. 输出文件位于 `Gazer4.0/bin/Release/net9.0-windows/win-x64/`

## 项目结构

```
Gazer4.0/
├── Gazer4.0.sln           # 解决方案文件
├── Gazer4.0/
│   ├── MainWindow.xaml    # 主窗口界面
│   ├── MainWindow.xaml.cs # 主窗口逻辑
│   ├── Settings.cs        # 设置管理
│   ├── PlaylistManager.cs # 播放列表管理
│   ├── ShakePreset.cs     # 抖动预设
│   ├── LanguageManager.cs # 多语言支持
│   ├── App.xaml           # 应用程序资源
│   ├── AssemblyInfo.cs    # 程序集信息
│   ├── favicon.ico        # 程序图标
│   └── Gazer4.0.csproj    # 项目文件
└── .gitignore             # Git忽略规则
```

## 技术栈

- **框架**：.NET 9.0 WPF
- **语言**：C#
- **构建工具**：MSBuild
- **发布**：GitHub Releases

## 贡献指南

欢迎提交 Issue 和 Pull Request！

## 许可证

本项目采用 MIT 许可证。

## 联系方式

- GitHub：[billye220670/CorineKit_Gazer](https://github.com/billye220670/CorineKit_Gazer)
- 问题反馈：[Issues](https://github.com/billye220670/CorineKit_Gazer/issues)

## 更新日志

### v1.0.0 (2026-01-31)
- 初始版本发布
- 图片浏览基本功能
- 相机抖动和脉冲效果
- 播放列表管理
- 多语言支持
- 自定义背景颜色
