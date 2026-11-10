using Mono.Cecil;
using Mono.Cecil.Cil;

string mode = args.Length > 0 ? args[0] : "dump";
string root = @"D:\portwork\game\assemblies";
string uiPath = Path.Combine(root, "patched", "BubbleYears.dll");
string simPath = Path.Combine(root, "patched", "BubbleYears.Simulation.dll");

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(uiPath)!);
resolver.AddSearchDirectory(Path.Combine(root, "original"));
resolver.AddSearchDirectory(Path.Combine(root, "desktop"));
resolver.AddSearchDirectory(@"D:\portwork\game\.godot\mono\temp\bin\Debug");
resolver.AddSearchDirectory(@"D:\portwork\godot\Godot_v4.7.2-stable_mono_win64\GodotSharp\Tools\GodotSharp");

static void DumpMethod(MethodDefinition m, int max = 200)
{
    Console.WriteLine($"### {m.FullName}");
    if (!m.HasBody) { Console.WriteLine("  <no body>"); return; }
    int i = 0;
    foreach (var ins in m.Body.Instructions)
    {
        if (i++ > max) { Console.WriteLine("  ..."); break; }
        Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
    }
}

static string Format(object? op) => op switch
{
    null => "",
    string s => "\"" + s + "\"",
    Instruction t => "IL_" + t.Offset.ToString("X4"),
    Instruction[] ts => string.Join(",", ts.Select(t => "IL_" + t.Offset.ToString("X4"))),
    MethodReference mr => mr.FullName,
    FieldReference fr => fr.FullName,
    TypeReference tr => tr.FullName,
    _ => op.ToString() ?? ""
};

void DumpContaining(string dllPath, string typeName, string methodNameContains, int max = 160)
{
    var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolver });
    foreach (var t in asm.MainModule.Types.SelectMany(Flatten))
    {
        if (t.FullName != typeName) continue;
        foreach (var m in t.Methods.Where(m => m.Name.Contains(methodNameContains)))
            DumpMethod(m, max);
    }
}

static IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
{
    yield return t;
    foreach (var n in t.NestedTypes) foreach (var x in Flatten(n)) yield return x;
}

if (mode == "dump")
{
    Console.WriteLine("== Search: SpinBox MaxValue 60 in UI ==");
    var ui = AssemblyDefinition.ReadAssembly(uiPath, new ReaderParameters { AssemblyResolver = resolver });
    foreach (var t in ui.MainModule.Types.SelectMany(Flatten))
    {
        foreach (var m in t.Methods)
        {
            if (!m.HasBody) continue;
            var ins = m.Body.Instructions;
            for (int k = 1; k < ins.Count; k++)
            {
                if (ins[k].OpCode == OpCodes.Callvirt &&
                    ins[k].Operand is MethodReference mr && mr.Name == "set_MaxValue" &&
                    ins[k - 1].OpCode == OpCodes.Ldc_R8 &&
                    Convert.ToDouble(ins[k - 1].Operand) == 60.0)
                {
                    Console.WriteLine($"MAX60: {t.FullName}::{m.Name} at IL_{ins[k - 1].Offset:X4}");
                }
            }
        }
    }
    Console.WriteLine("== Search: Enumerable.Sum near ldc.i4 210 in UI ==");
    foreach (var t in ui.MainModule.Types.SelectMany(Flatten))
    {
        foreach (var m in t.Methods)
        {
            if (!m.HasBody) continue;
            var ins = m.Body.Instructions;
            for (int k = 0; k < ins.Count; k++)
            {
                if (ins[k].OpCode == OpCodes.Call && ins[k].Operand is MethodReference mr &&
                    mr.DeclaringType.Name == "Enumerable" && mr.Name == "Sum")
                {
                    for (int j = k + 1; j < Math.Min(ins.Count, k + 6); j++)
                    {
                        if (ins[j].OpCode == OpCodes.Ldc_I4 && Convert.ToInt32(ins[j].Operand) == 210)
                            Console.WriteLine($"SUM210: {t.FullName}::{m.Name} sum@IL_{ins[k].Offset:X4} ldc210@IL_{ins[j].Offset:X4}");
                        if (ins[j].OpCode == OpCodes.Ldc_I4_S && Convert.ToInt32(ins[j].Operand) == 210)
                            Console.WriteLine($"SUM210s: {t.FullName}::{m.Name} sum@IL_{ins[k].Offset:X4}");
                    }
                }
            }
        }
    }
    Console.WriteLine();
    DumpContaining(simPath, "BubbleYears.Simulation.GameSession", "CheckHeroTraits", 80);
    Console.WriteLine();
    DumpContaining(simPath, "BubbleYears.Simulation.GameSession", "NewCareer", 90);
    Console.WriteLine();
    Console.WriteLine("== Search: Enumerable.Sum near ldc 210 in SIM ==");
    var sim = AssemblyDefinition.ReadAssembly(simPath, new ReaderParameters { AssemblyResolver = resolver });
    foreach (var t in sim.MainModule.Types.SelectMany(Flatten))
    {
        foreach (var m in t.Methods)
        {
            if (!m.HasBody) continue;
            var ins = m.Body.Instructions;
            for (int k = 0; k < ins.Count; k++)
            {
                if (ins[k].OpCode == OpCodes.Call && ins[k].Operand is MethodReference mr &&
                    mr.DeclaringType.Name == "Enumerable" && mr.Name == "Sum")
                {
                    for (int j = k + 1; j < Math.Min(ins.Count, k + 6); j++)
                    {
                        if ((ins[j].OpCode == OpCodes.Ldc_I4 && Convert.ToInt32(ins[j].Operand) == 210) ||
                            (ins[j].OpCode == OpCodes.Ldc_I4_S && Convert.ToInt32(ins[j].Operand) == 210))
                            Console.WriteLine($"SUM210: {t.FullName}::{m.Name} sum@IL_{ins[k].Offset:X4} ldc@IL_{ins[j].Offset:X4}");
                    }
                }
            }
        }
    }
}

