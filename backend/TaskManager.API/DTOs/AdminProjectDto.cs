namespace TaskManager.API.DTOs
{
    public class AdminProjectDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } 
        public decimal? BudgetCap { get; set; }
        public string Status { get; set; } = "active";
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;
        public DateTimeOffset? Deadline { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
