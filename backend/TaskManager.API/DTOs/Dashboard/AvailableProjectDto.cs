namespace TaskManager.API.DTOs.Dashboard
{
    public class AvailableProjectDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string ManagerEmail { get; set; } = string.Empty;
    }
}