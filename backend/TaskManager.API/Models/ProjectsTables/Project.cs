namespace TaskManager.API.Models.ProjectsTables
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? BudgetCap { get; set; }
        public string Status { get; set; } = "active";
        public Guid OwnerId { get; set; }
        public bool IsArchived { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; }

    }
}
