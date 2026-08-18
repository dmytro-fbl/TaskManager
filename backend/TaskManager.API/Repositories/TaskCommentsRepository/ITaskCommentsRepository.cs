using TaskManager.API.Models.TasksTables.Comments;

namespace TaskManager.API.Repositories.CommentsRepository
{
    public interface ITaskCommentsRepository
    {
        Task<IEnumerable<TaskComment>> GetByTaskIdAsync(Guid taskId);
        Task<TaskComment> GetByIdAsync(Guid commentId);
        Task<Guid> CreateAsync(TaskComment comment);
        Task UpdateAsync(Guid commentId, string newBody);
        Task DeleteAsync(Guid commentId);
        Task<IEnumerable<TaskCommentVersion>> GetVersionsAsync(Guid commentId);
    }
}
