namespace TaskManager.API.DTOs.Tasks
{
    public class UpdateWorkLogInput
    {
        public Guid WorkLogId { get; set; }
        public decimal HoursSpent { get; set; }
        public string? Comment { get; set; }
    }
}
