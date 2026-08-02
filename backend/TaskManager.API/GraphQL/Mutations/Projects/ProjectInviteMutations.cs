using HotChocolate.Authorization;
using TaskManager.API.Repositories.ProjectsRepository;
using System.Security.Claims;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class ProjectInviteMutations
    {
        [Authorize]
        public async Task<bool> InviteUserToProjectAsync(
            Guid projectId,
            string email,
            string projectRole,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            // Check if user is project owner or manager
            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, userId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає прав для додавання користувачів до цього проекту.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new GraphQLException("Email не може бути порожнім.");
            }

            if (string.IsNullOrWhiteSpace(projectRole))
            {
                projectRole = "member";
            }

            return await projectRepository.InviteUserToProjectAsync(projectId, email.ToLower().Trim(), projectRole);
        }
    }
}
