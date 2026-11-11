using System.Text;

namespace SaveEditorGui;

internal static class Program
{
    internal static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Godot", "app_userdata", "泡沫年代 - Bubble Years", "logs", "editor.log");

    internal static void Log(string text)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        }
        catch { }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => { Log("UI 异常: " + e.Exception); MessageBox.Show(e.Exception.ToString(), "界面异常"); };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("未处理异常: " + e.ExceptionObject);

        try
        {
            if (args.Length > 0 && args[0] == "--selftest")
            {
                Environment.Exit(SelfTest.Run(args.Length > 1 ? args[1] : null, args.Length > 2 ? args[2] : "manual-1"));
                return;
            }

            if (args.Length > 0 && args[0] == "--uicheck")
            {
                ApplicationConfiguration.Initialize();
                var f = new MainForm();
                f.Show();
                Application.DoEvents();
                string report = f.UiCheck(args.Length > 1 ? args[1] : "manual-1");
                Log("UI 自检:\n" + report);
                Console.WriteLine(report);
                f.Close();
                Environment.Exit(0);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log("启动失败: " + ex);
            MessageBox.Show("启动失败：\n" + ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
