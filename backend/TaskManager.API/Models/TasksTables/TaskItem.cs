namespace TaskManager.API.Models.TasksTables
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }

        public Guid AuthorId { get; set; }

        public Guid? AssigneeId { get; set; }

        public Guid StatusId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string Priority { get; set; } = "medium";

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? EstimatedBudget { get; set; }

        public string? EstimatedUnit { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? CompletedAt { get; set; }
    }
}
