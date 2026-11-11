using BubbleYears.Simulation;

namespace SaveEditorGui;

/// <summary>无界面自检：读取 → 改一个员工 → 保存 → 重新读取验证。</summary>
internal static class SelfTest
{
    public static int Run(string dir, string slot)
    {
        string savesDir = dir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Godot", "app_userdata", "泡沫年代 - Bubble Years", "saves");

        Console.WriteLine("存档目录: " + savesDir);
        var repo = new SaveRepository(savesDir);
        var info = repo.List().FirstOrDefault(s => s.Key == slot);
        if (info == null || !info.Exists || !info.Readable)
        {
            Console.WriteLine($"槽位 {slot} 不存在或不可读");
            return 3;
        }

        var read = repo.Read(slot);
        var world = read.World;
        var firm = world.Firms[0];
        Console.WriteLine($"[读取] 天={world.Day} 公司={firm.Name} 现金={world.Accounts[firm.Account]:N0} 员工={world.Workforce?.People?.Count ?? 0}");

        var emp = world.Workforce?.People?.FirstOrDefault() ?? world.Career?.Hero;
        if (emp == null) { Console.WriteLine("没有员工可测"); return 4; }

        string oldName = emp.Name;
        emp.Name = "GUI自检_" + DateTime.Now.ToString("HHmmss");
        emp.Abilities ??= new int[6];
        if (emp.Abilities.Length >= 6) emp.Abilities[0] = 123;

        var traits = StudioCatalog.Traits;
        Console.WriteLine($"[特质目录] 共 {traits.Length} 个，示例: {string.Join("、", traits.Take(3).Select(t => $"{t.Name}{(t.Positive ? "+" : "-")}{t.Cost}"))}");
        var firstPositive = traits.FirstOrDefault(t => t.Positive);
        if (firstPositive != null)
        {
            emp.Traits ??= new List<string>();
            if (!emp.Traits.Contains(firstPositive.Id)) emp.Traits.Add(firstPositive.Id);
            Console.WriteLine($"[特质] 给 {emp.Name} 增加了 {firstPositive.Name}");
        }

        decimal cash = world.Accounts[firm.Account];
        world.Accounts[firm.Account] = cash;

        try
        {
            repo.Write(slot, read.Envelope, world);
            Console.WriteLine("[保存] 成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[保存] 失败: " + ex.GetType().Name + ": " + ex.Message);
            return 5;
        }

        var back = repo.Read(slot);
        var b = back.World.Workforce?.People?.FirstOrDefault() ?? back.World.Career?.Hero;
        bool ok = b != null && b.Name == emp.Name && b.Abilities != null && b.Abilities[0] == 123
                  && (firstPositive == null || (b.Traits ?? new List<string>()).Contains(firstPositive.Id));
        Console.WriteLine($"[回读] 姓名={b?.Name} 智力={b?.Abilities?[0]} 特质数={b?.Traits?.Count ?? 0} => {(ok ? "一致 ✅" : "不一致 ❌")}");

        // 还原姓名，避免测试污染
        b.Name = oldName;
        if (b.Abilities != null) b.Abilities[0] = emp.Abilities[0];
        return ok ? 0 : 6;
    }
}
