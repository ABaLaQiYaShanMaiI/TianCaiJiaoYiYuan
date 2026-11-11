using System.Drawing;
using System.Windows.Forms;

namespace SaveEditorGui;

public partial class MainForm
{
    private TabControl tabs;
    private ComboBox cbSlots;
    private Button btnRefresh, btnLoad, btnSave;
    private Label lblStatus;

    private NumericUpDown numDay, numCompanyCash, numPersonalCash, numCompanyDebt, numPersonalDebt;
    private TextBox txtCompany;

    private SplitContainer split;
    private ListView lvPeople;
    private TextBox txtName;
    private NumericUpDown[] nums = new NumericUpDown[6];
    private NumericUpDown numSati, numSalary;
    private CheckBox chkSatiLock;
    private CheckedListBox clbTraits;
    private Button btnSatiAll, btnAbilityAll;
    private Label lblEmpInfo;

    private static readonly string[] AbilityNames = { "智力", "魅力", "口才", "美术", "音乐", "技术" };

    private void BuildUi()
    {
        Text = "天才交易员 · 存档修改器（v1.0.3）";
        Width = 1180;
        Height = 780;
        MinimumSize = new Size(1020, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        var top = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8, 8, 8, 4) };
        top.Controls.Add(new Label { Text = "存档槽位", AutoSize = true, Location = new Point(10, 15) });
        cbSlots = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(76, 11), Width = 150 };
        btnRefresh = new Button { Text = "刷新", Location = new Point(236, 10), Width = 64 };
        btnLoad = new Button { Text = "读取", Location = new Point(306, 10), Width = 64 };
        btnSave = new Button { Text = "保存到该槽位", Location = new Point(376, 10), Width = 120, BackColor = Color.FromArgb(200, 235, 210) };
        lblStatus = new Label { Text = "选择槽位后点“读取”", AutoSize = true, Location = new Point(510, 15), ForeColor = Color.DimGray };
        top.Controls.AddRange(new Control[] { cbSlots, btnRefresh, btnLoad, btnSave, lblStatus });

        tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildBasicTab());
        tabs.TabPages.Add(BuildEmployeeTab());
        tabs.SelectedIndex = 1;   // 默认停在“员工”页（修改器重点）
        Controls.Add(tabs);
        Controls.Add(top);
        top.BringToFront();

        // 分割条位置必须等窗体有真实尺寸后再设，否则会抛异常
        Shown += (_, _) =>
        {
            try
            {
                if (split != null && split.Width > 700)
                    split.SplitterDistance = Math.Min(660, split.Width - 420);
            }
            catch { }
        };
    }

    private TabPage BuildBasicTab()
    {
        var page = new TabPage("基本信息");
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(16) };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        numDay = MakeNum(0, 100000, 0);
        txtCompany = new TextBox { Width = 230 };
        numCompanyCash = MakeNum(0, 1_000_000_000_000_000m, 0);
        numPersonalCash = MakeNum(0, 1_000_000_000_000_000m, 0);
        numCompanyDebt = MakeNum(0, 1_000_000_000_000_000m, 0);
        numPersonalDebt = MakeNum(0, 1_000_000_000_000_000m, 0);

        int r = 0;
        AddRow(t, r++, "当前天数", numDay, "公司名称", txtCompany);
        AddRow(t, r++, "公司现金", numCompanyCash, "个人现金", numPersonalCash);
        AddRow(t, r++, "公司债务", numCompanyDebt, "个人债务", numPersonalDebt);

        var tip = new Label
        {
            Text = "改完点右上角「保存到该槽位」，再进游戏「读取存档」选该槽位生效。\n（建议先用游戏内 F5 存一次再改）",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 24, 0, 0)
        };
        t.Controls.Add(tip, 0, r);
        t.SetColumnSpan(tip, 4);
        page.Controls.Add(t);
        return page;
    }

    private static NumericUpDown MakeNum(decimal min, decimal max, decimal val)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Min(Math.Max(val, min), max),
            DecimalPlaces = 0,
            ThousandsSeparator = true,
            Width = 200
        };
    }

    private static void AddRow(TableLayoutPanel t, int row, string l1, Control c1, string l2, Control c2)
    {
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        t.Controls.Add(new Label { Text = l1, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        t.Controls.Add(c1, 1, row);
        t.Controls.Add(new Label { Text = l2, AutoSize = true, Anchor = AnchorStyles.Left }, 2, row);
        t.Controls.Add(c2, 3, row);
    }
}
