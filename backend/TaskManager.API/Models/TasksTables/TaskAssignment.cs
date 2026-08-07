namespace TaskManager.API.Models.TasksTables
{
    public class TaskAssignment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public decimal EstimatedHours { get; set; }
        public Guid AssignedBy { get; set; }
        public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
