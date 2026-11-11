using System.Text;
using System.Windows.Forms;
using BubbleYears.Simulation;

namespace SaveEditorGui;

public partial class MainForm
{
    private async Task LoadSelectedSlot()
    {
        var key = SelectedKey();
        if (key == null) { MessageBox.Show("请先选择槽位"); return; }

        btnLoad.Enabled = false; btnSave.Enabled = false;
        lblStatus.Text = "正在读取（世界较大，可能需要几秒）…";
        try
        {
            var data = await Task.Run(() => _repo.Read(key));
            _slot = key;
            _env = data.Envelope;
            _world = data.World;
            _firm = _world.Firms[0];

            _binding = true;
            numDay.Value = Math.Clamp(_world.Day, 0, 100000);
            txtCompany.Text = _firm.Name ?? "";
            numCompanyCash.Value = Math.Clamp(_world.Accounts[_firm.Account], 0, 1_000_000_000_000_000m);
            numPersonalCash.Value = Math.Clamp(_world.Accounts.TryGetValue("personal", out var pc) ? pc : 0m, 0, 1_000_000_000_000_000m);
            numCompanyDebt.Value = Math.Clamp(_firm.Debt, 0, 1_000_000_000_000_000m);
            numPersonalDebt.Value = Math.Clamp(_world.PersonalDebt, 0, 1_000_000_000_000_000m);
            _binding = false;

            BuildPeopleList();
            _dirty = false;
            lblStatus.Text = $"已读取 {key}：第 {_world.Day} 天 / {_firm.Name} / 员工 {_people.Count} 人";
        }
        catch (Exception ex)
        {
            MessageBox.Show("读取失败：\n" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "读取失败";
        }
        finally
        {
            btnLoad.Enabled = true; btnSave.Enabled = true;
        }
    }

    private void BuildPeopleList()
    {
        _people = new List<Employee>();
        if (_world.Workforce?.People != null) _people.AddRange(_world.Workforce.People);
        var hero = _world.Career?.Hero;
        if (hero != null && !_people.Contains(hero)) _people.Add(hero);

        lvPeople.BeginUpdate();
        lvPeople.Items.Clear();
        foreach (var e in _people)
        {
            var ab = e.Abilities ?? new[] { e.Design, 0, 0, 0, 0, 0, e.Engineering };
            var it = new ListViewItem(e.Name ?? "") { Tag = e };
            it.SubItems.Add(e.Company.ToString());
            it.SubItems.Add(e.Role ?? "");
            it.SubItems.Add((e.Satisfaction ?? 0).ToString("F0"));
            for (int i = 0; i < 6; i++) it.SubItems.Add(i < ab.Length ? ab[i].ToString() : "0");
            lvPeople.Items.Add(it);
        }
        lvPeople.EndUpdate();
        _sel = null;
        ClearEmployeeEditor();
    }

    private async Task SaveSlot()
    {
        if (_world == null || _slot == null) { MessageBox.Show("请先读取一个槽位"); return; }
        if (MessageBox.Show($"确认把修改写入槽位 {_slot}？\n（会先自动备份到 saves\\.editor-backup-时间）",
                "确认保存", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return;

        btnSave.Enabled = false; btnLoad.Enabled = false;
        lblStatus.Text = "正在保存（141MB 世界需要几秒）…";
        try
        {
            string bk = Path.Combine(_savesDir, ".editor-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(bk);
            foreach (var f in Directory.GetFiles(_savesDir, _slot + ".json*"))
                File.Copy(f, Path.Combine(bk, Path.GetFileName(f)), true);

            await Task.Run(() => _repo.Write(_slot, _env, _world));
            _dirty = false;
            lblStatus.Text = $"已保存到 {_slot}（备份：{Path.GetFileName(bk)}）";
            MessageBox.Show("保存成功。\n进游戏后点「读取存档」选择该槽位即可生效。", "完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblStatus.Text = "保存失败";
            MessageBox.Show("保存失败：\n" + ex.Message + "\n\n（未写盘，原存档保持不变）", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSave.Enabled = true; btnLoad.Enabled = true;
        }
    }

    private void ClearEmployeeEditor()
    {
        _binding = true;
        lblEmpInfo.Text = "未选择员工";
        txtName.Text = "";
        foreach (var n in nums) n.Value = 0;
        numSati.Value = 0;
        numSalary.Value = 0;
        for (int i = 0; i < clbTraits.Items.Count; i++) clbTraits.SetItemChecked(i, false);
        _binding = false;
    }


    /// <summary>界面自检：读取槽位、填充界面，返回控件状态文本（用于定位“看不到员工信息”这类问题）。</summary>
    internal string UiCheck(string slot)
    {
        var sb = new StringBuilder();
        tabs.SelectedIndex = 1;
        sb.AppendLine($"窗体: {Width}x{Height}  选项卡={tabs.TabPages.Count}  当前页={tabs.SelectedIndex}");
        try
        {
            var data = _repo.Read(slot);
            _slot = slot;
            _env = data.Envelope;
            _world = data.World;
            _firm = _world.Firms[0];
            _binding = true;
            numDay.Value = Math.Clamp(_world.Day, 0, 100000);
            txtCompany.Text = _firm.Name ?? "";
            _binding = false;
            BuildPeopleList();
        }
        catch (Exception ex)
        {
            sb.AppendLine("读取失败: " + ex.GetType().Name + ": " + ex.Message);
            return sb.ToString();
        }

        Application.DoEvents();
        sb.AppendLine($"槽位={_slot}  天数={_world.Day}  公司={_firm.Name}  员工数={_people.Count}");
        sb.AppendLine($"员工页: split={split.Width}x{split.Height}  splitter={split.SplitterDistance}  panel1={split.Panel1.Width}  panel2={split.Panel2.Width}");
        sb.AppendLine($"列表: items={lvPeople.Items.Count}  visible={lvPeople.Visible}  size={lvPeople.Width}x{lvPeople.Height}  columns={lvPeople.Columns.Count}");
        sb.AppendLine($"右栏: 姓名框={txtName.Visible}/{txtName.Bounds}  满意度={numSati.Visible}/{numSati.Bounds}  特质项={clbTraits.Items.Count}");

        if (lvPeople.Items.Count > 0)
        {
            lvPeople.Items[0].Selected = true;
            lvPeople.Select();
            BindEmployee((Employee)lvPeople.Items[0].Tag);
            Application.DoEvents();
            sb.AppendLine($"选中第1人: 姓名={txtName.Text}  智力={nums[0].Value}  满意度={numSati.Value}  特质勾选={clbTraits.CheckedItems.Count}");
        }
        sb.AppendLine($"提示条: {lblStatus.Text}");
        return sb.ToString();
    }
}
