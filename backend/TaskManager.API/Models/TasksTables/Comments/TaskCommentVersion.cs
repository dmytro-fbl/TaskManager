namespace TaskManager.API.Models.TasksTables.Comments
{
    public class TaskCommentVersion
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public string PreviousBody { get; set; } = string.Empty;
        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
