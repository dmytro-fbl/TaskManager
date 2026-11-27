using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.Extensions;
using TaskManager.API.Models.Constants;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;
using TaskManager.API.Services;

namespace TaskManager.API.GraphQL.Mutations.Tasks
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class TaskAssignmentMutations
    {
        [Authorize]
        public async Task<bool> AssignUserToTaskAsync(
            Guid taskId,
            Guid userId,
            decimal estimatedHours,
            Guid? roleId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectMembershipRepository projectRepository,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            var currentUserId = claimsPrincipal.GetUserId();

            if (estimatedHours <= 0)
            {
                throw new GraphQLException(
                    "Кількість годин має бути більшою за нуль."
                );
            }

            var task = await taskRepository.GetTaskByIdAsync(taskId);

            if (task == null)
            {
                throw new GraphQLException("Таску не знайдено.");
            }

            await accessService.EnsureProjectNotArchivedAsync(task.ProjectId);

            var currentUser = await userRepository.GetUserByIdAsync(
                currentUserId
            );

            if (currentUser == null)
            {
                throw new GraphQLException("Поточного користувача не знайдено.");
            }

            var currentUserRole = await projectRepository.GetUserProjectRoleAsync(
                task.ProjectId,
                currentUserId
            );

            var canManageAssignments =
                currentUser.IsAdmin ||
                currentUserRole == ProjectRoles.Manager;

            if (!canManageAssignments)
            {
                throw new GraphQLException(
                    "Тільки менеджер або адміністратор може призначати користувачів."
                );
            }

            var assignedUser = await userRepository.GetUserByIdAsync(userId);

            if (assignedUser == null || !assignedUser.IsActive)
            {
                throw new GraphQLException(
                    "Користувача не знайдено або він неактивний."
                );
            }

            var isAssignedUserInProject =
                await projectRepository.IsUserInProjectAsync(
                    task.ProjectId,
                    userId
                );

            if (!isAssignedUserInProject)
            {
                throw new GraphQLException(
                    "Користувач не є учасником цього проєкту."
                );
            }

            if (roleId.HasValue)
            {
                var isValidRole = await taskRepository.IsRoleInProjectAsync(task.ProjectId, roleId.Value);
                if (!isValidRole) throw new GraphQLException("Вказана роль не належить цьому проєкту.");
            }

            var assignment = new Models.TasksTables.TaskAssignment
            {
                TaskId = taskId,
                UserId = userId,
                RoleId = roleId,
                EstimatedHours = estimatedHours,
                AssignedBy = currentUserId,
                AssignedAt = DateTimeOffset.UtcNow
            };

            var created = await taskRepository.AddTaskAssignmentAsync(
                assignment
            );

            if (!created)
            {
                throw new GraphQLException(
                    "Цей користувач уже призначений на цю таску."
                );
            }

            return true;
        }

        [Authorize]
        public async Task<bool> UpdateTaskAssignmentAsync(
            Guid taskId,
            Guid userId,
            decimal estimatedHours,
            Guid? roleId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectMembershipRepository projectRepository,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            if (estimatedHours <= 0)
            {
                throw new GraphQLException("Кількість годин має бути більшою за нуль.");
            }

            var currentUserId = claimsPrincipal.GetUserId();
            var task = await taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                throw new GraphQLException("Таску не знайдено.");
            }

            await accessService.EnsureProjectNotArchivedAsync(task.ProjectId);

            var currentUser = await userRepository.GetUserByIdAsync(currentUserId);
            if (currentUser == null)
            {
                throw new GraphQLException("Поточного користувача не знайдено.");
            }

            var currentUserRole = await projectRepository.GetUserProjectRoleAsync(task.ProjectId, currentUserId);
            if (!currentUser.IsAdmin && currentUserRole != ProjectRoles.Manager)
            {
                throw new GraphQLException("Тільки менеджер або адміністратор може редагувати призначення.");
            }

            if (roleId.HasValue)
            {
                var isValidRole = await taskRepository.IsRoleInProjectAsync(task.ProjectId, roleId.Value);
                if (!isValidRole)
                {
                    throw new GraphQLException("Вказана роль не належить цьому проєкту.");
                }
            }

            var updated = await taskRepository.UpdateTaskAssignmentAsync(taskId, userId, roleId, estimatedHours);
            if (!updated)
            {
                throw new GraphQLException("Призначення користувача на цю таску не знайдено.");
            }

            return true;
        }

        [Authorize]
        public async Task<bool> RemoveUserFromTaskAsync(
            Guid taskId,
            Guid userId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskRepository taskRepository,
            [Service] IProjectMembershipRepository projectRepository,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            var currentUserId = claimsPrincipal.GetUserId();

            var task = await taskRepository.GetTaskByIdAsync(taskId);

            if (task == null)
            {
                throw new GraphQLException("Таску не знайдено.");
            }

            await accessService.EnsureProjectNotArchivedAsync(task.ProjectId);

            var currentUser = await userRepository.GetUserByIdAsync(
                currentUserId
            );

            if (currentUser == null)
            {
                throw new GraphQLException("Поточного користувача не знайдено.");
            }

            var currentUserRole = await projectRepository.GetUserProjectRoleAsync(
                task.ProjectId,
                currentUserId
            );

            var canManageAssignments =
                currentUser.IsAdmin ||
                currentUserRole == ProjectRoles.Manager;

            if (!canManageAssignments)
            {
                throw new GraphQLException(
                    "Тільки менеджер або адміністратор може видаляти призначення."
                );
            }

            var removed = await taskRepository.RemoveTaskAssignmentAsync(
                taskId,
                userId
            );

            if (!removed)
            {
                throw new GraphQLException(
                    "Призначення користувача не знайдено."
                );
            }

            return true;
        }
    }
}