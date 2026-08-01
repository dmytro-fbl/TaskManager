using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Models.ProjectsTables;
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
            if(!Guid.TryParse(userIdClaim, out var ownerId))
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
    }
}
