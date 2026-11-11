using System.Text;
using System.Windows.Forms;
using BubbleYears.Simulation;

namespace SaveEditorGui;

public partial class MainForm : Form
{
    private readonly string _savesDir;
    private SaveRepository _repo;
    private string _slot;
    private SaveEnvelope _env;
    private WorldState _world;
    private Firm _firm;
    private List<Employee> _people = new();
    private Employee _sel;
    private bool _binding;
    private bool _dirty;

    public MainForm()
    {
        _savesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Godot", "app_userdata", "泡沫年代 - Bubble Years", "saves");

        BuildUi();
        BuildTraitList();
        WireEvents();
        LoadSlotList();

        // 启动后自动读取第一个可用槽位（省得手动点）
        Shown += async (_, _) =>
        {
            try
            {
                if (cbSlots.Items.Count > 0 && _world == null) await LoadSelectedSlot();
            }
            catch (Exception ex) { MessageBox.Show("自动读取失败：\n" + ex.Message); }
        };
    }

    private void WireEvents()
    {
        btnRefresh.Click += (_, _) => LoadSlotList();
        btnLoad.Click += async (_, _) => await LoadSelectedSlot();
        btnSave.Click += async (_, _) => await SaveSlot();

        numDay.ValueChanged += (_, _) => { if (!_binding && _world != null) _world.Day = (int)numDay.Value; };
        txtCompany.TextChanged += (_, _) => { if (!_binding && _firm != null) _firm.Name = txtCompany.Text; };
        numCompanyCash.ValueChanged += (_, _) => { if (!_binding && _firm != null) _world.Accounts[_firm.Account] = numCompanyCash.Value; };
        numPersonalCash.ValueChanged += (_, _) => { if (!_binding && _world != null) _world.Accounts["personal"] = numPersonalCash.Value; };
        numCompanyDebt.ValueChanged += (_, _) => { if (!_binding && _firm != null) _firm.Debt = numCompanyDebt.Value; };
        numPersonalDebt.ValueChanged += (_, _) => { if (!_binding && _world != null) _world.PersonalDebt = numPersonalDebt.Value; };
    }

    private void LoadSlotList()
    {
        try
        {
            Directory.CreateDirectory(_savesDir);
            _repo = new SaveRepository(_savesDir);
            var slots = _repo.List();
            cbSlots.Items.Clear();
            foreach (var s in slots)
                cbSlots.Items.Add($"{s.Key}   {(s.Exists ? s.Company : "空槽")}   第{s.Day}天");
            if (cbSlots.Items.Count > 0) cbSlots.SelectedIndex = 0;
            lblStatus.Text = $"存档目录：{_savesDir}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("读取槽位列表失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string SelectedKey()
    {
        var s = cbSlots.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(s)) return null;
        int sp = s.IndexOf("   ", StringComparison.Ordinal);
        return sp > 0 ? s[..sp] : s;
    }
}
