namespace MMCL_CLIRRS;

public class ReservationStatisticsForm : Form
{
    private readonly ReservationSearchService _search;
    private readonly Panel _contentPanel = new Panel();
    private readonly Label _lblSummary = new Label();
    private readonly ComboBox _cmbTrend = new ComboBox();
    private readonly FieldShell _cmbTrendShell;

    private ReservationStatistics _stats = new();
    private TrendGrouping _trendGrouping = TrendGrouping.Day;
    private ModernTabControl? _tabs;

    public ReservationStatisticsForm(ReservationSearchService search)
    {
        _search = search;

        Text = "Reservation Statistics and Reports";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1200, 680);
        MinimumSize = new Size(1050, 600);

        var header = Theme.CreateHeader(
            "Reservation statistics",
            "A live view of reservations, outcomes, locations, and user trends.",
            this);
        Controls.Add(header);

        var btnRefresh = new AppButton
        {
            Text = "Refresh",
            Kind = AppButtonKind.Quiet,
            Size = new Size(104, 34),
            Location = new Point(header.Width - 260, 26),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnRefresh.Click += (s, e) => ReloadStatistics();
        header.Controls.Add(btnRefresh);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(24, 18, 24, 18),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        _lblSummary.AutoSize = true;
        _lblSummary.Font = Theme.Font(9.25f, FontStyle.Bold);
        _lblSummary.ForeColor = Theme.Muted;

        _cmbTrend.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTrend.Items.AddRange(new object[] { "Day", "Week", "Month", "Year" });
        _cmbTrend.SelectedIndex = 0;
        _cmbTrend.SelectedIndexChanged += (s, e) =>
        {
            _trendGrouping = (TrendGrouping)_cmbTrend.SelectedIndex;
            ReloadStatistics();
        };
        _cmbTrendShell = new FieldShell(_cmbTrend, 38) { Size = new Size(122, 38) };

        layout.Controls.Add(BuildToolbar(), 0, 0);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = Theme.Background;
        layout.Controls.Add(_contentPanel, 0, 2);

        ReloadStatistics();
    }

