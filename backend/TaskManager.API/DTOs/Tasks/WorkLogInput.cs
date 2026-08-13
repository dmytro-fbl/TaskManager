namespace TaskManager.API.DTOs.Tasks
{
    public class WorkLogInput
    {
        public Guid TaskId { get; set; }
        public decimal HoursSpent { get; set; }
        public string? Comment { get; set; }
        
    }
}
