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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDefaultValues();
            LoadTickets();
            dgTickets.SelectionChanged += dgTickets_SelectionChanged;
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
                MessageBox.Show("Please select a ticket to update.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ticket = dgTickets.SelectedRows[0].DataBoundItem as HelpDesk.Model.Ticket;
            if (ticket == null)
            {
                MessageBox.Show("Selected ticket is invalid.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIssueTitle.Text))
            {
                MessageBox.Show("Issue Title cannot be empty.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid category.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbAssignedTo.SelectedItem == null)
            {
                MessageBox.Show("Please select an assigned employee.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbStatus.SelectedItem == null)
            {
                MessageBox.Show("Please select a status.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedStatus = cmbStatus.SelectedItem.ToString();

            if ((selectedStatus == "Resolved" || selectedStatus == "Closed") &&
                string.IsNullOrWhiteSpace(txtResolution.Text))
            {
                MessageBox.Show("Resolution is required when status is Resolved or Closed.",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ticket.IssueTitle = txtIssueTitle.Text.Trim();
            ticket.Description = txtDescription.Text.Trim();
            ticket.CategoryId = ((TicketCategory)cmbCategory.SelectedItem).Id;
            ticket.AssignedEmployeeId = ((Employee)cmbAssignedTo.SelectedItem).Id; // never null
            ticket.Status = selectedStatus;
            ticket.ResolutionNotes = txtResolution.Text.Trim();
            ticket.DateResolved = (selectedStatus == "Resolved" || selectedStatus == "Closed")
                ? DateTime.Now
                : (DateTime?)null;

            _ticketService.Update(ticket); 

            LoadTickets();
            lblStatus.Text = $"Ticket #{ticket.Id} updated successfully.";
            MessageBox.Show("Ticket updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgTickets_SelectionChanged(object sender, EventArgs e)
        {
            if (dgTickets.SelectedRows.Count == 0)
            {
                txtIssueTitle.Clear();
                txtDescription.Clear();
                cmbCategory.SelectedItem = null;
                cmbAssignedTo.SelectedItem = null;
                cmbStatus.SelectedItem = null;
                txtResolution.Clear();
                return;
            }

            var selectedTicket = dgTickets.SelectedRows[0].DataBoundItem as HelpDesk.Model.Ticket;
            if (selectedTicket == null) return;

            txtIssueTitle.Text = selectedTicket.IssueTitle;
            txtDescription.Text = selectedTicket.Description;

            if (selectedTicket.Category != null)
            {
                foreach (var item in cmbCategory.Items)
                {
                    if (item is TicketCategory category && category.Id == selectedTicket.Category.Id)
                    {
                        cmbCategory.SelectedItem = item;
                        break;
                    }
                }
            }

            if (selectedTicket.AssignedEmployee != null)
            {
                foreach (var item in cmbAssignedTo.Items)
                {
                    if (item is Employee emp && emp.Id == selectedTicket.AssignedEmployee.Id)
                    {
                        cmbAssignedTo.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                cmbAssignedTo.SelectedItem = null;
            }

            cmbStatus.SelectedItem = selectedTicket.Status;
            txtResolution.Text = selectedTicket.ResolutionNotes;
        }
    }
}
