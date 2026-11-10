using BubbleYears.Simulation;

int failures = 0;
void Check(bool ok, string what)
{
    Console.WriteLine((ok ? "[PASS] " : "[FAIL] ") + what);
    if (!ok) failures++;
}

// 1) 特质选择：选走全部正面特质，应始终合法
var positives = StudioCatalog.Traits.Where(t => t.Positive).Select(t => t.Id).ToArray();
var sel = GameSession.CheckHeroTraits(positives);
Console.WriteLine($"正面特质总数={positives.Length}, Valid={sel.Valid}, Remaining={sel.Remaining}, Positive={sel.Positive}");
Check(sel.Valid, $"选择 {positives.Length} 个正面特质仍然合法（不再限制 3 个）");

// 2) 初始能力突破 60 上限且总和 != 210
var abilities = new[] { 999, 999, 999, 999, 999, 999 };
GameSession game = null;
try
{
    game = GameSession.NewCareer(123456789UL, "测试主角", new Employee(), abilities, positives);
    Check(true, "NewCareer 接受总和 5994 的能力值（原版会抛异常）");
}
catch (Exception ex)
{
    Check(false, "NewCareer 仍然拒绝越界能力：" + ex.GetType().Name + " " + ex.Message);
}

if (game != null)
{
    var hero = game.State.Career.Hero;
    Console.WriteLine($"主角能力={string.Join(",", hero.Abilities)}  特质数={hero.Traits.Count}");
    Check(hero.Abilities.All(a => a == 999), "越界能力被原样保存（未被夹到 60）");
    Check(hero.Traits.Count == positives.Length, "全部正面特质被保留");

    // 3) 编解码往返
    string json = SaveCodec.Write(game.State);
    var back = SaveCodec.Read(json);
    Check(back.Day == game.State.Day && back.Career.Hero.Abilities.SequenceEqual(abilities), "SaveCodec 往返后世界与能力值一致");

    // 4) 存档仓库（文件层）往返
    string dir = Path.Combine(Path.GetTempPath(), "by_save_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dir);
    var repo = new SaveRepository(dir);
    var env = new SaveEnvelope { Page = 2, DayProgress = 12.5, Speed = 5, Name = "验证 / 2026-01-01", Day = game.State.Day, Company = "验证公司" };
    repo.Write("manual-1", env, game.State);
    var read = repo.Read("manual-1");
    Check(read.Envelope.DayProgress == 12.5 && read.Envelope.Page == 2, "SaveRepository 写入并读回成功（手动槽）");
    var slots = repo.List();
    Console.WriteLine("槽位：" + string.Join(", ", slots.Select(s => s.Key + (s.Exists ? "*" : ""))));
    Check(slots.Any(s => s.Key == "manual-1"), "存档列表能看到 manual-1");
    Check(repo.LatestReadableKey() != null, "LatestReadableKey 能定位最新档");
    Console.WriteLine("存档目录：" + dir);
}

Console.WriteLine(failures == 0 ? "== 全部验证通过 ==" : $"== 有 {failures} 项失败 ==");
return failures == 0 ? 0 : 1;
