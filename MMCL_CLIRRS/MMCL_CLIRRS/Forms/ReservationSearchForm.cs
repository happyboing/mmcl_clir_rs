namespace MMCL_CLIRRS;

public partial class ReservationSearchForm : Form
{
    private readonly ReservationManagementService? _management;
    private readonly ReservationSearchService _service;
    private Func<List<Reservation>> _currentSearch;

    public ReservationSearchForm(ReservationSearchService service)
        : this(null, service)
    {
    }

    public ReservationSearchForm(ReservationManagementService? management,
                                 ReservationSearchService service)
    {
        InitializeComponent();
        ApplyGridTheme();
        _management = management;
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ReservationManagementService? managementService = _management;
        if (managementService != null)
        {
            managementService.ReservationExpired += OnReservationExpired;
            Shown += OnStaffScreenShown;
            FormClosed += (s, e) => managementService.ReservationExpired -= OnReservationExpired;
        }

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

        AcceptButton = btnSearch;                          // Enter key triggers Search
        _currentSearch = _service.GetAllChronological;     // what "refresh" means at startup

        UpdateInputVisibility();
        ShowResults(_service.GetAllChronological());
    }


    // Expiration notices that happened while the staff screen was closed are
    // retained by the management service and shown when staff next opens it.
    private void OnStaffScreenShown(object? sender, EventArgs e)
    {
        if (_management == null)
            return;

        foreach (ReservationExpirationNotice notice in _management.GetPendingExpirationNotifications())
            ShowExpirationNotice(notice);
    }

    // Live no-show notifications are raised by the main-menu schedule monitor.
    private void OnReservationExpired(ReservationExpirationNotice notice)
    {
        // If the form has not appeared yet, leave the notice pending for its
        // Shown handler rather than losing the notification.
        if (IsDisposed || !Visible)
            return;

        _management?.AcknowledgeExpirationNotification(notice.ReservationId);
        ShowExpirationNotice(notice);
    }

    private void ShowExpirationNotice(ReservationExpirationNotice notice)
    {
        if (IsDisposed)
            return;

        ShowResults(_currentSearch());
        MessageBox.Show(this,
            $"Reservation #{notice.ReservationId} for {notice.UserName} was not occupied within " +
            $"the 5-minute grace period.\n\n" +
            $"Location: {notice.SpotOrRoom}\n" +
            $"Scheduled start: {notice.ScheduledStart:dd MMM yyyy, HH:mm}\n" +
            $"The location is now available for another reservation.",
            "Reservation automatically expired",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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
        _management?.ProcessDueReservations();
        if (selected.Status != ReservationStatus.Active)
        {
            MessageBox.Show("This reservation is no longer active and cannot be cancelled.");
            ShowResults(_currentSearch());
            return;
        }

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
        _management?.ProcessDueReservations();
        if (selected.Status != ReservationStatus.Active)
        {
            MessageBox.Show("This reservation is no longer active and cannot be updated.");
            ShowResults(_currentSearch());
            return;
        }

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
        _management?.ProcessDueReservations();
        if (selected.Status != ReservationStatus.Active)
        {
            MessageBox.Show("This reservation is no longer active and cannot be verified.");
            ShowResults(_currentSearch());
            return;
        }

        string? entered = PromptDialog.Show(this, "Verify Reservation",
            $"Enter the ID number of the person occupying {selected.SpotOrRoom}:");
        if (entered == null) return;   // cancelled

        OperationResult result = _service.Verify(selected.ReservationId, entered);

        MessageBox.Show(result.Message, result.Success ? "Verified" : "Verification failed",
            MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

        if (result.Success) ShowResults(_currentSearch());
    }
    private void btnUndo_Click(object? sender, EventArgs e)
    {
        _management?.ProcessDueReservations();
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