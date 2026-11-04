namespace MMCL_CLIRRS;

public class ReservationSearchForm : Form
{
    private readonly ReservationSearchService _service;
    private Func<List<Reservation>> _currentSearch;

    private readonly ComboBox cmbFilter = new ComboBox();
    private readonly TextBox txtSearch = new TextBox();
    private readonly DateTimePicker dtpFrom = new DateTimePicker();
    private readonly DateTimePicker dtpTo = new DateTimePicker();
    private readonly ComboBox cmbStatus = new ComboBox();

    private readonly Panel pnlText = new Panel();
    private readonly Panel pnlDate = new Panel();
    private readonly Panel pnlStatus = new Panel();

    private readonly AppButton btnSearch = new AppButton();
    private readonly AppButton btnShowAll = new AppButton();
    private readonly AppButton btnActivityLog = new AppButton();
    private readonly AppButton btnVerify = new AppButton();
    private readonly AppButton btnUpdate = new AppButton();
    private readonly AppButton btnCancelReservation = new AppButton();
    private readonly AppButton btnUndo = new AppButton();

    private readonly DataGridView dgvReservations = new DataGridView();
    private readonly Label _lblCount = new Label();

    public ReservationSearchForm(ReservationSearchService service)
    {
        _service = service;

        Text = "CLIR Reservation Search and Management";
        Theme.StyleForm(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1200, 680);
        MinimumSize = new Size(1000, 600);

        Controls.Add(Theme.CreateHeader(
            "Search & manage reservations",
            "Find a booking, verify a student, update details, or undo the last change.",
            this));

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(24, 18, 24, 18),
            ColumnCount = 1,
            RowCount = 4
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(content);

        content.Controls.Add(BuildSearchCard(), 0, 0);
        content.Controls.Add(BuildGridCard(), 0, 2);
        content.Controls.Add(BuildActionBar(), 0, 3);

        cmbFilter.Items.AddRange(new object[]
            { "Reservation ID", "Name", "User ID", "Date Range", "Spot/Room", "Status", "Discussion Rooms" });
        cmbFilter.SelectedIndex = 0;
        cmbFilter.SelectedIndexChanged += (s, e) => UpdateInputVisibility();
        cmbStatus.DataSource = Enum.GetValues<ReservationStatus>();
        cmbStatus.SelectedIndex = 0;

        btnSearch.Click += btnSearch_Click;
        btnShowAll.Click += btnShowAll_Click;
        btnCancelReservation.Click += btnCancelReservation_Click;
        btnVerify.Click += btnVerify_Click;
        btnUpdate.Click += btnUpdate_Click;
        btnActivityLog.Click += btnActivityLog_Click;
        btnUndo.Click += btnUndo_Click;

        dgvReservations.CellFormatting += FormatStatusCell;
        AcceptButton = btnSearch;
        _currentSearch = _service.GetAllChronological;

        UpdateInputVisibility();
        ShowResults(_service.GetAllChronological());
    }

