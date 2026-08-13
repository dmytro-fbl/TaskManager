namespace TaskManager.API.DTOs.Tasks
{
    public class WorkLogInput
    {
        public Guid TaskId { get; set; }
        public Guid RoleLabelId { get; set; }
        public decimal TimeSpentHours { get; set; }
        public string? Comment { get; set; }
        
    }
}
