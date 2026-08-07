using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Models.TasksTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class TaskMutations
    {
        [Authorize]
        public async Task<TaskItem> CreateTaskAsync(
            CreateTaskInput input,
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

            var project = await projectRepository.GetProjectByIdAsync(
                input.ProjectId
            );

            if (project == null)
            {
                throw new GraphQLException("Проєкт не знайдено.");
            }

            var hasProjectAccess = currentUser.IsAdmin ||
                await projectRepository.IsUserInProjectAsync(
                    input.ProjectId,
                    userId
                );

            if (!hasProjectAccess)
            {
                throw new GraphQLException(
                    "У вас немає доступу до цього проєкту."
                );
            }

            if (string.IsNullOrWhiteSpace(input.Title) ||
                input.Title.Trim().Length < 2)
            {
                throw new GraphQLException(
                    "Назва таски має містити щонайменше 2 символи."
                );
            }

            var allowedPriorities = new[]
            {
                "low",
                "medium",
                "high",
                "critical"
            };

            var priority = input.Priority.Trim().ToLowerInvariant();

            if (!allowedPriorities.Contains(priority))
            {
                throw new GraphQLException("Невірний пріоритет таски.");
            }

            if (input.StartDate.HasValue &&
                input.DueDate.HasValue &&
                input.DueDate < input.StartDate)
            {
                throw new GraphQLException(
                    "Дата завершення не може бути раніше дати початку."
                );
            }

            var statusExists = await taskRepository.IsProjectStatusAsync(
                input.ProjectId,
                input.StatusId
            );

            if (!statusExists)
            {
                throw new GraphQLException(
                    "Вибраний статус не належить цьому проєкту."
                );
            }

            if (input.EstimatedBudget is < 0)
            {
                throw new GraphQLException(
                    "Орієнтовний бюджет не може бути від'ємним."
                );
            }

            var task = new TaskItem
            {
                ProjectId = input.ProjectId,
                AuthorId = userId,
                StatusId = input.StatusId,
                Title = input.Title.Trim(),
                Notes = input.Notes?.Trim(),
                Priority = priority,
                StartDate = input.StartDate,
                DueDate = input.DueDate,
                EstimatedBudget = input.EstimatedBudget,
                EstimatedUnit = input.EstimatedUnit?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var taskId = await taskRepository.CreateTaskAsync(task);
            var createdTask = await taskRepository.GetTaskByIdAsync(taskId);

            return createdTask
                ?? throw new GraphQLException(
                    "Таску створено, але не вдалося її отримати."
                );
        }

        [Authorize]
        public async Task<TaskItem> UpdateTaskStatusAsync(
            Guid taskId,
            Guid statusId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var currentUserId = GetCurrentUserId(claimsPrincipal);

            var currentUser = await userRepository.GetUserByIdAsync(
                currentUserId
            );

            if (currentUser == null)
            {
                throw new GraphQLException(
                    "Користувача не знайдено."
                );
            }

            var task = await taskRepository.GetTaskByIdAsync(taskId);

            if (task == null)
            {
                throw new GraphQLException(
                    "Таску не знайдено."
                );
            }

            var hasAccess = currentUser.IsAdmin ||
                await projectRepository.IsUserInProjectAsync(
                    task.ProjectId,
                    currentUserId
                );

            if (!hasAccess)
            {
                throw new GraphQLException(
                    "У вас немає доступу до цієї таски."
                );
            }

            var statusExists = await taskRepository.IsProjectStatusAsync(
                task.ProjectId,
                statusId
            );

            if (!statusExists)
            {
                throw new GraphQLException(
                    "Вибраний статус не належить цьому проєкту."
                );
            }

            var updated = await taskRepository.UpdateTaskStatusAsync(
                taskId,
                statusId
            );

            if (!updated)
            {
                throw new GraphQLException(
                    "Не вдалося оновити статус таски."
                );
            }

            return await taskRepository.GetTaskByIdAsync(taskId)
                ?? throw new GraphQLException(
                    "Статус оновлено, але таску не вдалося отримати."
                );
        }
        private static Guid GetCurrentUserId(
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdValue =
                claimsPrincipal.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value ??
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