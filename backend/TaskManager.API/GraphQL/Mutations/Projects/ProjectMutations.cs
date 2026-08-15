using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Projects;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class ProjectMutations
    {
        [Authorize]
        public async Task<Project> CreateProjectAsync(
            CreateProjectInput input,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? claimsPrincipal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdClaim, out var ownerId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача. Увійдіть знову.");
            }

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                throw new GraphQLException("Назва проекту не може бути порожньою.");
            }

            if (input.Deadline.Date < DateTimeOffset.UtcNow.Date)
            {
                throw new GraphQLException("Дедлайн не може бути встановлений у минулому.");
            }

            var newProject = new Project
            {
                Title = input.Title.Trim(),
                Description = input.Description?.Trim() ?? string.Empty,
                BudgetCap = input.BudgetCap,
                Status = "active",
                OwnerId = ownerId,
                IsArchived = false,
                Deadline = input.Deadline,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            

            var createdProjectId = await projectRepository.CreateProjectWithOwnerAsync(newProject);

            var createdProject = await projectRepository.GetProjectByIdAsync(createdProjectId);

            await projectRepository.AddDefaultProjectRolesAsync(createdProjectId);

            return createdProject
                ?? throw new GraphQLException("Помилка під час зчитування створеного проекту.");
        }

        [Authorize]
        public async Task<bool> ToggleProjectIsArchivedAsync(
            Guid projectId,
            bool isArchived,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var currentUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(currentUserId, out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);
                if (currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("доступ заборонено!");
                }

                var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
                if (currentProject == null)
                {
                    throw new GraphQLException("Даний проект не знайдено.");
                }
                return await projectRepository.ToggleArchiveProjectAsync(projectId, isArchived);
            }

            throw new GraphQLException("Помилка авторизації");
        }


        [Authorize]
        public async Task<bool> UpdateProjectAsync(
            Guid projectId,
            string title,
            string? description,
            decimal? budgetCap,
            DateTimeOffset deadline,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var currentUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst("sub")?.Value;

            if(Guid.TryParse(currentUserId,out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);
                if (currentUser == null)
                    throw new GraphQLException("Даний користувач не дійсний");
                
                var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
                if (currentProject == null || currentProject.Status == "archived")
                    throw new GraphQLException("Даний проект архівований або його не існує");

                var userRole = await projectRepository.GetUserProjectRoleAsync(projectId, userId);

                bool hasAccess = currentUser.IsAdmin || currentProject.OwnerId == userId || userRole == "manager";

                if (!hasAccess)
                    throw new GraphQLException("У вас немає прав на редагування цього проекту.");

                return await projectRepository.UpdateProjectAsync(projectId, title, description, budgetCap, deadline);
            }
            throw new GraphQLException("Помилка авторизації.");
        }

        [Authorize]
        public async Task<bool> RemoveProjectMemberAsync(
            Guid projectId,
            Guid memberUserId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository,
            [Service] ITaskRepository taskRepository)
        {
            var currentUserIdStr = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(currentUserId);
                if (currentUser == null)
                    throw new GraphQLException("Даний користувач не дійсний");

                var currentProject = await projectRepository.GetProjectByIdAsync(projectId);

                if (currentProject == null || currentProject.Status == "archived" || currentProject.IsArchived)
                    throw new GraphQLException("Даний проект архівований або його не існує");

                if (currentProject.OwnerId == memberUserId)
                    throw new GraphQLException("Неможливо вилучити власника проєкту з команди.");

                var userRole = await projectRepository.GetUserProjectRoleAsync(projectId, currentUserId);
                bool hasAccess = currentUser.IsAdmin || currentProject.OwnerId == currentUserId || userRole == "manager";

                if (!hasAccess)
                    throw new GraphQLException("У вас немає прав для видалення учасників з цього проєкту.");

                bool hasTasks = await taskRepository.HasUserTasksInProjectAsync(projectId, memberUserId);
                if (hasTasks)
                    throw new GraphQLException("Неможливо вилучити користувача, оскільки на нього призначені завдання або він є їх автором у цьому проєкті.");

                return await projectRepository.RemoveMemberAsync(projectId, memberUserId);
            }

            throw new GraphQLException("Помилка авторизації.");
        }

        [Authorize]
        public async Task<ProjectRoleDTO> CreateProjectRoleAsync(
            Guid projectId,
            string name,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
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
            var currentUser = await userRepository.GetUserByIdAsync(userId);
            if (currentUser == null)
                throw new GraphQLException("Даний користувач не дійсний");

            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            {
                throw new GraphQLException("Назва ролі має містити щонайменше 2 символи.");
            }

            var membership = await projectRepository.GetUserProjectRoleAsync(projectId, userId);
            var isManager = membership != null && membership == "manager";

            if (!currentUser.IsAdmin && !isManager)
            {
                throw new GraphQLException("Тільки менеджер проєкту може створювати нові ролі.");
            }

            try
            {
                return await projectRepository.CreateProjectRoleAsync(projectId, name);
            }
            catch
            {
                throw new GraphQLException("Плмилка авторизації.");
            }
        }
    }
}
