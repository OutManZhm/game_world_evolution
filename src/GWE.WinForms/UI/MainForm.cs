using System.Reflection;
using GWE.Service;
using GWE.Service.Turn;

namespace GWE.WinForms.UI;

/// <summary>主界面：主要 NPC 当前状态 + 事件流 + 世界张力 + “下一回合”。只调用 Service，不接触数据库。</summary>
public sealed class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(31, 45, 61);
    private static readonly Color Muted = Color.FromArgb(110, 126, 145);
    private static readonly Color Accent = Color.FromArgb(47, 111, 237);
    private static readonly Color Surface = Color.FromArgb(247, 248, 250);

    private readonly ITurnService _turnService;
    private readonly INpcQueryService _queryService;

    private readonly Label _clockLabel = new();
    private readonly Label _statusLabel = new();
    private readonly DataGridView _npcGrid = CreateGrid();
    private readonly DataGridView _eventGrid = CreateGrid();
    private readonly DataGridView _tensionGrid = CreateGrid();
    private readonly DataGridView _factionGrid = CreateGrid();
    private readonly DataGridView _memoryGrid = CreateGrid();
    private readonly DataGridView _beliefGrid = CreateGrid();
    private readonly Label _profileLabel = new();
    private readonly Button _nextTurnButton = new();
    private readonly SplitContainer _split = new();

    public int NpcRowCount => _npcGrid.Rows.Count;
    public int EventRowCount => _eventGrid.Rows.Count;
    public int TensionRowCount => _tensionGrid.Rows.Count;
    public int FactionRowCount => _factionGrid.Rows.Count;

    public MainForm(ITurnService turnService, INpcQueryService queryService)
    {
        _turnService = turnService;
        _queryService = queryService;

        Text = "世界演化 · 回合推演";
        ClientSize = new Size(1320, 820);
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Surface;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildLayout();
        ConfigureColumns();
        _npcGrid.SelectionChanged += OnNpcSelectionChanged;
        Load += (_, _) =>
        {
            try
            {
                _split.SplitterDistance = Math.Max(200, (int)(_split.Height * 0.52));
            }
            catch (InvalidOperationException)
            {
                // 窗体尺寸尚未确定时忽略，沿用默认分割位置。
            }
            RefreshAll();
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.White };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "世界演化 · 灰岩城纪事",
            Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
            ForeColor = Ink,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };
        _clockLabel.ForeColor = Muted;
        _clockLabel.Dock = DockStyle.Fill;
        _clockLabel.TextAlign = ContentAlignment.MiddleRight;
        _clockLabel.AutoSize = true;
        _clockLabel.Margin = new Padding(0, 0, 18, 0);

        _nextTurnButton.Text = "下一回合";
        _nextTurnButton.Width = 128;
        _nextTurnButton.Height = 38;
        _nextTurnButton.FlatStyle = FlatStyle.Flat;
        _nextTurnButton.FlatAppearance.BorderSize = 0;
        _nextTurnButton.BackColor = Accent;
        _nextTurnButton.ForeColor = Color.White;
        _nextTurnButton.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        _nextTurnButton.Cursor = Cursors.Hand;
        _nextTurnButton.Margin = new Padding(0, 0, 20, 0);
        _nextTurnButton.Click += OnNextTurn;

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(_clockLabel, 1, 0);
        header.Controls.Add(_nextTurnButton, 2, 0);

        var split = _split;
        split.Dock = DockStyle.Fill;
        split.Orientation = Orientation.Horizontal;
        split.SplitterWidth = 8;
        split.BackColor = Surface;
        split.Panel1MinSize = 160;
        split.Panel2MinSize = 140;
        split.Panel1.Controls.Add(BuildSection("主要 NPC 当前状态", _npcGrid));

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 9F) };
        tabs.TabPages.Add(BuildTabPage("事件流", BuildSection("事件因果流", _eventGrid)));
        tabs.TabPages.Add(BuildTabPage("世界张力", BuildSection("未解决的矛盾", _tensionGrid)));
        tabs.TabPages.Add(BuildTabPage("组织秘材", BuildSection("组织库存与秘材流向", _factionGrid)));
        tabs.TabPages.Add(BuildTabPage("人物档案", BuildProfilePage()));
        tabs.Padding = new Point(12, 4);
        split.Panel2.Controls.Add(tabs);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Muted;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(16, 0, 0, 0);
        _statusLabel.Text = "就绪。";

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);
        Controls.Add(root);
    }

    private static TabPage BuildTabPage(string title, Control content)
    {
        var page = new TabPage(title) { BackColor = Surface, Padding = new Padding(4) };
        page.Controls.Add(content);
        return page;
    }

    private static Control BuildSection(string title, Control content)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 1, Padding = new Padding(16, 8, 16, 8) };
        var container = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.White, Padding = new Padding(1) };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = Ink,
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        container.Controls.Add(label, 0, 0);
        container.Controls.Add(content, 0, 1);
        panel.Controls.Add(container, 0, 0);
        return panel;
    }

    private Control BuildProfilePage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(16, 8, 16, 8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        _profileLabel.Dock = DockStyle.Fill;
        _profileLabel.ForeColor = Ink;
        _profileLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
        _profileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _profileLabel.Text = "选中上方人物查看其记忆与信念。";

        root.Controls.Add(_profileLabel, 0, 0);
        root.Controls.Add(BuildSection("记忆（情绪权重随年月衰减）", _memoryGrid), 0, 1);
        root.Controls.Add(BuildSection("信念（他以为的事实，置信度随传播衰减）", _beliefGrid), 0, 2);
        return root;
    }

    private void OnNpcSelectionChanged(object? sender, EventArgs e)
    {
        if (_npcGrid.CurrentRow?.DataBoundItem is Service.Contracts.NpcStatusDto npc)
        {
            RefreshProfile(npc.Id);
        }
    }

    private void RefreshProfile(long npcId)
    {
        var profile = _queryService.GetProfile(npcId);
        _profileLabel.Text = $"{profile.Name} · {profile.Title} · {profile.Location}"
            + $"　记忆 {profile.Memories.Count} 条　信念 {profile.Beliefs.Count} 条";
        _memoryGrid.DataSource = profile.Memories.ToList();
        _beliefGrid.DataSource = profile.Beliefs.ToList();
    }

    private void ConfigureColumns()
    {
        AddColumn(_npcGrid, "姓名", nameof(Service.Contracts.NpcStatusDto.Name), 110);
        AddColumn(_npcGrid, "称号", "RankTitle", 84);
        AddColumn(_npcGrid, "身份", "Role", 82);
        AddColumn(_npcGrid, "势力", "Faction", 120);
        AddColumn(_npcGrid, "所在地", "Location", 120);
        AddColumn(_npcGrid, "现场", "Site", 100);
        AddColumn(_npcGrid, "设施", "Facilities", 72);
        AddColumn(_npcGrid, "年龄", "Age", 48);
        AddColumn(_npcGrid, "战力", "Power", 56);
        AddColumn(_npcGrid, "学术", "ArcanaRank", 48);
        AddColumn(_npcGrid, "进阶进度", "Cultivation", 70);
        AddColumn(_npcGrid, "秘材", "HasReagent", 48);
        AddColumn(_npcGrid, "根骨", "Constitution", 48);
        AddColumn(_npcGrid, "悟性", "Comprehension", 48);
        AddColumn(_npcGrid, "魅力", "Charm", 48);
        AddColumn(_npcGrid, "体质", "Physique", 48);
        AddColumn(_npcGrid, "意志", "Willpower", 48);
        AddColumn(_npcGrid, "健康", "Health", 52);
        AddColumn(_npcGrid, "心情", "Mood", 52);
        AddColumn(_npcGrid, "金钱", "Money", 52);
        AddColumn(_npcGrid, "AP", "ApBudget", 40);
        var lastAction = AddColumn(_npcGrid, "最近动向", "LastAction", 320);
        lastAction.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        AddColumn(_eventGrid, "时间", "Time", 72);
        AddColumn(_eventGrid, "类型", "Type", 64);
        AddColumn(_eventGrid, "人物", "Actor", 100);
        AddColumn(_eventGrid, "显著", "Salience", 48);
        AddColumn(_eventGrid, "状态", "Status", 64);
        var summary = AddColumn(_eventGrid, "事件", "Summary", 400);
        summary.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        AddColumn(_tensionGrid, "类型", "Type", 140);
        var stakes = AddColumn(_tensionGrid, "利害", "Stakes", 300);
        stakes.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        AddColumn(_tensionGrid, "紧迫度", "Urgency", 64);
        AddColumn(_tensionGrid, "期限回合", "DeadlineTick", 72);
        AddColumn(_tensionGrid, "状态", "State", 64);

        AddColumn(_factionGrid, "组织", "Name", 180);
        AddColumn(_factionGrid, "类型", "Kind", 130);
        AddColumn(_factionGrid, "成员", "Members", 56);
        AddColumn(_factionGrid, "秘材库存", "ReagentStock", 72);
        AddColumn(_factionGrid, "库存上限", "ReagentCap", 72);
        AddColumn(_factionGrid, "累计发放", "ReagentIssued", 72);
        AddColumn(_factionGrid, "排队待发", "Awaiting", 72);
        AddColumn(_factionGrid, "金库", "Treasury", 72);
        AddColumn(_factionGrid, "金库软上限", "SoftCap", 88);
        AddColumn(_factionGrid, "状态", "Status", 64);

        AddColumn(_memoryGrid, "回合", "Tick", 76);
        AddColumn(_memoryGrid, "类型", "Kind", 88);
        AddColumn(_memoryGrid, "记忆", "Summary", 420).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        AddColumn(_memoryGrid, "情绪权重", "Weight", 72);

        AddColumn(_beliefGrid, "回合", "Tick", 76);
        AddColumn(_beliefGrid, "信念", "Content", 420).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        AddColumn(_beliefGrid, "置信度", "Confidence", 64);
        AddColumn(_beliefGrid, "来源", "Source", 64);
    }

    private static DataGridViewTextBoxColumn AddColumn(DataGridView grid, string header, string property, int width)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Name = property,
            Width = width,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        grid.Columns.Add(column);
        return column;
    }

    private void OnNextTurn(object? sender, EventArgs e)
    {
        try
        {
            _nextTurnButton.Enabled = false;
            var result = _turnService.Advance();
            RefreshAll();
            _statusLabel.Text = $"第 {result.Tick} 回合结算完成：{result.Events.Count} 条事件，{result.Year} 年 {result.Month} 月。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "回合结算失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _nextTurnButton.Enabled = true;
        }
    }

    private void RefreshAll()
    {
        var clock = _queryService.GetClock();
        _clockLabel.Text = clock.Text;
        Text = $"世界演化 · 第 {clock.Tick} 回合";

        _npcGrid.DataSource = _queryService.GetMainNpcs().ToList();
        _eventGrid.DataSource = _queryService.GetRecentEvents(300).ToList();
        _tensionGrid.DataSource = _queryService.GetTensions().ToList();
        _factionGrid.DataSource = _queryService.GetFactions().ToList();

        if (_npcGrid.Rows.Count > 0)
        {
            _npcGrid.Rows[0].Selected = true;
            _npcGrid.CurrentCell = _npcGrid.Rows[0].Cells[0];
            if (_npcGrid.CurrentRow?.DataBoundItem is Service.Contracts.NpcStatusDto selected)
            {
                RefreshProfile(selected.Id);
            }
        }

        if (_eventGrid.Rows.Count > 0)
        {
            _eventGrid.FirstDisplayedScrollingRowIndex = _eventGrid.Rows.Count - 1;
        }
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(230, 234, 242),
            EnableHeadersVisualStyles = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(238, 242, 249),
            ForeColor = Color.FromArgb(58, 74, 95),
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Ink,
            SelectionBackColor = Color.FromArgb(220, 231, 251),
            SelectionForeColor = Ink,
            Padding = new Padding(6, 0, 0, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 251, 253),
            SelectionBackColor = Color.FromArgb(220, 231, 251),
            SelectionForeColor = Ink
        };
        grid.RowTemplate.Height = 30;

        typeof(DataGridView)
            .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(grid, true);
        return grid;
    }
}
