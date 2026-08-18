namespace TaskManager.API.DTOs.Tasks.Comments
{
    public class CreateTaskCommentInput
    {
        public Guid TaskId { get; set; }
        public Guid? ParentCommentId { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
