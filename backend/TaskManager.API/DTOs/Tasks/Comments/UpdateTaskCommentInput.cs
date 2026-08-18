namespace TaskManager.API.DTOs.Tasks.Comments
{
    public class UpdateTaskCommentInput
    {
        public Guid CommentId { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}
