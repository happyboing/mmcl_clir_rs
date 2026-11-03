namespace MMCL_CLIRRS
{
    partial class ReservationSearchForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblSearchBy = new Label();
            cmbFilter = new ComboBox();
            txtSearch = new TextBox();
            dtpFrom = new DateTimePicker();
            dtpTo = new DateTimePicker();
            btnSearch = new Button();
            btnShowAll = new Button();
            btnCancelReservation = new Button();
            dgvReservations = new DataGridView();
            pnlHeader = new Panel();
            lblTitle = new Label();
            cmbStatus = new ComboBox();
            btnActivityLog = new Button();
            btnVerify = new Button();
            btnUpdate = new Button();
            btnUndo = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvReservations).BeginInit();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // lblSearchBy
            // 
            lblSearchBy.AutoSize = true;
            lblSearchBy.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSearchBy.ForeColor = Color.FromArgb(183, 28, 28);
            lblSearchBy.Location = new Point(20, 83);
            lblSearchBy.Name = "lblSearchBy";
            lblSearchBy.Size = new Size(71, 17);
            lblSearchBy.TabIndex = 0;
            lblSearchBy.Text = "Search by:";
            // 
            // cmbFilter
            // 
            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilter.FormattingEnabled = true;
            cmbFilter.Location = new Point(100, 80);
            cmbFilter.Name = "cmbFilter";
            cmbFilter.Size = new Size(170, 25);
            cmbFilter.TabIndex = 1;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(285, 80);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(260, 25);
            txtSearch.TabIndex = 2;
            // 
            // dtpFrom
            // 
            dtpFrom.Format = DateTimePickerFormat.Short;
            dtpFrom.Location = new Point(285, 80);
            dtpFrom.Name = "dtpFrom";
            dtpFrom.Size = new Size(125, 25);
            dtpFrom.TabIndex = 3;
            // 
            // dtpTo
            // 
            dtpTo.Format = DateTimePickerFormat.Short;
            dtpTo.Location = new Point(420, 80);
            dtpTo.Name = "dtpTo";
            dtpTo.Size = new Size(125, 25);
            dtpTo.TabIndex = 4;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = Color.FromArgb(183, 28, 28);
            btnSearch.Cursor = Cursors.Hand;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSearch.ForeColor = Color.White;
            btnSearch.Location = new Point(560, 76);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(100, 32);
            btnSearch.TabIndex = 5;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = false;
            // 
            // btnShowAll
            // 
            btnShowAll.BackColor = Color.White;
            btnShowAll.Cursor = Cursors.Hand;
            btnShowAll.FlatAppearance.BorderColor = Color.FromArgb(183, 28, 28);
            btnShowAll.FlatStyle = FlatStyle.Flat;
            btnShowAll.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnShowAll.ForeColor = Color.FromArgb(183, 28, 28);
            btnShowAll.Location = new Point(670, 76);
            btnShowAll.Name = "btnShowAll";
            btnShowAll.Size = new Size(100, 32);
            btnShowAll.TabIndex = 6;
            btnShowAll.Text = "Show All";
            btnShowAll.UseVisualStyleBackColor = false;
            // 
            // btnCancelReservation
            // 
            btnCancelReservation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCancelReservation.BackColor = Color.FromArgb(140, 20, 20);
            btnCancelReservation.Cursor = Cursors.Hand;
            btnCancelReservation.FlatAppearance.BorderSize = 0;
            btnCancelReservation.FlatStyle = FlatStyle.Flat;
            btnCancelReservation.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancelReservation.ForeColor = Color.White;
            btnCancelReservation.Location = new Point(332, 558);
            btnCancelReservation.Name = "btnCancelReservation";
            btnCancelReservation.Size = new Size(190, 36);
            btnCancelReservation.TabIndex = 7;
            btnCancelReservation.Text = "Cancel Reservation";
            btnCancelReservation.UseVisualStyleBackColor = false;
            // 
            // dgvReservations
            // 
            dgvReservations.AllowUserToAddRows = false;
            dgvReservations.AllowUserToDeleteRows = false;
            dgvReservations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvReservations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReservations.BackgroundColor = Color.White;
            dgvReservations.BorderStyle = BorderStyle.None;
            dgvReservations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvReservations.Location = new Point(12, 147);
            dgvReservations.MultiSelect = false;
            dgvReservations.Name = "dgvReservations";
            dgvReservations.ReadOnly = true;
            dgvReservations.RowHeadersVisible = false;
            dgvReservations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReservations.Size = new Size(960, 405);
            dgvReservations.TabIndex = 8;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(183, 28, 28);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(984, 60);
            pnlHeader.TabIndex = 9;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 14);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(451, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "CLIR Reservation Search and Management";
            // 
            // cmbStatus
            // 
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.FormattingEnabled = true;
            cmbStatus.Location = new Point(285, 80);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Size = new Size(170, 25);
            cmbStatus.TabIndex = 10;
            // 
            // btnActivityLog
            // 
            btnActivityLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnActivityLog.BackColor = Color.White;
            btnActivityLog.Cursor = Cursors.Hand;
            btnActivityLog.FlatStyle = FlatStyle.Flat;
            btnActivityLog.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnActivityLog.ForeColor = Color.FromArgb(183, 28, 28);
            btnActivityLog.Location = new Point(782, 75);
            btnActivityLog.Name = "btnActivityLog";
            btnActivityLog.Size = new Size(190, 32);
            btnActivityLog.TabIndex = 11;
            btnActivityLog.Text = "Activity Log";
            btnActivityLog.UseVisualStyleBackColor = false;
            // 
            // btnVerify
            // 
            btnVerify.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnVerify.BackColor = Color.FromArgb(183, 28, 28);
            btnVerify.Cursor = Cursors.Hand;
            btnVerify.FlatStyle = FlatStyle.Flat;
            btnVerify.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnVerify.ForeColor = Color.White;
            btnVerify.Location = new Point(20, 558);
            btnVerify.Name = "btnVerify";
            btnVerify.Size = new Size(150, 36);
            btnVerify.TabIndex = 12;
            btnVerify.Text = "Verify";
            btnVerify.UseVisualStyleBackColor = false;
            // 
            // btnUpdate
            // 
            btnUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUpdate.BackColor = Color.FromArgb(183, 28, 28);
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUpdate.ForeColor = Color.White;
            btnUpdate.Location = new Point(176, 558);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(150, 36);
            btnUpdate.TabIndex = 13;
            btnUpdate.Text = "Update";
            btnUpdate.UseVisualStyleBackColor = false;
            // 
            // btnUndo
            // 
            btnUndo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUndo.BackColor = Color.White;
            btnUndo.Cursor = Cursors.Hand;
            btnUndo.FlatStyle = FlatStyle.Flat;
            btnUndo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUndo.ForeColor = Color.FromArgb(183, 28, 28);
            btnUndo.Location = new Point(528, 558);
            btnUndo.Name = "btnUndo";
            btnUndo.Size = new Size(150, 36);
            btnUndo.TabIndex = 14;
            btnUndo.Text = "Undo Last";
            btnUndo.UseVisualStyleBackColor = false;
            // 
            // ReservationSearchForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 636);
            Controls.Add(btnUndo);
            Controls.Add(btnUpdate);
            Controls.Add(btnVerify);
            Controls.Add(btnActivityLog);
            Controls.Add(cmbStatus);
            Controls.Add(lblSearchBy);
            Controls.Add(pnlHeader);
            Controls.Add(dgvReservations);
            Controls.Add(btnCancelReservation);
            Controls.Add(btnShowAll);
            Controls.Add(btnSearch);
            Controls.Add(dtpTo);
            Controls.Add(dtpFrom);
            Controls.Add(txtSearch);
            Controls.Add(cmbFilter);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MinimumSize = new Size(900, 500);
            Name = "ReservationSearchForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CLIR Reservation Search and Management";
            ((System.ComponentModel.ISupportInitialize)dgvReservations).EndInit();
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblSearchBy;
        private ComboBox cmbFilter;
        private TextBox txtSearch;
        private DateTimePicker dtpFrom;
        private DateTimePicker dtpTo;
        private Button btnSearch;
        private Button btnShowAll;
        private Button btnCancelReservation;
        private DataGridView dgvReservations;
        private Panel pnlHeader;
        private Label lblTitle;
        private ComboBox cmbStatus;
        private Button btnActivityLog;
        private Button btnVerify;
        private Button btnUpdate;
        private Button btnUndo;
    }
}