    private CardPanel BuildSearchCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Margin = new Padding(0)
        };

        Theme.AddField(card, "Search by", cmbFilter, 18, 14, 180);

        pnlText.Location = new Point(210, 0);
        pnlText.Size = new Size(330, 74);
        pnlText.BackColor = Theme.Surface;
        Theme.AddField(pnlText, "Search term", txtSearch, 0, 14, 320);
        card.Controls.Add(pnlText);

        pnlDate.Location = new Point(210, 0);
        pnlDate.Size = new Size(340, 74);
        pnlDate.BackColor = Theme.Surface;
        Theme.AddField(pnlDate, "From", dtpFrom, 0, 14, 158);
        Theme.AddField(pnlDate, "To", dtpTo, 168, 14, 158);
        card.Controls.Add(pnlDate);

        pnlStatus.Location = new Point(210, 0);
        pnlStatus.Size = new Size(200, 74);
        pnlStatus.BackColor = Theme.Surface;
        Theme.AddField(pnlStatus, "Status", cmbStatus, 0, 14, 180);
        card.Controls.Add(pnlStatus);

        dtpFrom.Format = DateTimePickerFormat.Short;
        dtpTo.Format = DateTimePickerFormat.Short;

        ConfigureButton(btnSearch, "Search", AppButtonKind.Primary, 110);
        ConfigureButton(btnShowAll, "Show all", AppButtonKind.Secondary, 110);
        ConfigureButton(btnActivityLog, "Activity log", AppButtonKind.Secondary, 128);

        btnSearch.Location = new Point(700, 30);
        btnShowAll.Location = new Point(820, 30);
        btnActivityLog.Location = new Point(940, 30);
        btnSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnShowAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnActivityLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        card.SizeChanged += (s, e) =>
        {
            btnSearch.Location = new Point(card.Width - 380, 30);
            btnShowAll.Location = new Point(card.Width - 260, 30);
            btnActivityLog.Location = new Point(card.Width - 140, 30);
        };

        card.Controls.Add(btnSearch);
        card.Controls.Add(btnShowAll);
        card.Controls.Add(btnActivityLog);
        return card;
    }

    private CardPanel BuildGridCard()
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            BorderColor = Theme.Border,
            Padding = new Padding(18, 14, 18, 16),
            Margin = new Padding(0)
        };

        card.Controls.Add(new Label
        {
            Text = "Reservations",
            Location = new Point(18, 13),
            AutoSize = true,
            Font = Theme.Font(11.5f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent
        });

        _lblCount.AutoSize = false;
        _lblCount.Size = new Size(180, 20);
        _lblCount.Location = new Point(600, 17);
        _lblCount.TextAlign = ContentAlignment.MiddleRight;
        _lblCount.Font = Theme.Font(8.75f, FontStyle.Bold);
        _lblCount.ForeColor = Theme.Muted;
        _lblCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        card.Controls.Add(_lblCount);

        card.SizeChanged += (s, e) =>
            _lblCount.Location = new Point(card.Width - 198, 17);

        dgvReservations.Location = new Point(18, 46);
        dgvReservations.Size = new Size(card.Width - 36, card.Height - 62);
        dgvReservations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                  AnchorStyles.Left | AnchorStyles.Right;
        Theme.StyleGrid(dgvReservations);
        card.Controls.Add(dgvReservations);

        return card;
    }

    private Panel BuildActionBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = new Padding(0)
        };

        ConfigureButton(btnVerify, "Verify", AppButtonKind.Primary, 112);
        ConfigureButton(btnUpdate, "Update", AppButtonKind.Secondary, 108);
        ConfigureButton(btnCancelReservation, "Cancel reservation", AppButtonKind.Danger, 176);
        ConfigureButton(btnUndo, "Undo last", AppButtonKind.Secondary, 116);

        btnVerify.Location = new Point(0, 9);
        btnUpdate.Location = new Point(124, 9);
        btnCancelReservation.Location = new Point(244, 9);
        btnUndo.Location = new Point(432, 9);
        btnVerify.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnCancelReservation.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        btnUndo.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        bar.Controls.Add(btnVerify);
        bar.Controls.Add(btnUpdate);
        bar.Controls.Add(btnCancelReservation);
        bar.Controls.Add(btnUndo);

        bar.Controls.Add(new Label
        {
            Text = "Select a row to enable row actions.",
            Location = new Point(566, 20),
            AutoSize = true,
            Font = Theme.Font(8.5f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent
        });

        return bar;
    }

    private static void ConfigureButton(AppButton button, string text, AppButtonKind kind, int width)
    {
        button.Text = text;
        button.Kind = kind;
        button.Size = new Size(width, 38);
    }

    private void UpdateInputVisibility()
    {
        string? filter = cmbFilter.SelectedItem?.ToString();
        bool isDate = filter == "Date Range";
        bool isStatus = filter == "Status";
        bool needsText = filter != "Discussion Rooms" && !isDate && !isStatus;

        pnlDate.Visible = isDate;
        pnlStatus.Visible = isStatus;
        pnlText.Visible = needsText;
    }

    private void ShowResults(List<Reservation> results)
    {
        dgvReservations.DataSource = null;
        dgvReservations.DataSource = results;
        FormatGrid();

        dgvReservations.CurrentCell = null;
        dgvReservations.ClearSelection();

        _lblCount.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
    }

    private void btnSearch_Click(object? sender, EventArgs e)
    {
        string text = txtSearch.Text.Trim();
        DateTime from = dtpFrom.Value;
        DateTime to = dtpTo.Value;
        Func<List<Reservation>> search;

        switch (cmbFilter.SelectedItem?.ToString())
        {
            case "Reservation ID":
                if (!int.TryParse(text, out int id))
                {
                    MessageBox.Show("Please enter a valid numeric Reservation ID.");
                    return;
                }
                search = () =>
                {
                    Reservation? found = _service.FindById(id);
                    return found == null ? new List<Reservation>() : new List<Reservation> { found };
                };
                break;

            case "Name":
                search = () => _service.SearchByName(text);
                break;

            case "User ID":
                search = () => _service.SearchByUserId(text);
                break;

            case "Date Range":
                search = () => _service.SearchByDateRange(from, to);
                break;

            case "Spot/Room":
                search = () => _service.SearchBySpot(text);
                break;

            case "Status":
                if (cmbStatus.SelectedItem is not ReservationStatus status)
                {
                    MessageBox.Show("Please select a status.");
                    return;
                }
                search = () => _service.SearchByStatus(status);
                break;

            case "Discussion Rooms":
                search = _service.GetDiscussionRooms;
                break;

            default:
                search = _service.GetAllChronological;
                break;
        }

        _currentSearch = search;
        ShowResults(_currentSearch());

        txtSearch.Clear();
        txtSearch.Focus();
    }

    private void btnShowAll_Click(object? sender, EventArgs e)
    {
        _currentSearch = _service.GetAllChronological;
        ShowResults(_currentSearch());
    }

    private bool TryGetSelected(out Reservation selected)
    {
        if (dgvReservations.CurrentRow?.DataBoundItem is Reservation r)
        {
            selected = r;
            return true;
        }

        MessageBox.Show("Please select a reservation first.");
        selected = null!;
        return false;
    }

    private void btnCancelReservation_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelected(out Reservation selected)) return;

        DialogResult confirm = MessageBox.Show(
            $"Cancel reservation #{selected.ReservationId} for {selected.UserName}?",
            "Confirm cancellation", MessageBoxButtons.YesNo);
        if (confirm != DialogResult.Yes) return;

        if (_service.Cancel(selected.ReservationId, undoable: true))
            ShowResults(_currentSearch());
        else
            MessageBox.Show("Only active reservations can be cancelled.");
    }

    private void btnUpdate_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelected(out Reservation selected)) return;

        using var dialog = new UpdateReservationDialog(selected);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        OperationResult result = _service.Update(selected.ReservationId, dialog.SpotOrRoom,
            dialog.Date, dialog.StartTime, dialog.EndTime);

        MessageBox.Show(result.Message, result.Success ? "Updated" : "Update failed",
            MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success) ShowResults(_currentSearch());
    }

    private void btnVerify_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelected(out Reservation selected)) return;

        string? entered = PromptDialog.Show(this, "Verify Reservation",
            $"Enter the ID number of the person occupying {selected.SpotOrRoom}:");
        if (entered == null) return;

        OperationResult result = _service.Verify(selected.ReservationId, entered);

        MessageBox.Show(result.Message, result.Success ? "Verified" : "Verification failed",
            MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success) ShowResults(_currentSearch());
    }

    private void btnUndo_Click(object? sender, EventArgs e)
    {
        string? next = _service.NextUndoDescription;
        if (next == null)
        {
            MessageBox.Show("There is nothing to undo.");
            return;
        }

        DialogResult confirm = MessageBox.Show($"Undo: {next}?", "Confirm undo", MessageBoxButtons.YesNo);
        if (confirm != DialogResult.Yes) return;

        OperationResult result = _service.UndoLast();
        MessageBox.Show(result.Message, result.Success ? "Undone" : "Can't undo",
            MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success) ShowResults(_currentSearch());
    }

    private void btnActivityLog_Click(object? sender, EventArgs e)
    {
        using var log = new ActivityLogForm(_service.GetRecentActivity());
        log.ShowDialog(this);
    }

    private void FormatGrid()
    {
        var headers = new Dictionary<string, string>
        {
            ["ReservationId"] = "ID",
            ["UserName"] = "Name",
            ["UserIdNumber"] = "User ID",
            ["UserType"] = "Type",
            ["Department"] = "Department",
            ["Program"] = "Program",
            ["YearLevel"] = "Year",
            ["SpotOrRoom"] = "Spot / Room",
            ["IsDiscussionRoom"] = "Room?",
            ["Date"] = "Date",
            ["StartTime"] = "Start",
            ["EndTime"] = "End",
            ["Status"] = "Status"
        };

        foreach (DataGridViewColumn col in dgvReservations.Columns)
            if (headers.TryGetValue(col.DataPropertyName, out string? text))
                col.HeaderText = text;

        if (dgvReservations.Columns["Date"] is { } dateColumn)
            dateColumn.DefaultCellStyle.Format = "dd MMM yyyy";
        if (dgvReservations.Columns["StartTime"] is { } startColumn)
            startColumn.DefaultCellStyle.Format = @"hh\:mm";
        if (dgvReservations.Columns["EndTime"] is { } endColumn)
            endColumn.DefaultCellStyle.Format = @"hh\:mm";

        dgvReservations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

        if (dgvReservations.Columns["Department"] is { } departmentColumn)
        {
            departmentColumn.MinimumWidth = 150;
            departmentColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
    }

    private void FormatStatusCell(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.CellStyle == null) return;
        if (dgvReservations.Columns[e.ColumnIndex].DataPropertyName != "Status") return;

        if (e.Value is ReservationStatus status)
        {
            e.CellStyle.ForeColor = status switch
            {
                ReservationStatus.Active => Theme.Success,
                ReservationStatus.Occupied => Theme.Info,
                ReservationStatus.Cancelled => Theme.Danger,
                ReservationStatus.Expired => Theme.Warning,
                _ => Theme.Muted
            };
            e.CellStyle.Font = Theme.Font(9f, FontStyle.Bold);
        }
    }
}
