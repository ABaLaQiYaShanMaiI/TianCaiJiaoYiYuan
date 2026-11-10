# 《天才交易员 DEMO》0.85.4 — PC 移植用工程包

本包从 Android APK（`gt.apk`，base 版本 0.85.4）中提取并整理，目标是给 PC 端移植提供尽可能完整、可直接使用的工程材料。

引擎信息（从 `project.binary` 解出）：
- Godot **4.7**（.NET 版），features = `["4.7","C#","GL Compatibility"]`
- 渲染：`gl_compatibility`；Windows 渲染设备驱动：`d3d12`
- C# 程序集名：`BubbleYears`；目标框架 **.NET 9.0**
- 视口 800×500，窗口覆盖 1600×1000，stretch=canvas_items / integer
- 物理引擎：Jolt Physics
- 自定义 user 目录名：`Godot/app_userdata/泡沫年代 - Bubble Years`

## 目录结构
```
project.godot                      # 由 project.binary 还原的工程配置
icon.svg                           # 应用图标
assets/  ui/  .godot/              # 原 APK 的 res:// 资源树（纹理 .ctex / 音频 / 字体 / 场景 .scn / 导入缓存 .godot/imported）
ui/scripts/*.cs                    # APK 里实际带出的 5 个 C# 源文件（UI 脚本）
assemblies/desktop/                # 编译产物：BubbleYears.dll、BubbleYears.Simulation.dll、GodotSharp.dll、deps/runtimeconfig  (net9.0)
_android_export/                   # Android 原始导出元数据：AndroidManifest.xml、project.binary、assets.sparsepck、_cl_、解出的 project_settings_decoded.json
```

## 能直接用/不能直接用的东西（很重要）
**已有：**
- 全部运行资源（导出的二进制形式）：`.ctex`、`.sample`、`.fontdata`、`.scn` 主场景、`.import`/`.remap`、`.godot/imported` 导入缓存
- 两个游戏程序集的编译产物（net9.0，AnyCPU，可直接反编译）
- 5 个 C# UI 脚本源码
- 完整工程设置（已还原成 project.godot）

**缺失：**
- 主游戏逻辑的 C# **源码**：只存在编译后的 `BubbleYears.dll` / `BubbleYears.Simulation.dll`，需要 PC 上用 ILSpy / dnSpy / dotPeek 反编译得到 C#
- 场景的 `.tscn` **源文件**：APK 里只有导出后的二进制 `.scn`（`res://.godot/exported/133200997/export-...-retro-main.scn`），要编辑场景需重建/反编译
- `addons/godot_mcp_bridge`（作者开发用的编辑器插件，导出包里没有）

## 建议的移植路线

### A. 先跑起来验证（最快）
1. 安装 Godot **4.7 .NET 版**（.NET 9 运行时；版本号需与 features 里的 4.7 一致）。
2. 用 Godot 打开本目录（已有 project.godot）。
3. 把 `assemblies/desktop/*.dll` 复制到工程 `.godot/mono/temp/bin/Debug/`（若用 Release 配置则放 `.../Release/`）。
4. 由于没有 `.csproj`，编辑器可能会提示「C# 程序集未构建」；这属于预期，见路线 B。若只想直接看资源运行，可先临时把 `[dotnet]` 配置注释掉观察场景/资源是否加载。

### B. 正式移植（推荐）
1. 用 ILSpy 反编译 `assemblies/desktop/BubbleYears.dll` 和 `BubbleYears.Simulation.dll`，导出 C# 工程源码（这两个 dll 是 AnyCPU，反编译质量通常很高）。
2. 用 Godot 4.7 .NET 新建/打开工程，把导出的 `.cs` 与 `ui/scripts/*.cs` 一起纳入（同名类会冲突，保留一份即可），生成 `BubbleYears.csproj`（模板见 `BubbleYears.csproj.example`），`dotnet build` 通过后再跑。
3. 资源直接复用：`assets/`、`.godot/imported/` 已按 res:// 布局放好；二进制 `.scn`/`.ctex` 需 Godot 在同一大版本下重新导入或转换。
4. 存档路径：PC 上 user:// 实际落在 `%APPDATA%/Godot/app_userdata/泡沫年代 - Bubble Years`（名字来自 `application/config/custom_user_dir_name`）。

### 已知坑
- 工程内 `.godot/exported/...scn` 是 Android 导出产物，PC 端编辑器能加载，但不等于可编辑的 `.tscn`。
- `project.godot` 的主场景我直接指向了导出后的 `.scn`；若想走 remap 机制可改回 `res://ui/scenes/retro-main.tscn`（目录里有对应 `.remap`）。
- `_custom_features="dotnet"`、`editor_plugins.enabled` 里的 MCP 插件在 PC 上没有，已从 project.godot 中去掉。
- 这个包来自已修改过的 APK（角色创建数值被改过、存档校验被旁路过），如果目标是“原版移植”，请以 `gt.apk` 原始审计为准；本次打包没有改动任何 DLL 字节（仅有 `gt.apk` 内导出的原文件）。
