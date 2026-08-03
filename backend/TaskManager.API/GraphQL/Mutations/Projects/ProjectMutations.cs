using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

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

            var newProject = new Project
            {
                Title = input.Title.Trim(),
                Description = input.Description?.Trim() ?? string.Empty,
                BudgetCap = input.BudgetCap,
                Status = "active",
                OwnerId = ownerId,
                IsArchived = false,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createdProjectId = await projectRepository.CreateProjectWithOwnerAsync(newProject);

            var createdProject = await projectRepository.GetProjectByIdAsync(createdProjectId);

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
                    throw new GraphQLException("Даний gроект не знайдено.");
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
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var currentUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(Guid.TryParse(currentUserId,out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);
                if (currentUser == null)
                    throw new GraphQLException("Даний користувач не дійсний");

                var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
                if (currentProject == null || currentProject.Status == "archived")
                    throw new GraphQLException("Даний проект архівований або його не існує");

                if (currentProject.OwnerId != currentUser.Id && !currentUser.IsAdmin)
                    throw new GraphQLException("Доступ оновлення заблоковано");

                return await projectRepository.UpdateProjectAsync(projectId, title, description, budgetCap);
            }
            throw new GraphQLException("Помилка авторизації.");
        }
    }
}
