# 天才交易员 / 泡沫年代 DEMO 0.85.4 —— PC（Windows）移植版

本目录是**可直接双击运行**的 PC 版，基于安卓 APK 里的 Godot 4.7.2 .NET 运行资源 + 编译好的 C# 程序集重建，
并对"初始人物数值上限 / 特质选择限制"做了 IL 级修改。

---

## 一、怎么玩

| 文件 | 用途 |
|---|---|
| `启动游戏.bat` | **双击即玩**（推荐） |
| `启动游戏-控制台排障.bat` | 带控制台启动，能看到日志（出问题先用这个） |
| `打开存档文件夹.bat` | 打开存档目录，方便备份/迁移 |

目录结构：

```
PC版\
├─ 启动游戏.bat / 启动游戏-控制台排障.bat / 打开存档文件夹.bat
├─ engine\   Godot 4.7.2 stable .NET 运行器（含 GodotSharp）
├─ game\     游戏工程（资源 + 程序集 + 构建壳）
├─ _原始程序集备份\   未修改的 BubbleYears.dll / BubbleYears.Simulation.dll
└─ _移植工具\         patcher（打补丁）、verify（逻辑验证）源码
```

> 整个 `PC版` 文件夹可以整体移动到其他盘/目录，路径变了也能跑（脚本用的是相对路径）。

---

## 二、修改内容（存档问题 + 数值/特质解锁）

### 1) 初始人物数值上限解除
- 创建角色的 6 个能力滑块上限：**60 → 999**（`MaxValue` 指令级修改）
- 开局校验"必须正好分配完 210 点"：**已取消**（UI 与模型层两处都放开）
- 模型层 `GameSession.NewCareer` 的能力范围（10–60）与总和（210）校验：**已跳过**（改为跳过异常分支，姓名/长度等基本校验仍在 UI 生效）
- 结果：能力值可任意设置（0–999），不再被夹回 60，也不会因为总和不是 210 而拒绝开局

### 2) 特质选择限制解除
- `GameSession.HeroTraitBudget` 的 5 点预算、以及"最多 3 个正面特质"的限制：**已解除**
  （`CheckHeroTraits` 现在恒返回 `Valid = true`，正面计数固定为 0，UI 不再禁用按钮）
- 提示文案已同步改为"正面特质数量不限，负面特质返还点数。"

### 2.1 存档失败修复（2026-11-11，v1.0.1）
- **`GameSession.ValidateExpansion`**：把 `Need(PaidUpdateVersion <= p.Version, "联网运营")` 这一处调用替换为空实现
  （某款联网运营产品的付费更新版本号超前时，原版会直接抛异常导致**永远存不上档**；其余校验全部保留）
- **诊断脚本**：改为只取文件修改时间（stat），不再读取存档内容，避免与游戏写盘抢句柄
  （原来会导致 `保存失败：The process cannot access the file because it is being used by another process.`）
- **提示文案**：保存/关窗失败时改为提示"原因见 logs\\game.log"，不再一律说"磁盘空间和权限"
- 结果：正面/负面特质可以任意组合、任意数量选择；开局不再因特质被拦

### 3) 存档相关
- 存档实现本身是完整的：写入前会 `Directory.CreateDirectory` 自动建目录；写入用临时文件 + 原子替换；
  手动槽 `manual-1..3`、自动槽 `auto-1..3`；退出/关窗时 `RequestQuit()` 会**先保存再退出**。
