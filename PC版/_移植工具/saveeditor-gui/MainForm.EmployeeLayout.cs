using System.Drawing;
using System.Windows.Forms;

namespace SaveEditorGui;

public partial class MainForm
{
    private TabPage BuildEmployeeTab()
    {
        var page = new TabPage("员工");
        split = new SplitContainer { Dock = DockStyle.Fill };

        lvPeople = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false
        };
        lvPeople.Columns.Add("姓名", 130);
        lvPeople.Columns.Add("公司", 55, HorizontalAlignment.Right);
        lvPeople.Columns.Add("岗位", 88);
        lvPeople.Columns.Add("满意度", 60, HorizontalAlignment.Right);
        foreach (var n in AbilityNames) lvPeople.Columns.Add(n, 46, HorizontalAlignment.Right);

        var leftBox = new GroupBox { Text = "员工列表（点击选择；含主角）", Dock = DockStyle.Fill, Padding = new Padding(6) };
        leftBox.Controls.Add(lvPeople);
        split.Panel1.Controls.Add(leftBox);

        var right = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        lblEmpInfo = new Label { Text = "未选择员工", AutoSize = true, Location = new Point(8, 6), ForeColor = Color.DimGray };
        right.Controls.Add(lblEmpInfo);
        right.Controls.Add(new Label { Text = "姓名", Location = new Point(8, 34), AutoSize = true });
        txtName = new TextBox { Location = new Point(58, 31), Width = 220 };
        right.Controls.Add(txtName);

        for (int i = 0; i < 6; i++)
        {
            int x = 8 + (i % 3) * 150;
            int y = 70 + (i / 3) * 42;
            right.Controls.Add(new Label { Text = AbilityNames[i], Location = new Point(x, y + 3), AutoSize = true });
            nums[i] = MakeNum(0, 999, 50);
            nums[i].Location = new Point(x + 44, y);
            nums[i].Width = 90;
            right.Controls.Add(nums[i]);
        }

        right.Controls.Add(new Label { Text = "满意度", Location = new Point(8, 160), AutoSize = true });
        numSati = new NumericUpDown
        {
            Location = new Point(58, 157),
            Width = 90,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 1
        };
        chkSatiLock = new CheckBox
        {
            Text = "满意度已锁定（不再随时间变化）",
            Location = new Point(158, 158),
            AutoSize = true,
            Checked = true,
            Enabled = false
        };
        right.Controls.Add(numSati);
        right.Controls.Add(chkSatiLock);

        right.Controls.Add(new Label { Text = "月薪", Location = new Point(8, 198), AutoSize = true });
        numSalary = MakeNum(0, 100_000_000, 4500);
        numSalary.Location = new Point(58, 195);
        numSalary.Width = 120;
        right.Controls.Add(numSalary);

        right.Controls.Add(new Label { Text = "特质（勾选 = 拥有）", Location = new Point(8, 234), AutoSize = true });
        clbTraits = new CheckedListBox
        {
            Location = new Point(8, 258),
            Width = 440,
            Height = 250,
            CheckOnClick = true
        };
        right.Controls.Add(clbTraits);

        btnAbilityAll = new Button { Text = "六项能力 → 全员", Location = new Point(8, 520), Width = 150 };
        btnSatiAll = new Button { Text = "满意度 → 全员", Location = new Point(166, 520), Width = 150 };
        right.Controls.Add(btnAbilityAll);
        right.Controls.Add(btnSatiAll);

        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }
}