if (mode == "dump2")
{
    var sim = AssemblyDefinition.ReadAssembly(simPath, new ReaderParameters { AssemblyResolver = resolver });
    var gs = sim.MainModule.Types.SelectMany(Flatten).First(t => t.FullName == "BubbleYears.Simulation.GameSession");
    var nc = gs.Methods.First(m => m.Name == "NewCareer");
    Console.WriteLine($"### NewCareer");
    foreach (var ins in nc.Body.Instructions)
        if (ins.Offset >= 0x40 && ins.Offset <= 0x100)
            Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
    Console.WriteLine();
    var ui = AssemblyDefinition.ReadAssembly(uiPath, new ReaderParameters { AssemblyResolver = resolver });
    var moveNext = ui.MainModule.Types.SelectMany(Flatten)
        .First(t => t.FullName.Contains("<<ShowNewCampaign>b__13>d")).Methods.First(m => m.Name == "MoveNext");
    Console.WriteLine($"### {moveNext.FullName}");
    foreach (var ins in moveNext.Body.Instructions)
        if (ins.Offset >= 0x70 && ins.Offset <= 0xC0)
            Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
    Console.WriteLine();
    var draw = ui.MainModule.Types.SelectMany(Flatten).First(t => t.Name == "RetroGameController")
        .Methods.First(m => m.Name == "DrawCareerCreation");
    Console.WriteLine($"### {draw.FullName}");
    foreach (var ins in draw.Body.Instructions)
        if (ins.Offset >= 0x390 && ins.Offset <= 0x3E0)
            Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
    Console.WriteLine();
    var check = gs.Methods.First(m => m.Name == "CheckHeroTraits");
    Console.WriteLine($"### {check.FullName} (tail)");
    foreach (var ins in check.Body.Instructions)
        if (ins.Offset >= 0xB0)
            Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
}
if (mode == "dump3")
{
    var sim = AssemblyDefinition.ReadAssembly(simPath, new ReaderParameters { AssemblyResolver = resolver });
    var gs = sim.MainModule.Types.SelectMany(Flatten).First(t => t.FullName == "BubbleYears.Simulation.GameSession");
    var nc = gs.Methods.First(m => m.Name == "NewCareer");
    foreach (var ins in nc.Body.Instructions)
        if (ins.Offset >= 0x40 && ins.Offset <= 0x9A)
            Console.WriteLine($"  IL_{ins.Offset:X4}: {ins.OpCode,-12} {Format(ins.Operand)}");
}

