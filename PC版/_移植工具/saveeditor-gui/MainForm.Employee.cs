using System.Windows.Forms;
using BubbleYears.Simulation;

namespace SaveEditorGui;

public partial class MainForm
{
    private readonly List<string> _traitIds = new();

    private void BuildTraitList()
    {
        clbTraits.Items.Clear();
        _traitIds.Clear();
        foreach (var t in StudioCatalog.Traits.OrderByDescending(t => t.Positive).ThenBy(t => t.Cost))
        {
            string mark = t.Positive ? "＋" : "－";
            clbTraits.Items.Add($"{mark}{t.Name}（{(t.Positive ? "-" : "+")}{t.Cost} 点）");
            _traitIds.Add(t.Id);
        }
        WireEmployeeEvents();
    }

    private void WireEmployeeEvents()
    {
        lvPeople.SelectedIndexChanged += (_, _) =>
        {
            if (_binding) return;
            if (lvPeople.SelectedItems.Count == 0) { _sel = null; ClearEmployeeEditor(); return; }
            if (lvPeople.SelectedItems[0].Tag is Employee e) BindEmployee(e);
        };

        txtName.TextChanged += (_, _) =>
        {
            if (_binding || _sel == null) return;
            _sel.Name = txtName.Text;
            _dirty = true;
            RefreshSelectedRow();
        };

        for (int i = 0; i < 6; i++)
        {
            int idx = i;
            nums[idx].ValueChanged += (_, _) =>
            {
                if (_binding || _sel == null) return;
                var ab = _sel.Abilities ?? new int[6];
                if (ab.Length < 6) { var t = new int[6]; Array.Copy(ab, t, Math.Min(6, ab.Length)); ab = t; }
                ab[idx] = (int)nums[idx].Value;
                _sel.Abilities = ab;
                if (idx == 3) _sel.Design = ab[3];
                if (idx == 5) _sel.Engineering = ab[5];
                _dirty = true;
                RefreshSelectedRow();
            };
        }

        numSati.ValueChanged += (_, _) =>
        {
            if (_binding || _sel == null) return;
            _sel.Satisfaction = Math.Clamp((double)numSati.Value, 0, 100);
            _dirty = true;
            RefreshSelectedRow();
        };

        numSalary.ValueChanged += (_, _) =>
        {
            if (_binding || _sel == null) return;
            _sel.MonthlySalary = numSalary.Value;
            _dirty = true;
        };

        clbTraits.ItemCheck += (_, e) =>
        {
            if (_binding || _sel == null) return;
            if (e.Index < 0 || e.Index >= _traitIds.Count) return;
            string id = _traitIds[e.Index];
            _sel.Traits ??= new List<string>();
            bool want = e.NewValue == CheckState.Checked;
            bool has = _sel.Traits.Contains(id);
            if (want && !has) _sel.Traits.Add(id);
            else if (!want && has) _sel.Traits.Remove(id);
            _dirty = true;
        };

        btnAbilityAll.Click += (_, _) =>
        {
            if (_sel == null) { MessageBox.Show("请先选择一个员工作为模板"); return; }
            var src = _sel.Abilities ?? new int[6];
            foreach (var e in _people)
            {
                var ab = new int[6];
                Array.Copy(src, ab, Math.Min(6, src.Length));
                e.Abilities = ab;
                e.Design = ab[3];
                e.Engineering = ab[5];
            }
            _dirty = true;
            BuildPeopleList();
            MessageBox.Show($"已把当前六项能力套用到 {_people.Count} 名员工");
        };

        btnSatiAll.Click += (_, _) =>
        {
            if (_sel == null) { MessageBox.Show("请先选择一个员工作为模板"); return; }
            double v = Math.Clamp((double)numSati.Value, 0, 100);
            foreach (var e in _people) e.Satisfaction = v;
            _dirty = true;
            BuildPeopleList();
            MessageBox.Show($"已把满意度 {v:F0} 套用到 {_people.Count} 名员工");
        };
    }

    private void BindEmployee(Employee e)
    {
        _sel = e;
        _binding = true;
        lblEmpInfo.Text = $"Id {e.Id} ｜ 公司 {e.Company} ｜ 岗位 {e.Role} ｜ 满意度 {(e.Satisfaction ?? 0):F0}";
        txtName.Text = e.Name ?? "";

        var ab = e.Abilities ?? new[] { e.Design, 0, 0, 0, 0, 0, e.Engineering };
        for (int i = 0; i < 6; i++)
            nums[i].Value = Math.Clamp(i < ab.Length ? ab[i] : 0, 0, 999);

        numSati.Value = (decimal)Math.Clamp(e.Satisfaction ?? 0, 0, 100);
        numSalary.Value = Math.Clamp(e.MonthlySalary ?? 0, 0, 100_000_000);

        var traits = e.Traits ?? new List<string>();
        for (int i = 0; i < _traitIds.Count && i < clbTraits.Items.Count; i++)
            clbTraits.SetItemChecked(i, traits.Contains(_traitIds[i]));
        _binding = false;
    }

    private void RefreshSelectedRow()
    {
        if (lvPeople.SelectedItems.Count == 0) return;
        var it = lvPeople.SelectedItems[0];
        var e = (Employee)it.Tag;
        var ab = e.Abilities ?? new[] { e.Design, 0, 0, 0, 0, 0, e.Engineering };
        it.Text = e.Name ?? "";
        it.SubItems[3].Text = (e.Satisfaction ?? 0).ToString("F0");
        for (int i = 0; i < 6; i++) it.SubItems[4 + i].Text = i < ab.Length ? ab[i].ToString() : "0";
        lblEmpInfo.Text = $"Id {e.Id} ｜ 公司 {e.Company} ｜ 岗位 {e.Role} ｜ 满意度 {(e.Satisfaction ?? 0):F0}";
    }
}
