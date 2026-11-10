using Godot;

namespace BubbleYears.BuildShell;

/// <summary>
/// 仅用于让 Godot.NET.Sdk 生成一个合法的程序集骨架；真正的游戏程序集在构建后由
/// CopyPrebuiltAssemblies 目标覆盖为 assemblies/patched 下的预编译版本。
/// </summary>
public partial class ShellMarker : RefCounted
{
}