- 已在 PC 上实测通过：世界编解码往返、文件层写入/读取、槽位列表、最新档定位（见 `_移植工具\verify`）。
- **存档位置（Windows）**：
  `%APPDATA%\Godot\app_userdata\泡沫年代 - Bubble Years\`
  - `saves\manual-1.json` … `auto-3.json`（外加同名分片目录）
  - `settings.json`（窗口缩放/全屏/垂直同步等偏好）
  用 `打开存档文件夹.bat` 可直达。备份就复制整个文件夹。
- 注意：安卓旧存档位于手机 `/data/data/com.artiverse.geniustrader/files/` 下（需 root/备份工具导出），
  把其中的 `saves` 目录复制到上面的 Windows 路径即可迁移；老文件 `bubble-years-v1.json` 会被当作只读"legacy 档"识别。

### 4) 为"能在 PC 上跑"做的技术修复（否则原样根本起不来）
1. `BubbleYears.dll` 的 PE 架构标记是 **ARM64**，x64 进程直接拒绝加载 → 改为 **AMD64**（纯 IL，安全）
2. 补一份最小 `BubbleYears.deps.json`（原包是 android-arm64 的，缺 `BubbleYears.Simulation` 解析 → 启动即 `FileNotFoundException`）
3. 生成 **30 个 C# 脚本占位文件**（APK 里被替换成 1 字节空文件；Godot 按 `[ScriptPath]` 找路径，缺一个就报"Cannot instantiate C# script"）
4. 主场景从 `.godot/exported/...scn` 迁到 `ui/scenes/retro-main.scn`，并改 `run/main_scene`
5. 新增 `[input_devices] pointing/emulate_touch_from_mouse=true`（否则鼠标无法拖动/滚动触摸面板）
6. 新增构建壳 `BubbleYears.csproj` + `BubbleYears.sln`：不编译源码，构建后把预编译（含补丁）程序集复制到输出目录
7. 去掉工程里指向不存在插件的 `editor_plugins` 配置

---

## 三、已知问题 / 限制

1. **本版是"工程直跑"形态**：`engine\Godot...exe --path game`。
   想做成单文件 exe 需要导出模板 + 资源重映射，而 APK 里**没有原始美术源文件**（只有编译后的 `.ctex/.fontdata`），
   Godot 导出时会因为没有源文件而跳过 `.import`，导致导出的 pck 里纹理全部丢失（已实测）。
   要彻底解决需要反推原始 png/svg/ttf/ogg 或改写场景引用，属于后续工作。
2. 老显卡（本机 Intel HD 4000）会被 Godot 自动降级到 ANGLE（D3D11）渲染，属正常提示。
3. 本机 WASAPI 无法初始化音频设备时会回落到静音驱动（远程/无声卡环境常见），不影响游戏逻辑。
4. 存档里的"正面 x/3"计数现在恒显示 0（限制已解除，显示未同步美化），不影响功能。
5. 启动需要 .NET 运行时：本机没有 .NET 9，所以启动脚本设置了 `DOTNET_ROLL_FORWARD=LatestMajor`，
   借用已装的 .NET 10 运行 net9.0 程序集。若装了 .NET 9 桌面运行时，可去掉该变量。

---

## 四、回滚 / 重新打补丁

- **回滚补丁**：把 `_原始程序集备份\BubbleYears.dll`、`BubbleYears.Simulation.dll` 覆盖到
  `game\assemblies\patched\`，再在 `game` 目录执行 `dotnet build -c Debug`（会复制到 `.godot\mono\temp\bin\Debug`）。
  ⚠️ 覆盖后记得把 `BubbleYears.dll` 的 PE 机器字（偏移 `e_lfanew+4`）改回/改成 `0x8664`，否则 x64 无法加载。
- **重新应用补丁**：`_移植工具\patcher` 里执行 `dotnet run -- apply`（路径常量指向 `D:\portwork\game`，按需修改）。
- **验证逻辑**：`_移植工具\verify` 里执行 `dotnet run`，会跑特质/能力/存档往返共 8 项断言。

---

## 五、本次实测记录

- 主菜单与 `_Ready` 流程：headless 跑 300 帧无脚本异常；带窗口模式可正常出画面
- `CheckHeroTraits(7 个正面特质)` → `Valid=True`
- `NewCareer(能力=999×6, 总和 5994, 7 个正面特质)` → 成功，能力原样保存
- `SaveCodec` 世界编解码往返一致；`SaveRepository` 写入/读回/列表/最新档定位全部通过

---

## 六、存档诊断工具（新增）与"单文件 exe"进展

### 1. 存档工具（详见 `存档问题-诊断与修复.md`）
- `存档自检.bat`：逐槽位检查可读性 + 世界数据往返校验，直接打印失败原因
- `存档备份.bat`：把 `saves` 目录打包成 zip
- `存档修复.bat`：主文件损坏时用 `.bak` 恢复
- 源码在 `_移植工具\savedoctor\`，界面机器人（用于真实点击回归）在 `game\bot\`

### 2. 日志与诊断
- 日志目录：`%APPDATA%\Godot\app_userdata\泡沫年代 - Bubble Years\logs\`
  - `game.log`：引擎日志 + C# 异常栈 + 游戏的"保存失败/读取存档失败"原因（`debug/file_logging` 已开启，保留 20 份）
  - `save-activity.log`：启动自检（引擎/系统/路径/可写性/现有存档）+ 每个存档文件的新建/更新/删除
  - `performance.log`：每秒一行 FPS / 帧时 / 尖峰 / 对象数 / drawcall（v1.0.3 新增）
- 新增自动加载 `game\port\diagnostics.gd`、`game\port\quickkeys.gd`（纯 GDScript，不影响 C# 逻辑）
- 入口：`查看游戏日志.bat` / `存档自检.bat` / `导出诊断包.bat` / `清理存档冗余.bat`
- 已实测：把 `saves` 占位成同名文件触发保存 → `game.log` 记录
  `保存失败：Cannot create '...\saves' because a file or directory with the same name already exists.`

### 3. v1.0.3 性能与体感（详见 `性能与体验-v1.0.3.md`）
- 解除 `Engine.MaxFps = 60` 硬上限（高刷屏不再节奏顿挫）
- 诊断扫描只盯 `saves/*.json`，不再递归 4500 个分片
- 游戏内快捷键：**F1 帮助 / F5 快速保存 / F9 快速读档 / F11 全屏 / F3 性能浮层**
- `存档自检.bat` 增加磁盘占用明细（你的 4 个槽合计约 648 MB，属"滑动存档窗口"设计）
- `清理存档冗余.bat`：只删孤儿 `.parts` 与残留 `*.tmp`

### 4. 单文件 exe：已尝试、暂未采用
尝试过「自研 PCK 打包 + 官方 Windows 模板 exe」路线（资源完全复用、不依赖导出器的资源扫描）：

- ✅ PCK 本身可用：纹理/字体/音频/C# 程序集都能从包里正常加载
- ❌ 但 release 模板运行时会崩（`0xC0000005`）或挂起：
  - 用文本 `project.godot` → 加载 C# 脚本时崩溃
  - 换用 `project.binary` → 不崩溃但主循环卡住
- 已排除程序集补丁因素（换回未打补丁的原版 DLL 同样崩）

结论：**官方 `--export-release` 仍是唯一稳妥的单文件路径**，而它依赖"源资源存在"。
因此下一步要么补齐原始美术/字体/音频源文件（方案 A/B），要么继续深挖模板对 pck 的额外要求。
当前交付仍采用已验证 100% 可用的「引擎 + 工程目录 + 启动脚本」形态。
