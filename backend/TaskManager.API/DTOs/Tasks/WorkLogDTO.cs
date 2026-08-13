namespace TaskManager.API.DTOs.Tasks
{
    public class WorkLogDTO
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid RoleLabelId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public decimal HoursSpent { get; set; }
        public DateTimeOffset LogDate { get; set; }
        public string? Comment { get; set; } = string.Empty;
    }
}
