namespace TaskManager.API.Models.TasksTables.Comments
{
    public class TaskComment
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid AuthorId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

    }
}
