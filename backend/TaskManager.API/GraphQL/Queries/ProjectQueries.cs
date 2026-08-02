using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class ProjectQueries
    {
        [Authorize]
        public async Task<IEnumerable<AdminProjectDto>> GetAdminProjectsAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository, 
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);
                if (currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("Доступ заборонено.");
                }
                return await projectRepository.GetAllProjectAsync();
            }
            throw new GraphQLException("Неавторизований запит");
        }
    }
}
