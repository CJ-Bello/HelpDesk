using HelpDesk.BLL;
using HelpDesk.DAL;
using HelpDesk.DTO;
using HelpDesk.Model;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.UI
{
    public partial class Form1 : Form
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public Form1(
            ITicketService ticketService,
            ITicketCategoryRepository ticketCategoryRepository,
            IEmployeeRepository employeeRepository)
        {
            InitializeComponent();
            _ticketService = ticketService;
            _ticketCategoryRepository = ticketCategoryRepository;
            _employeeRepository = employeeRepository;

            dgTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgTickets.MultiSelect = false;
            dgTickets.ReadOnly = true;
            dgTickets.AllowUserToAddRows = false;

            dgTickets.CellClick += dgTickets_CellClick;
            dgTickets.SelectionChanged += dgTickets_SelectionChanged;

            LoadTickets();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDefaultValues();
            LoadTickets();
            dgTickets.SelectionChanged += dgTickets_SelectionChanged;
            dgTickets.CellClick += dgTickets_CellClick;
            this.dgTickets.SelectionChanged += new System.EventHandler(this.dgTickets_SelectionChanged);
            this.dgTickets.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgTickets_CellClick);
            dgTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgTickets.MultiSelect = false; 
            dgTickets.ReadOnly = true;    
            dgTickets.AutoGenerateColumns = false;

            var categories = new List<TicketCategory> 
            {
                new TicketCategory { Id = 1, Name = "Hardware" },
                new TicketCategory { Id = 2, Name = "Software" },
                new TicketCategory { Id = 3, Name = "Network" },
                new TicketCategory { Id = 4, Name = "Account Access" },
                new TicketCategory { Id = 5, Name = "Others" }
            };
            cmbCategory.DataSource = null;
            cmbCategory.DataSource = categories;
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";

            cmbStatus.Items.Clear();
            cmbStatus.Items.AddRange(new string[] { "New", "In Progress", "Resolved", "Closed" });
            cmbStatus.SelectedIndex = 0;

            var categories2 = _ticketService.GetCategories();

            categories.Insert(0, new TicketCategory { Id = 0, Name = "All" });

            cmbFilterCategory.DataSource = categories2;
            cmbFilterCategory.DisplayMember = "Name";
            cmbFilterCategory.ValueMember = "Id"; 
            cmbFilterCategory.SelectedIndex = 0;  

            cmbFilterStatus.Items.Clear();
            cmbFilterStatus.Items.Add("All");
            cmbFilterStatus.Items.Add("New");
            cmbFilterStatus.Items.Add("In Progress");
            cmbFilterStatus.Items.Add("Resolved");
            cmbFilterStatus.Items.Add("Closed");
            cmbFilterStatus.SelectedIndex = 0;

            RefreshGrid();
        }

        private void LoadDefaultValues()
        {
            cmbCategory.DataSource = _ticketCategoryRepository.GetAll();
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";

            cmbAssignedTo.DataSource = _employeeRepository.GetAll();
            cmbAssignedTo.DisplayMember = "FullName";
            cmbAssignedTo.ValueMember = "Id";

            cmbStatus.Items.AddRange(new string[] { "New", "In-Progress", "Resolved", "Closed" });
            cmbStatus.SelectedIndex = 0;
        }

        private void LoadTickets()
        {
            dgTickets.AutoGenerateColumns = true;
            dgTickets.DataSource = _ticketService.GetAll(null, null, null).ToList();
            dgTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgTickets.ReadOnly = true;
            dgTickets.AllowUserToAddRows = false;
        }

        private void btnCreateTicket_Click(object sender, EventArgs e)
        {
            Model.Ticket ticket = new Model.Ticket()
            {
                IssueTitle = txtIssueTitle.Text,
                Description = txtDescription.Text,
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = Convert.ToInt32(cmbAssignedTo.SelectedValue),
                Status = cmbStatus.Text
            };

            var result = _ticketService.Add(ticket);

            if (!result.isOk)
                MessageBox.Show(result.message);

            if (result.isOk)
            {
                MessageBox.Show(result.message);
                LoadDefaultValues();
                LoadTickets();
                return;
            }
        }

        private void btnUpdateTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                lblStatus.Text = "Please select a ticket!";
                return;
            }

            var selected = dgTickets.SelectedRows[0].DataBoundItem as HelpDesk.Model.Ticket;
            if (selected == null) return;

            string title = txtIssueTitle.Text.Trim();
            string description = txtDescription.Text.Trim();
            string resolution = txtResolution.Text.Trim();
            string status = cmbStatus.Text;

            if (string.IsNullOrWhiteSpace(title))
            {
                lblStatus.Text = "Issue Title cannot be empty!";
                return;
            }

            if (cmbCategory.SelectedIndex < 0)
            {
                lblStatus.Text = "Please select a category!";
                return;
            }

            if (!new[] { "New", "In Progress", "Resolved", "Closed" }.Contains(status))
            {
                lblStatus.Text = "Invalid status!";
                return;
            }

            if (status == "Resolved" || status == "Closed")
            {
                if ((status == "Resolved" || status == "Closed") && string.IsNullOrEmpty(resolution))
                {
                    lblStatus.Text = "Resolution notes are required!";
                    return;
                }


                if (cmbAssignedTo.SelectedIndex < 0)
                {
                    lblStatus.Text = "Assigned employee is required!";
                    return;
                }

                DateTime now = DateTime.Now;
                if (now < selected.DateCreated)
                {
                    lblStatus.Text = "Resolved date cannot be earlier than creation date!";
                    return;
                }

                selected.AssignedEmployeeId = (int)cmbAssignedTo.SelectedValue;
                selected.DateResolved = now;
            }
            else
            {
                selected.DateResolved = null;

                if (status == "New")
                {
                    txtResolution.Clear();
                    selected.ResolutionNotes = string.Empty;
                }
            }

            selected.IssueTitle = title;
            selected.Description = description;
            selected.CategoryId = (int)cmbCategory.SelectedValue;
            selected.Status = status;
            selected.ResolutionNotes = resolution;

            LoadTickets();
            LoadDefaultValues();
            RefreshGrid();
            ClearInputs();
            lblStatus.Text = "Ticket updated!";
        }

        private void dgTickets_SelectionChanged(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                lblStatus.Text = "No ticket selected.";
                return;
            }

            var selected = dgTickets.SelectedRows[0].DataBoundItem as HelpDesk.Model.Ticket;
            if (selected == null) return;

            string assigned = selected.AssignedEmployeeId != 0 ? $"Assigned to ID: {selected.AssignedEmployeeId}" : "Unassigned";
            string resolvedDate = selected.DateResolved.HasValue ? selected.DateResolved.Value.ToString("g") : "Not resolved";

            lblStatus.Text = $"Ticket: {selected.IssueTitle} | Status: {selected.Status} | {assigned} | Resolved: {resolvedDate}";
        }
        private void ClearInputs()
        {
            txtIssueTitle.Clear();
            txtDescription.Clear();
            txtResolution.Clear();

            cmbCategory.SelectedIndex = -1;
            cmbAssignedTo.SelectedIndex = -1;
            cmbStatus.SelectedIndex = 0;

            dtpDateCreated.Value = DateTime.Now;

            lblStatus.Text = "Ready";
        }
        private void RefreshGrid()
        {
            dgTickets.DataSource = null;
            dgTickets.DataSource = _ticketService.GetAll(null, null, null);
        }
        private void UpdateCounts()
        {
            lblCounts.Text = $"Total: {0} | Open: {1} | Closed: {2}";

        }

        private void btnDeleteTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                lblStatus.Text = "No ticket selected.";
                return;
            }

            var ticket = dgTickets.SelectedRows[0].DataBoundItem as HelpDesk.Model.Ticket;
            if (ticket == null) return;

            if (chkConfirmDelete.Checked)
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to delete this ticket?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            dgTickets.Rows.Remove(dgTickets.SelectedRows[0]);

            LoadTickets();
            LoadDefaultValues();
            RefreshGrid();
            ClearInputs();
            lblStatus.Text = "Ticket deleted!";
        }

        private void dgTickets_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            dgTickets.Rows[e.RowIndex].Selected = true;

            var selected = dgTickets.Rows[e.RowIndex].DataBoundItem as HelpDesk.Model.Ticket;
            if (selected == null) return;

            txtIssueTitle.Text = selected.IssueTitle;
            txtDescription.Text = selected.Description;
            cmbCategory.SelectedValue = selected.CategoryId;
            cmbAssignedTo.SelectedValue = selected.AssignedEmployeeId;
            cmbStatus.Text = selected.Status;
            txtResolution.Text = selected.ResolutionNotes;

            dtpDateCreated.Value = selected.DateCreated;
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            var allTickets = _ticketService.GetAll(null, null, null);

            if (dgTickets.Rows.Count == 0)
            {
                lblStatus.Text = "No tickets to clear.";
                return;
            }

            DialogResult result = MessageBox.Show(
                "Are you sure you want to delete ALL tickets?",
                "Confirm Clear All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            foreach (var ticket in allTickets)
            {
                _ticketService.Delete(ticket.Id);
            }

            LoadTickets();
            LoadDefaultValues();
            RefreshGrid();
            ClearInputs();
            lblStatus.Text = "All tickets have been deleted!";
        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            int? categoryFilter = null;
            if (cmbFilterCategory.SelectedValue != null)
            {
                int val = 0;
                if (int.TryParse(cmbFilterCategory.SelectedValue.ToString(), out val) && val != 0)
                    categoryFilter = val;
            }

            string statusFilter = null;
            if (cmbFilterStatus.SelectedIndex > 0) 
                statusFilter = cmbFilterStatus.Text;

            var filteredTickets = _ticketService.GetAll(statusFilter, categoryFilter, null);

            dgTickets.DataSource = null;
            dgTickets.DataSource = filteredTickets;
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            cmbFilterCategory.SelectedIndex = 0; 
            cmbFilterStatus.SelectedIndex = 0;   

            dgTickets.DataSource = _ticketService.GetAll(null, null, null);
            lblStatus.Text = "Filters reset. All tickets displayed.";
        }
    }
}
