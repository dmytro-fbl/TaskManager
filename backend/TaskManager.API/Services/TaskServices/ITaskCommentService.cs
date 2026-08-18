using TaskManager.API.DTOs.Tasks.Comments;
using TaskManager.API.Models.TasksTables.Comments;

namespace TaskManager.API.Services.TaskServices
{
    public interface ITaskCommentService
    {
        Task<IEnumerable<TaskComment>> GetCommentsByTaskIdAsync(Guid taskId, Guid userId);
        Task<IEnumerable<TaskCommentVersion>> GetVersionsAsync(Guid commentId, Guid userId);
        Task<TaskComment> CreateCommentAsync(CreateTaskCommentInput input, Guid userId);
        Task<TaskComment> UpdateCommentAsync(UpdateTaskCommentInput input, Guid userId);
        Task DeleteCommentAsync(DeleteTaskCommentInput input, Guid userId);
    }
}
