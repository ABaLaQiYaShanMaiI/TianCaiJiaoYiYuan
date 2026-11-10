# TianCaiJiaoYiYuan（天才交易员 / 泡沫年代 DEMO 0.85.4）—— Windows 移植版

从安卓 APK（Godot **4.7.2 stable .NET** / net9.0）重建的 PC 可运行工程，并对
**初始人物数值上限**与**特质选择限制**做了 IL 级解锁。

> 原始 `gt-mod3.apk`、`resources/`（jadx 解包 + APK assets）、`sources/`（反编译 Java）体积过大或涉及原包版权，
> 未纳入本仓库；本仓库只保留"移植所需的工程文件、补丁产物、工具与说明"。

---

## 1. 目录结构

```
.
├─ 说明-PC移植.md                 完整技术说明（改动清单 / 实测记录 / 回滚方式）
├─ PC版/
│  ├─ 启动游戏.bat                双击即玩
│  ├─ 启动游戏-控制台排障.bat      带日志启动
│  ├─ 打开存档文件夹.bat
│  ├─ game/                       游戏工程（Godot 项目根）
│  │  ├─ project.godot            已迁移主场景、已开启鼠标→触摸模拟
│  │  ├─ BubbleYears.csproj/.sln  "构建壳"：不编译源码，构建后复制预编译程序集
│  │  ├─ build_shell/             构建壳的标记类 + 自定义最小 deps.json
│  │  ├─ assets/                  资源（.import 存根 + 开场视频 + 立绘配置 + 字体许可）
│  │  ├─ ui/                      主场景 .scn + 30 个 C# 脚本占位（Godot 按路径解析）
│  │  ├─ .godot/imported/         编译后的纹理/字体/音频（原图缺失，这是唯一可用副本）
│  │  ├─ .godot/exported/         原始导出主场景（存档备查）
│  │  └─ assemblies/
│  │     ├─ patched/              ★ 已打补丁的两个程序集（实际运行用的就是它们）
│  │     ├─ original/             未修改的原始程序集（回滚用）
│  │     └─ desktop/              来自 APK 的原始发布副本（JSON 里写的是 android-arm64）
│  ├─ _原始程序集备份/            与 assemblies/original 相同，便于回滚
│  └─ _移植工具/
│     ├─ patcher/                 Mono.Cecil 补丁工具（可重复应用）
│     └─ verify/                  逻辑验证程序（特质/能力/存档共 8 项断言）
└─ 手机游戏移植和存档Bug修复.txt   原记录文件（空）
```

---

## 2. 运行（Windows x64）

1. 下载引擎：**Godot 4.7.2 stable .NET（mono）win64**（约 173 MB）
   <https://github.com/godotengine/godot/releases/tag/4.7.2-stable> →
   `Godot_v4.7.2-stable_mono_win64.zip`
2. 解压到 `PC版/engine/`，确保 `PC版/engine/Godot_v4.7.2-stable_mono_win64.exe` 存在。
3. 双击 `PC版/启动游戏.bat`。

启动脚本会设置 `DOTNET_ROLL_FORWARD=LatestMajor`：游戏程序集是 **net9.0**，
若机器上只装了 .NET 8/10，也能通过 roll-forward 运行（装了 .NET 9 桌面运行时则更稳）。

### 重新构建程序集（可选）

工程使用"构建壳"：`BubbleYears.csproj` 不编译任何源码，构建后把 `assemblies/patched/*.dll`
复制到 `.godot/mono/temp/bin/Debug/`。因此**不需要 C# 源码**也能构建：

```bash
cd PC版/game
dotnet build -c Debug        # 需要联网还原 Godot.NET.Sdk 4.7.2
```

---

## 3. 已做的移植修复（否则原样起不来）

| # | 问题 | 处理 |
|---|---|---|
| 1 | `BubbleYears.dll` 的 PE 机器字是 **ARM64**，x64 进程拒绝加载 | 改为 `0x8664`（纯 IL，安全） |
| 2 | `deps.json` 是 android-arm64，`BubbleYears.Simulation` 解析失败 | 手写最小 `deps.json` |
| 3 | 30 个脚本路径只有 5 个占位文件 | 按 DLL 的 `[ScriptPath]` 补齐 30 个 1 字节占位 |
| 4 | 主场景指向 `.godot/exported/...`（缓存目录） | 迁到 `ui/scenes/retro-main.scn` |
| 5 | 触摸滚动对鼠标无效 | `input_devices/pointing/emulate_touch_from_mouse=true` |
| 6 | 无 `.csproj/.sln`，Godot 认为程序集未构建 | 构建壳工程 |

---

## 4. 数值 / 特质解锁（IL 补丁）

| 位置 | 原行为 | 现在 |
|---|---|---|
| `DrawCareerCreation` 六个 `SpinBox` | `MaxValue = 60` | **999** |
| 开局校验（UI） | `Abilities.Sum() != 210` 拦截 | **已移除** |
| `GameSession.NewCareer` | 能力 10–60、总和 210、特质合法才放行 | **跳过异常分支** |
| `GameSession.CheckHeroTraits` | `数量≤3 && 点数≥0` | **恒 `Valid=true`**，正面计数置 0 |
| 提示文案 | "正面特质最多3个" | "正面特质数量不限，负面特质返还点数。" |

重新应用：`cd PC版/_移植工具/patcher && dotnet run -- apply`（脚本内路径常量需按实际位置调整）。
逻辑验证：`cd PC版/_移植工具/verify && dotnet run`。

---

## 5. 存档

- 位置：`%APPDATA%\Godot\app_userdata\泡沫年代 - Bubble Years\`
  - `saves\manual-1..3.json`、`auto-1..3.json` + 分片目录
  - `settings.json`（窗口缩放/全屏/VSync）
- 关窗/退出走 `RequestQuit()`：**先保存再退出**，失败会提示而不是静默丢档。
- 安卓旧档：手机 `/data/data/com.artiverse.geniustrader/files/` 下的 `saves` 拷到上面的目录即可。

---

## 6. 已知限制

1. **未提供单文件 exe**：APK 中没有原始 png/svg/ttf/ogg（只有编译后的 `.ctex/.fontdata`），
   Godot 导出时会跳过这些资源的 `.import`，导出包的纹理路径无法解析（已实测多种导出配置）。
   因此以"工程直跑"形式提供；要做成 exe 需先反推原始美术或改写场景资源引用。
2. 旧显卡会被 Godot 自动降级到 ANGLE（D3D11）渲染，属正常提示。
3. 无可用音频输出端点时回落到静音驱动，不影响逻辑。
4. 存档界面"正面 x/3"计数恒显示 0（限制已解除，未做文案美化）。

---

## 7. 版权说明

游戏本体、美术、音频、字体及原始 APK 版权归原作者/原发行方所有；
本仓库仅包含为**个人学习用途的移植适配**所需的配置、补丁与工具代码。