    private CardPanel BuildToolbar()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(18, 0, 18, 0),
            Margin = new Padding(0)
        };

        _lblSummary.Location = new Point(18, 22);
        card.Controls.Add(_lblSummary);

        var caption = Theme.Caption("TREND GROUPING");
        caption.Location = new Point(900, 24);
        caption.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        card.Controls.Add(caption);

        _cmbTrendShell.Location = new Point(1020, 13);
        _cmbTrendShell.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        card.Controls.Add(_cmbTrendShell);

        card.SizeChanged += (s, e) =>
        {
            caption.Location = new Point(card.Width - 282, 24);
            _cmbTrendShell.Location = new Point(card.Width - 140, 13);
        };

        return card;
    }

    private void ReloadStatistics()
    {
        _stats = new ReservationStatisticsService(_search).Build(_trendGrouping);

        _lblSummary.Text =
            $"Total {_stats.Total}   •   Active {_stats.Active}   •   Occupied {_stats.Occupied}   •   " +
            $"Completed {_stats.Completed}   •   Cancelled {_stats.Cancelled}   •   " +
            $"Graph {_stats.GraphNodes} nodes / {_stats.GraphConnections} links";

        int selectedTab = _tabs?.SelectedIndex ?? 0;

        _contentPanel.SuspendLayout();
        while (_contentPanel.Controls.Count > 0)
            _contentPanel.Controls[0].Dispose();

        _tabs = new ModernTabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(BuildOverviewTab());
        _tabs.TabPages.Add(BuildLocationsTab());
        _tabs.TabPages.Add(BuildUsersTab());
        _tabs.TabPages.Add(BuildActivityTab());

        if (selectedTab < _tabs.TabCount)
            _tabs.SelectedIndex = selectedTab;

        _contentPanel.Controls.Add(_tabs);
        _contentPanel.ResumeLayout();
    }

    private TabPage BuildOverviewTab()
    {
        var page = NewPage("Overview");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Theme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Background,
            Padding = new Padding(0, 4, 0, 8)
        };

        cards.Controls.Add(MakeStatCard("Total", _stats.Total, Theme.Primary));
        cards.Controls.Add(MakeStatCard("Active", _stats.Active, Theme.Success));
        cards.Controls.Add(MakeStatCard("Occupied", _stats.Occupied, Theme.Info));
        cards.Controls.Add(MakeStatCard("Completed", _stats.Completed, Theme.Muted));
        cards.Controls.Add(MakeStatCard("Cancelled", _stats.Cancelled, Theme.Danger));

        layout.Controls.Add(cards, 0, 0);
        layout.SetColumnSpan(cards, 2);

        var statusChart = new StatBarChart { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 7, 0) };
        statusChart.SetData("Reservation outcomes", _stats.StatusCounts);
        layout.Controls.Add(statusChart, 0, 1);

        var trendChart = new StatBarChart { Dock = DockStyle.Fill, Margin = new Padding(7, 0, 0, 0) };
        trendChart.SetData($"Reservation trends · {_trendGrouping}", _stats.TrendCounts);
        layout.Controls.Add(trendChart, 1, 1);

        page.Controls.Add(layout);
        return page;
    }

    private static CardPanel MakeStatCard(string title, int value, Color accent)
    {
        var card = new CardPanel
        {
            Size = new Size(178, 92),
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(16)
        };

        card.Controls.Add(new Panel
        {
            Location = new Point(16, 14),
            Size = new Size(28, 3),
            BackColor = accent
        });

        card.Controls.Add(new Label
        {
            Text = value.ToString(),
            Location = new Point(16, 24),
            AutoSize = true,
            Font = Theme.Font(24f, FontStyle.Bold),
            ForeColor = accent,
            BackColor = Color.Transparent
        });

        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(18, 66),
            AutoSize = true,
            Font = Theme.Font(8.75f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        return card;
    }

    private TabPage BuildLocationsTab()
    {
        var page = NewPage("Locations");
        page.Controls.Add(MakeCountGroup(
            $"Most used areas and seats  ·  {_stats.SpotCounts.Count}",
            "Area / Seat", _stats.SpotCounts));
        return page;
    }

    private TabPage BuildUsersTab()
    {
        var page = NewPage("Users");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Theme.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 3; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3f));

        layout.Controls.Add(MakeCountGroup("By department", "Department",
            _stats.DepartmentCounts), 0, 0);
        layout.Controls.Add(MakeCountGroup("By program", "Program",
            _stats.ProgramCounts), 1, 0);
        layout.Controls.Add(MakeCountGroup("By year level", "Year level",
            _stats.YearLevelCounts), 0, 1);
        layout.Controls.Add(MakeCountGroup("By user type", "User type",
            _stats.UserTypeCounts), 1, 1);
        layout.Controls.Add(MakeGraphNodeGroup("Top users  ·  graph",
            "User", "Connections", _stats.TopUsers), 0, 2);
        layout.Controls.Add(MakeGraphEdgeGroup("Top user–location links  ·  graph",
            _stats.TopUserLocations), 1, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildActivityTab()
    {
        var page = NewPage("Activity");

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(18, 14, 18, 18)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Recent reservation activity  ·  most recent first",
            Dock = DockStyle.Fill,
            Font = Theme.Font(10.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        }, 0, 0);

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.25f),
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Ink,
            HorizontalScrollbar = true,
            IntegralHeight = false
        };

        if (_stats.RecentActivity.Count == 0)
        {
            list.Items.Add("(no recorded staff operations yet)");
            list.ForeColor = Theme.Muted;
        }
        else
        {
            foreach (ActivityLogEntry entry in _stats.RecentActivity)
                list.Items.Add(entry.ToString());
        }

        layout.Controls.Add(list, 0, 1);
        card.Controls.Add(layout);
        page.Controls.Add(card);
        return page;
    }

    private static TabPage NewPage(string title)
        => new(title)
        {
            BackColor = Theme.Surface,
            Padding = new Padding(16)
        };

    private static CardPanel MakeCountGroup(string title, string firstHeader,
                                            IReadOnlyList<CountItem> items)
    {
        var (card, body) = MakeListCard(title);
        var list = MakeTwoColumnList(firstHeader, "Reservations");

        foreach (CountItem item in items)
        {
            var row = new ListViewItem(item.Label);
            row.SubItems.Add(item.Count.ToString());
            list.Items.Add(row);
        }

        if (list.Items.Count == 0)
            list.Items.Add(new ListViewItem("(no data)"));

        body.Controls.Add(list);
        return card;
    }

    private static CardPanel MakeGraphNodeGroup(string title, string firstHeader,
                                                string secondHeader,
                                                IReadOnlyList<GraphNode> nodes)
    {
        var (card, body) = MakeListCard(title);
        var list = MakeTwoColumnList(firstHeader, secondHeader);

        foreach (GraphNode node in nodes)
        {
            var row = new ListViewItem(node.Label);
            row.SubItems.Add(node.Weight.ToString());
            list.Items.Add(row);
        }

        if (list.Items.Count == 0)
            list.Items.Add(new ListViewItem("(no data)"));

        body.Controls.Add(list);
        return card;
    }

    private static CardPanel MakeGraphEdgeGroup(string title,
                                                IReadOnlyList<GraphEdge> edges)
    {
        var (card, body) = MakeListCard(title);

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Ink,
            Font = Theme.Font(9f)
        };
        list.Columns.Add("User", 190);
        list.Columns.Add("Location", 180);
        list.Columns.Add("Links", 70);

        foreach (GraphEdge edge in edges)
        {
            var row = new ListViewItem(edge.FromLabel);
            row.SubItems.Add(edge.ToLabel);
            row.SubItems.Add(edge.Weight.ToString());
            list.Items.Add(row);
        }

        if (list.Items.Count == 0)
            list.Items.Add(new ListViewItem("(no data)"));

        body.Controls.Add(list);
        return card;
    }

    private static (CardPanel card, Panel body) MakeListCard(string title)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Margin = new Padding(0, 0, 10, 10),
            Padding = new Padding(16, 12, 16, 16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = Theme.Font(10.25f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        }, 0, 0);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = new Padding(0)
        };
        layout.Controls.Add(body, 0, 1);

        card.Controls.Add(layout);
        return (card, body);
    }

    private static ListView MakeTwoColumnList(string firstHeader, string secondHeader)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Ink,
            Font = Theme.Font(9f)
        };
        list.Columns.Add(firstHeader, 220);
        list.Columns.Add(secondHeader, 100);
        return list;
    }
}
