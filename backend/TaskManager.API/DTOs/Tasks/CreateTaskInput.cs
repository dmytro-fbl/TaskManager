namespace TaskManager.API.DTOs.Tasks
{
    public class CreateTaskInput
    {
        public Guid RoleId { get; set; }
        public Guid ProjectId { get; set; }

        public Guid StatusId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string Priority { get; set; } = "medium";

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? EstimatedBudget { get; set; }

        public string? EstimatedUnit { get; set; }
    }
}