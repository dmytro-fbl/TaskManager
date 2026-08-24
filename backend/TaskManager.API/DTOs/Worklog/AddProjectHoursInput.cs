namespace TaskManager.API.DTOs.Worklog
{
    public class AddProjectHoursInput
    {
        public Guid ProjectId { get; set; }
        public decimal Hours { get; set; }
        public string? Description { get; set; }
    }
}