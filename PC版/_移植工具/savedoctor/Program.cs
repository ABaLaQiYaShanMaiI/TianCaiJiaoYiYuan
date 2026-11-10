using System.IO.Compression;
using System.Text;
using BubbleYears.Simulation;

Console.OutputEncoding = Encoding.UTF8;

string baseDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Godot", "app_userdata", "泡沫年代 - Bubble Years");
string savesDir = args.Length > 0 && Directory.Exists(args[0]) ? args[0] : Path.Combine(baseDir, "saves");
string command = args.Length > 1 ? args[1].ToLowerInvariant() : "report";

Console.WriteLine("存档目录: " + savesDir);
if (!Directory.Exists(savesDir))
{
    Console.WriteLine("[!] 目录不存在：游戏还没保存过，或存档被移动到别处。");
    Console.WriteLine("    期望路径：" + savesDir);
    return 2;
}

var repo = new SaveRepository(savesDir);
var slots = repo.List();
Console.WriteLine($"槽位数量: {slots.Count}");
Console.WriteLine();

int bad = 0, okCount = 0;
foreach (var s in slots)
{
    string state = !s.Exists ? "空槽" : (s.Readable ? (s.RecoveredBackup ? "可读(已从备份恢复)" : "可读") : "不可读");
    Console.WriteLine($"[{s.Key}] {state}  标题={s.Title}  公司={s.Company}  天={s.Day}  保存于={s.SavedAt:yyyy-MM-dd HH:mm:ss}  错误={s.Error}");
    if (!s.Exists) continue;
    if (!s.Readable) { bad++; continue; }
    okCount++;
    try
    {
        var r = repo.Read(s.Key);
        var world = r.World;
        string roundTrip = SaveCodec.Write(world);
        var back = SaveCodec.Read(roundTrip);
        bool consistent = back.Day == world.Day && back.Firms.Count == world.Firms.Count;
        Console.WriteLine($"        世界: 天={world.Day} 公司数={world.Firms.Count} 员工={world.Workforce?.People?.Count ?? 0} 股票={world.Stocks.Count} 编解码一致={consistent} 世界字节={roundTrip.Length}");
        if (!consistent) bad++;
    }
    catch (Exception ex)
    {
        bad++;
        Console.WriteLine($"        [X] 读取失败: {ex.GetType().Name}: {ex.Message}");
    }
}

// 遗留单文件存档
string legacy = Path.Combine(baseDir, "bubble-years-v1.json");
Console.WriteLine();
if (File.Exists(legacy))
{
    var fi = new FileInfo(legacy);
    Console.WriteLine($"[legacy] {legacy}  {fi.Length} 字节  修改于 {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
    try { var r = repo.Read("legacy"); Console.WriteLine($"        可读: 天={r.World.Day}"); }
    catch (Exception ex) { Console.WriteLine($"        [X] 不可读: {ex.Message}"); bad++; }
}
else Console.WriteLine("[legacy] 无旧版单文件存档");

Console.WriteLine();
Console.WriteLine($"汇总: 可读 {okCount} / 异常 {bad}");

if (command == "fix")
{
    Console.WriteLine("=== 尝试修复：用 .bak 覆盖损坏主文件 ===");
    int fixedCount = 0;
    foreach (var s in slots.Where(x => x.Exists && !x.Readable))
    {
        string primary = Path.Combine(savesDir, s.Key + ".json");
        string bak = primary + ".bak";
        if (!File.Exists(bak)) { Console.WriteLine($"  {s.Key}: 无 .bak，跳过"); continue; }
        try
        {
            File.Copy(primary, primary + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true);
            File.Copy(bak, primary, true);
            var r = repo.Read(s.Key);
            Console.WriteLine($"  {s.Key}: 已用备份恢复，读到 天={r.World.Day}");
            fixedCount++;
        }
        catch (Exception ex) { Console.WriteLine($"  {s.Key}: 修复失败 {ex.Message}"); }
    }
    Console.WriteLine($"修复 {fixedCount} 个槽位");
}

if (command == "backup")
{
    string zip = Path.Combine(baseDir, "saves-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip");
    if (File.Exists(zip)) File.Delete(zip);
    ZipFile.CreateFromDirectory(savesDir, zip);
    Console.WriteLine("已打包备份: " + zip + "  (" + new FileInfo(zip).Length / 1024 + " KB)");
}

return bad == 0 ? 0 : 1;
