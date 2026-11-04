namespace MMCL_CLIRRS;

public class ReservationStatisticsForm : Form
{
    private static readonly Color Maroon = Color.FromArgb(183, 28, 28);
    private static readonly Color PageBackground = Color.FromArgb(246, 247, 249);

    private readonly ReservationSearchService _search;
    private readonly Panel _contentPanel = new Panel();
    private readonly Label _lblSummary = new Label();
    private readonly ComboBox _cmbTrend = new ComboBox();

    private ReservationStatistics _stats = new();
    private TrendGrouping _trendGrouping = TrendGrouping.Day;
    private TabControl? _tabs;

    public ReservationStatisticsForm(ReservationSearchService search)
    {
        _search = search;

        Text = "Reservation Statistics and Reports";
        Font = new Font("Segoe UI", 9.5f);
        BackColor = PageBackground;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1200, 720);

        BuildHeader();
        BuildToolbar();

        _contentPanel.Location = new Point(16, 116);
        _contentPanel.Size = new Size(1168, 588);
        _contentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                               AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(_contentPanel);

        ReloadStatistics();
    }

    private void BuildHeader()
    {
        var header = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(ClientSize.Width, 64),
            BackColor = Maroon,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        header.Controls.Add(new Label
        {
            Text = "Reservation Statistics and Reports",
            Location = new Point(18, 7),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        });

        header.Controls.Add(new Label
        {
            Text = "Staff dashboard  •  live reservation records, outcomes, locations, and user trends",
            Location = new Point(20, 39),
            AutoSize = true,
            ForeColor = Color.FromArgb(255, 235, 235),
            Font = new Font("Segoe UI", 9)
        });

        var btnRefresh = MakeHeaderButton("Refresh", ClientSize.Width - 262, 94);
        btnRefresh.Click += (s, e) => ReloadStatistics();
        header.Controls.Add(btnRefresh);

        var btnHome = MakeHeaderButton("← Back to Home", ClientSize.Width - 158, 140);
        btnHome.Click += (s, e) => Close();
        header.Controls.Add(btnHome);

        Controls.Add(header);
    }

    private void BuildToolbar()
    {
        var toolbar = new Panel
        {
            Location = new Point(0, 64),
            Size = new Size(ClientSize.Width, 44),
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblSummary.Location = new Point(18, 13);
        _lblSummary.AutoSize = true;
        _lblSummary.ForeColor = Color.FromArgb(55, 71, 79);
        _lblSummary.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        toolbar.Controls.Add(_lblSummary);

        toolbar.Controls.Add(new Label
        {
            Text = "Trend grouping:",
            Location = new Point(820, 13),
            AutoSize = true,
            ForeColor = Color.FromArgb(55, 71, 79)
        });

        _cmbTrend.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbTrend.Items.AddRange(new object[] { "Day", "Week", "Month", "Year" });
        _cmbTrend.SelectedIndex = 0;
        _cmbTrend.Location = new Point(930, 8);
        _cmbTrend.Width = 110;
        _cmbTrend.SelectedIndexChanged += (s, e) =>
        {
            _trendGrouping = (TrendGrouping)_cmbTrend.SelectedIndex;
            ReloadStatistics();
        };
        toolbar.Controls.Add(_cmbTrend);

        Controls.Add(toolbar);

        Controls.Add(new Panel
        {
            Location = new Point(0, 108),
            Size = new Size(ClientSize.Width, 1),
            BackColor = Color.FromArgb(224, 224, 224),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        });
    }

    private static Button MakeHeaderButton(string text, int x, int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, 16),
            Size = new Size(width, 32),
            BackColor = Color.White,
            ForeColor = Maroon,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void ReloadStatistics()
    {
        _stats = new ReservationStatisticsService(_search).Build(_trendGrouping);

        _lblSummary.Text =
            $"Total reservations (excluding expired): {_stats.Total}   •   " +
            $"Active: {_stats.Active}   •   Occupied: {_stats.Occupied}   •   " +
            $"Graph: {_stats.GraphNodes} nodes / {_stats.GraphConnections} links";

        int selectedTab = _tabs?.SelectedIndex ?? 0;

        _contentPanel.SuspendLayout();
        foreach (Control control in _contentPanel.Controls)
            control.Dispose();
        _contentPanel.Controls.Clear();

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f)
        };

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
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(0, 4, 0, 4)
        };

        cards.Controls.Add(MakeStatCard("Total", _stats.Total, Maroon));
        cards.Controls.Add(MakeStatCard("Active", _stats.Active, Color.SeaGreen));
        cards.Controls.Add(MakeStatCard("Occupied", _stats.Occupied, Color.FromArgb(198, 40, 40)));
        cards.Controls.Add(MakeStatCard("Completed", _stats.Completed, Color.FromArgb(21, 101, 192)));
        cards.Controls.Add(MakeStatCard("Cancelled", _stats.Cancelled, Color.FromArgb(239, 108, 0)));

        layout.Controls.Add(cards, 0, 0);
        layout.SetColumnSpan(cards, 2);

        var statusChart = new StatBarChart { Dock = DockStyle.Fill, Margin = new Padding(4) };
        statusChart.SetData("Reservation Outcomes", _stats.StatusCounts);
        layout.Controls.Add(statusChart, 0, 1);

        var trendChart = new StatBarChart { Dock = DockStyle.Fill, Margin = new Padding(4) };
        trendChart.SetData($"Reservation Trends ({_trendGrouping})", _stats.TrendCounts);
        layout.Controls.Add(trendChart, 1, 1);

        page.Controls.Add(layout);
        return page;
    }

    private static Panel MakeStatCard(string title, int value, Color accent)
    {
        var card = new Panel
        {
            Size = new Size(154, 82),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3)
        };

        card.Controls.Add(new Label
        {
            Text = value.ToString(),
            Location = new Point(10, 5),
            AutoSize = true,
            ForeColor = accent,
            Font = new Font("Segoe UI", 19, FontStyle.Bold)
        });

        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(11, 54),
            AutoSize = true,
            ForeColor = Color.FromArgb(84, 110, 122),
            Font = new Font("Segoe UI", 8.5f)
        });

        return card;
    }

    private TabPage BuildLocationsTab()
    {
        var page = NewPage("Locations");

        page.Controls.Add(MakeCountGroup(
            $"Most Used Areas/Seats ({_stats.SpotCounts.Count})",
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
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 3; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3f));

        layout.Controls.Add(MakeCountGroup("By Department", "Department",
            _stats.DepartmentCounts), 0, 0);
        layout.Controls.Add(MakeCountGroup("By Program", "Program",
            _stats.ProgramCounts), 1, 0);
        layout.Controls.Add(MakeCountGroup("By Year Level", "Year Level",
            _stats.YearLevelCounts), 0, 1);
        layout.Controls.Add(MakeCountGroup("By User Type", "User Type",
            _stats.UserTypeCounts), 1, 1);
        layout.Controls.Add(MakeGraphNodeGroup("Top Users (Graph)",
            "User", "Connections", _stats.TopUsers), 0, 2);
        layout.Controls.Add(MakeGraphEdgeGroup("Top User–Location Links (Graph)",
            _stats.TopUserLocations), 1, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildActivityTab()
    {
        var page = NewPage("Activity");

        var group = new GroupBox
        {
            Text = "Recent Reservation Activity — most recent first (Stack)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10)
        };

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.None,
            HorizontalScrollbar = true
        };

        if (_stats.RecentActivity.Count == 0)
        {
            list.Items.Add("(no recorded staff operations yet)");
        }
        else
        {
            foreach (ActivityLogEntry entry in _stats.RecentActivity)
                list.Items.Add(entry.ToString());
        }

        group.Controls.Add(list);
        page.Controls.Add(group);
        return page;
    }

    private static TabPage NewPage(string title)
        => new TabPage(title)
        {
            BackColor = Color.White,
            Padding = new Padding(12)
        };

    private static GroupBox MakeCountGroup(string title, string firstHeader,
                                           IReadOnlyList<CountItem> items)
    {
        var list = MakeTwoColumnList(firstHeader, "Reservations");

        foreach (CountItem item in items)
        {
            var row = new ListViewItem(item.Label);
            row.SubItems.Add(item.Count.ToString());
            list.Items.Add(row);
        }

        if (list.Items.Count == 0)
            list.Items.Add(new ListViewItem("(no data)"));

        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10),
            Margin = new Padding(4)
        };
        group.Controls.Add(list);
        return group;
    }

    private static GroupBox MakeGraphNodeGroup(string title, string firstHeader,
                                               string secondHeader,
                                               IReadOnlyList<GraphNode> nodes)
    {
        var list = MakeTwoColumnList(firstHeader, secondHeader);

        foreach (GraphNode node in nodes)
        {
            var row = new ListViewItem(node.Label);
            row.SubItems.Add(node.Weight.ToString());
            list.Items.Add(row);
        }

        if (list.Items.Count == 0)
            list.Items.Add(new ListViewItem("(no data)"));

        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10),
            Margin = new Padding(4)
        };
        group.Controls.Add(list);
        return group;
    }

    private static GroupBox MakeGraphEdgeGroup(string title,
                                               IReadOnlyList<GraphEdge> edges)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9)
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

        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(10),
            Margin = new Padding(4)
        };
        group.Controls.Add(list);
        return group;
    }

    private static ListView MakeTwoColumnList(string firstHeader, string secondHeader)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9)
        };
        list.Columns.Add(firstHeader, 210);
        list.Columns.Add(secondHeader, 100);
        return list;
    }
}
