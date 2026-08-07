using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.Models.TasksTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class TaskQuery
    {
        [Authorize]
        public async Task<IEnumerable<TaskItem>> GetProjectTasksAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            var currentUser = await userRepository.GetUserByIdAsync(userId);

            if (currentUser == null)
            {
                throw new GraphQLException("Користувача не знайдено.");
            }

            var hasAccess = currentUser.IsAdmin ||
                            await projectRepository.IsUserInProjectAsync(
                                projectId,
                                userId
                            );

            if (!hasAccess)
            {
                throw new GraphQLException(
                    "У вас немає доступу до цього проєкту."
                );
            }

            return await taskRepository.GetProjectTasksAsync(projectId);
        }

        [Authorize]
        public async Task<TaskItem?> GetTaskAsync(
            Guid id,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var task = await taskRepository.GetTaskByIdAsync(id);

            if (task == null)
            {
                return null;
            }

            var userId = GetCurrentUserId(claimsPrincipal);
            var currentUser = await userRepository.GetUserByIdAsync(userId);

            if (currentUser == null)
            {
                throw new GraphQLException("Користувача не знайдено.");
            }

            var hasAccess = currentUser.IsAdmin ||
                            await projectRepository.IsUserInProjectAsync(
                                task.ProjectId,
                                userId
                            );

            if (!hasAccess)
            {
                throw new GraphQLException(
                    "У вас немає доступу до цієї таски."
                );
            }

            return task;
        }

        private static Guid GetCurrentUserId(
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdValue =
                claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new GraphQLException(
                    "Не вдалося авторизувати користувача."
                );
            }

            return userId;
        }
    }
}