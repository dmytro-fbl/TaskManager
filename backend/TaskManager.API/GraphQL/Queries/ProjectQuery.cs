using HotChocolate.Authorization;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories.ProjectsRepository;
using System.Security.Claims;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class ProjectQuery
    {
        [Authorize]
        public async Task<IEnumerable<Project>> GetMyProjectsAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            return await projectRepository.GetUserProjectsAsync(userId);
        }

        [Authorize]
        public async Task<Project?> GetProjectAsync(
            Guid id,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(id, userId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає доступу до цього проекту.");
            }

            return await projectRepository.GetProjectByIdAsync(id);
        }

        [Authorize]
        public async Task<IEnumerable<User>> GetProjectMembersAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, userId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає доступу до цього проекту.");
            }

            return await projectRepository.GetProjectMembersAsync(projectId);
        }

        [Authorize]
        public async Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, userId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає доступу до цього проекту.");
            }

            return await projectRepository.GetProjectMembershipsAsync(projectId);
        }
    }
}
