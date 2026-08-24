namespace TaskManager.API.DTOs.Dashboard
{
    public class MyProjectDashboardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? BudgetHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MyRole { get; set; } = string.Empty;
        public decimal UsedHours { get; set; }
    }
}