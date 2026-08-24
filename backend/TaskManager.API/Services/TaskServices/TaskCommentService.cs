using HotChocolate;
using TaskManager.API.DTOs.Tasks.Comments;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.CommentsRepository;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;
using TaskManager.API.Services.TaskServices;

namespace TaskManager.API.Services
{
    public class TaskCommentService : ITaskCommentService
    {
        private readonly ITaskCommentsRepository _commentRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;

        public TaskCommentService(
            ITaskCommentsRepository commentRepository,
            ITaskRepository taskRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository)
        {
            _commentRepository = commentRepository;
            _taskRepository = taskRepository;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        private async Task CheckProjectAccessAsync(Guid taskId, Guid userId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null) throw new GraphQLException("Таску не знайдено.");

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) throw new GraphQLException("Користувача не знайдено.");

            var hasAccess = user.IsAdmin || await _projectRepository.IsUserInProjectAsync(task.ProjectId, userId);

            if (!hasAccess)
                throw new GraphQLException("У вас немає доступу до цього проєкту.");
        }

        public async Task<IEnumerable<TaskComment>> GetCommentsByTaskIdAsync(Guid taskId, Guid userId)
        {
            await CheckProjectAccessAsync(taskId, userId);
            return await _commentRepository.GetByTaskIdAsync(taskId);
        }

        public async Task<IEnumerable<TaskCommentVersion>> GetVersionsAsync(Guid commentId, Guid userId)
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null) throw new GraphQLException("Коментар не знайдено.");

            await CheckProjectAccessAsync(comment.TaskId, userId);
            return await _commentRepository.GetVersionsAsync(commentId);
        }

        public async Task<TaskComment> CreateCommentAsync(CreateTaskCommentInput input, Guid userId)
        {
            await CheckProjectAccessAsync(input.TaskId, userId);

            if (string.IsNullOrWhiteSpace(input.Body))
                throw new GraphQLException("Коментар не може бути порожнім.");

            if (input.ParentCommentId.HasValue)
            {
                var parent = await _commentRepository.GetByIdAsync(input.ParentCommentId.Value);
                if (parent == null || parent.TaskId != input.TaskId)
                    throw new GraphQLException("Батьківський коментар не знайдено або він належить іншій тасці.");
            }

            var newComment = new TaskComment
            {
                TaskId = input.TaskId,
                AuthorId = userId,
                ParentCommentId = input.ParentCommentId,
                Body = input.Body.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                IsEdited = false,
                IsDeleted = false
            };

            var commentId = await _commentRepository.CreateAsync(newComment);
            return await _commentRepository.GetByIdAsync(commentId)
                   ?? throw new GraphQLException("Помилка при отриманні створеного коментаря.");
        }

        public async Task<TaskComment> UpdateCommentAsync(UpdateTaskCommentInput input, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(input.Body))
                throw new GraphQLException("Коментар не може бути порожнім.");

            var comment = await _commentRepository.GetByIdAsync(input.CommentId);
            if (comment == null) throw new GraphQLException("Коментар не знайдено.");
            if (comment.IsDeleted) throw new GraphQLException("Не можна редагувати видалений коментар.");

            var user = await _userRepository.GetUserByIdAsync(userId);

            if (comment.AuthorId != userId && (user == null || !user.IsAdmin))
                throw new GraphQLException("У вас немає прав для редагування цього коментаря.");

            await _commentRepository.UpdateAsync(input.CommentId, input.Body.Trim());

            return await _commentRepository.GetByIdAsync(input.CommentId)
                   ?? throw new GraphQLException("Помилка при отриманні оновленого коментаря.");
        }

        public async Task DeleteCommentAsync(DeleteTaskCommentInput input, Guid userId)
        {
            var comment = await _commentRepository.GetByIdAsync(input.CommentId);
            if (comment == null) throw new GraphQLException("Коментар не знайдено.");
            if (comment.IsDeleted) throw new GraphQLException("Коментар вже видалено.");

            var user = await _userRepository.GetUserByIdAsync(userId);

            if (comment.AuthorId != userId && (user == null || !user.IsAdmin))
                throw new GraphQLException("У вас немає прав для видалення цього коментаря.");

            await _commentRepository.DeleteAsync(input.CommentId);
        }
    }
}