if (mode == "apply")
{
    static AssemblyDefinition LoadUnlocked(string path, IAssemblyResolver res)
    {
        string tmp = Path.Combine(Path.GetTempPath(), "py_" + Guid.NewGuid().ToString("N") + ".dll");
        File.Copy(path, tmp, true);
        return AssemblyDefinition.ReadAssembly(tmp, new ReaderParameters { AssemblyResolver = res });
    }

    Console.WriteLine("=== 补丁 1/4：Simulation.NewCareer 跳过初始能力/特质校验 ===");
    var sim = LoadUnlocked(simPath, resolver);
    var gs = sim.MainModule.Types.SelectMany(Flatten).First(t => t.FullName == "BubbleYears.Simulation.GameSession");
    var nc = gs.Methods.First(m => m.Name == "NewCareer");
    var il = nc.Body.GetILProcessor();
    Instruction? throwInstr = null;
    foreach (var ins in nc.Body.Instructions)
        if (ins.OpCode == OpCodes.Throw) { throwInstr = ins; break; }
    if (throwInstr == null) throw new Exception("NewCareer: 未找到 throw");
    var afterThrow = throwInstr.Next;
    // throw 前是 newobj ArgumentException，其前是 ldstr 消息
    var ldstrMsg = throwInstr.Previous!.Previous!;
    if (ldstrMsg.OpCode != OpCodes.Ldstr) throw new Exception("NewCareer: 结构不符合预期");
    Console.WriteLine($"  将以 br 覆盖：IL_{ldstrMsg.Offset:X4} ldstr \"{ldstrMsg.Operand}\" -> IL_{afterThrow.Offset:X4}");
    ldstrMsg.OpCode = OpCodes.Br;
    ldstrMsg.Operand = afterThrow;

    Console.WriteLine("=== 补丁 2/4：CheckHeroTraits 强制有效 + 正面特质计数归零（解除 3 个上限与点数预算） ===");
    var check = gs.Methods.First(m => m.Name == "CheckHeroTraits");
    var checkIl = check.Body.GetILProcessor();
    var validStart = check.Body.Instructions.First(i => i.Offset == 0xC5);
    // 0xC5..0xD2 是 valid 的计算，改成恒 true
    var cur = validStart;
    validStart.OpCode = OpCodes.Ldc_I4_1;
    validStart.Operand = null;
    cur = validStart.Next!;
    while (true)
    {
        var next = cur!.Next;
        bool isTarget = cur.Offset == 0xD2;
        cur.OpCode = OpCodes.Nop;
        cur.Operand = null;
        if (isTarget) break;
        cur = next;
    }
    Console.WriteLine($"  已置 valid=true；并将 Positive 参数改为 0");
    var posLoad = check.Body.Instructions.First(i => i.Offset == 0xD4);
    posLoad.OpCode = OpCodes.Ldc_I4_0;
    posLoad.Operand = null;
    sim.Write(simPath + ".new");
    File.Move(simPath + ".new", simPath, true);
    Console.WriteLine("  -> Simulation 已写回");

    Console.WriteLine("=== 补丁 3/4：UI 六个能力 SpinBox 上限 60 -> 999 ===");
    var ui = LoadUnlocked(uiPath, resolver);
    var controller = ui.MainModule.Types.SelectMany(Flatten).First(t => t.FullName == "BubbleYears.RetroGameController");
    var draw = controller.Methods.First(m => m.Name == "DrawCareerCreation");
    int maxPatched = 0;
    var drawIl = draw.Body.GetILProcessor();
    foreach (var ins in draw.Body.Instructions)
    {
        if (ins.OpCode == OpCodes.Callvirt && ins.Operand is MethodReference mr && mr.Name == "set_MaxValue"
            && ins.Previous!.OpCode == OpCodes.Ldc_R8 && Convert.ToDouble(ins.Previous.Operand) == 60.0)
        {
            ins.Previous.Operand = 999.0;
            maxPatched++;
        }
    }
    Console.WriteLine($"  已修改 {maxPatched} 处 MaxValue");

    Console.WriteLine("=== 补丁 4/4：UI 开局校验去掉“必须分配完 210 点” ===");
    var moveNext = ui.MainModule.Types.SelectMany(Flatten)
        .First(t => t.FullName.Contains("<<ShowNewCampaign>b__13>d")).Methods.First(m => m.Name == "MoveNext");
    var mnIl = moveNext.Body.GetILProcessor();
    int sumPatched = 0;
    foreach (var ins in moveNext.Body.Instructions.ToList())
    {
        if (ins.OpCode == OpCodes.Call && ins.Operand is MethodReference mr &&
            mr.DeclaringType.Name == "Enumerable" && mr.Name == "Sum")
        {
            // 确认后面紧跟 ldc.i4 210
            var n1 = ins.Next;
            var n2 = n1?.Next;
            var n3 = n2?.Next;
            bool near210 = (n1 != null && IsIntConst(n1, 210)) || (n2 != null && IsIntConst(n2, 210)) || (n3 != null && IsIntConst(n3, 210));
            if (!near210) continue;
            ins.OpCode = OpCodes.Pop;
            ins.Operand = null;
            mnIl.InsertAfter(ins, Instruction.Create(OpCodes.Ldc_I4, 210));
            sumPatched++;
        }
    }
    Console.WriteLine($"  已替换 {sumPatched} 处 Sum 调用为常量 210");

    // 顺手把“最多3个”的提示文案改成解锁说明
    int labelPatched = 0;
    foreach (var ins in draw.Body.Instructions)
    {
        if (ins.OpCode == OpCodes.Ldstr && ins.Operand is string s && s.Contains("正面特质最多3个"))
        {
            ins.Operand = "正面特质数量不限，负面特质返还点数。悬停查看效果。";
            labelPatched++;
        }
    }
    Console.WriteLine($"  已更新提示文案 {labelPatched} 处");
    ui.Write(uiPath + ".new");
    File.Move(uiPath + ".new", uiPath, true);
    Console.WriteLine("  -> UI 已写回");
    Console.WriteLine("全部补丁完成。");
}

static bool IsIntConst(Instruction i, int v)
{
    if (i.OpCode == OpCodes.Ldc_I4) return Convert.ToInt32(i.Operand) == v;
    if (i.OpCode == OpCodes.Ldc_I4_S) return Convert.ToInt32(i.Operand) == v;
    if (i.OpCode == OpCodes.Ldc_I4_0) return v == 0;
    if (i.OpCode == OpCodes.Ldc_I4_1) return v == 1;
    if (i.OpCode == OpCodes.Ldc_I4_2) return v == 2;
    if (i.OpCode == OpCodes.Ldc_I4_3) return v == 3;
    return false;
}

