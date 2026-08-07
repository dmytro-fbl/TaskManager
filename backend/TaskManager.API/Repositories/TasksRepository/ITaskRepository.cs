using TaskManager.API.Models.TasksTables;

namespace TaskManager.API.Repositories.TasksRepository
{
    public interface ITaskRepository
    {
        Task<Guid> CreateTaskAsync(TaskItem task);

        Task<TaskItem?> GetTaskByIdAsync(Guid taskId);

        Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId);

        Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsAsync(Guid taskId);

        Task<bool> AddTaskAssignmentAsync(TaskAssignment assignment);

        Task<bool> RemoveTaskAssignmentAsync(Guid taskId, Guid userId);
        Task<bool> IsProjectStatusAsync(Guid projectId, Guid statusId);
    }
}
