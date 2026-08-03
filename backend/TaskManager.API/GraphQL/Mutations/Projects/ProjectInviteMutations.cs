using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class ProjectInviteMutations
    {
        [Authorize]
        public async Task<bool> InviteExistingUserToProjectAsync(
            Guid projectId,
            string email,
            string projectRole,
            Guid? roleLabelId,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var inviterUserId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new GraphQLException("Email не може бути порожнім.");
            }

            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(projectRole))
            {
                projectRole = "contributor";
            }

            if (projectRole is not ("manager" or "contributor"))
            {
                throw new GraphQLException("Невірна роль проекту.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, inviterUserId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає прав для додавання користувачів до цього проекту.");
            }

            var result = await projectRepository.CreateProjectInvitationForExistingUserAsync(
                projectId,
                email,
                projectRole,
                inviterUserId,
                roleLabelId
            );

            if (!result)
            {
                throw new GraphQLException("Не вдалося створити запрошення.");
            }

            return true;
        }

        [Authorize]
        public async Task<bool> AcceptProjectInviteAsync(
            string token,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var currentUserId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            var success = await projectRepository.AcceptProjectInvitationAsync(token, currentUserId);
            if (!success)
            {
                throw new GraphQLException("Запрошення недійсне або протерміноване.");
            }

            return true;
        }

        [Authorize]
        public async Task<bool> AddUserToProjectDirectlyAsync(
            Guid projectId,
            string email,
            string projectRole,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var inviterUserId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new GraphQLException("Email не може бути порожнім.");
            }

            email = email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(projectRole))
            {
                projectRole = "contributor";
            }

            if (projectRole is not ("manager" or "contributor"))
            {
                throw new GraphQLException("Невірна роль проєкту.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, inviterUserId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає прав для додавання користувачів до цього проєкту.");
            }

            var result = await projectRepository.InviteUserToProjectAsync(projectId, email, projectRole);

            return result;


        }
    }
}