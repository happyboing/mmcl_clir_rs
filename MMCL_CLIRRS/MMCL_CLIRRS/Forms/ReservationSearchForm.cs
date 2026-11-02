namespace MMCL_CLIRRS;

public partial class ReservationSearchForm : Form
{
    private readonly ReservationSearchService _service;
    private Func<List<Reservation>> _currentSearch;

    public ReservationSearchForm(ReservationSearchService service)
    {
        InitializeComponent();
        ApplyGridTheme();
        _service = service;

        cmbFilter.Items.AddRange(new object[]
            { "Reservation ID", "Name", "User ID", "Date Range", "Spot/Room", "Status", "Discussion Rooms" });
        cmbFilter.SelectedIndex = 0;
        cmbFilter.SelectedIndexChanged += (s, e) => UpdateInputVisibility();
        cmbStatus.DataSource = Enum.GetValues<ReservationStatus>();
        cmbStatus.SelectedIndex = 0;

        btnSearch.Click += btnSearch_Click;
        btnShowAll.Click += btnShowAll_Click;
        btnCancelReservation.Click += btnCancelReservation_Click;

        AcceptButton = btnSearch;                          // Enter key triggers Search
        _currentSearch = _service.GetAllChronological;     // what "refresh" means at startup

        UpdateInputVisibility();
        ShowResults(_service.GetAllChronological());
    }

    // Show date pickers only for date searches, the text box for the rest.
    private void UpdateInputVisibility()
    {
        string? filter = cmbFilter.SelectedItem?.ToString();
        bool isDate = filter == "Date Range";
        bool isStatus = filter == "Status";
        bool needsText = filter != "Discussion Rooms" && !isDate && !isStatus;

        dtpFrom.Visible = dtpTo.Visible = isDate;
        cmbStatus.Visible = isStatus;
        txtSearch.Visible = needsText;
    }
    private void ShowResults(List<Reservation> results)
    {
        dgvReservations.DataSource = null;
        dgvReservations.DataSource = results;
        FormatGrid();

        dgvReservations.CurrentCell = null;   // no row selected by default
        dgvReservations.ClearSelection();
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

        txtSearch.Clear();   // ready for the next search
        txtSearch.Focus();
    }

    private void btnShowAll_Click(object? sender, EventArgs e)
    {
        _currentSearch = _service.GetAllChronological;
        ShowResults(_currentSearch());
    }

    private void btnCancelReservation_Click(object? sender, EventArgs e)
    {
        if (dgvReservations.CurrentRow?.DataBoundItem is not Reservation selected)
        {
            MessageBox.Show("Please select a reservation first.");
            return;
        }

        DialogResult confirm = MessageBox.Show(
            $"Cancel reservation #{selected.ReservationId} for {selected.UserName}?",
            "Confirm cancellation", MessageBoxButtons.YesNo);
        if (confirm != DialogResult.Yes) return;

        if (_service.Cancel(selected.ReservationId))
            ShowResults(_currentSearch());   // re-run the last search so the grid reflects the change
        else
            MessageBox.Show("Only active reservations can be cancelled.");
    }

    private void ApplyGridTheme()
    {
        dgvReservations.EnableHeadersVisualStyles = false;   // needed, or the header colors are ignored
        dgvReservations.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(183, 28, 28);
        dgvReservations.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgvReservations.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgvReservations.GridColor = Color.FromArgb(230, 230, 230);
        dgvReservations.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(253, 236, 236);  // light red tint
        dgvReservations.DefaultCellStyle.SelectionBackColor = Color.FromArgb(211, 47, 47);
        dgvReservations.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvReservations.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(183, 28, 28);
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

        dgvReservations.Columns["Date"]!.DefaultCellStyle.Format = "dd MMM yyyy";
        dgvReservations.Columns["StartTime"]!.DefaultCellStyle.Format = @"hh\:mm";
        dgvReservations.Columns["EndTime"]!.DefaultCellStyle.Format = @"hh\:mm";

        dgvReservations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        dgvReservations.Columns["Department"]!.MinimumWidth = 150;
        dgvReservations.Columns["Department"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }
}