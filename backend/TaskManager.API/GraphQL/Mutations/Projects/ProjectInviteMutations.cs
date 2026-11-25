using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using TaskManager.API.Extensions;
using TaskManager.API.Repositories;
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
            var inviterUserId = claimsPrincipal.GetUserId();

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
                throw new GraphQLException("Невірна роль у проєкті. Дозволені: manager або contributor.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, inviterUserId);
            if (!isUserInProject)
            {
                throw new GraphQLException("У вас немає прав запрошувати користувачів до цього проєкту.");
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
            var currentUserId = claimsPrincipal.GetUserId();

            var success = await projectRepository.AcceptProjectInvitationAsync(token, currentUserId);
            if (!success)
            {
                throw new GraphQLException("Запрошення недійсне або термін його дії минув.");
            }

            return true;
        }

        [Authorize]
        public async Task<bool> AddUserToProjectDirectlyAsync(
            Guid projectId,
            string email,
            string projectRole,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var inviterUserId = claimsPrincipal.GetUserId();

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
                throw new GraphQLException("Невірна роль у проєкті. Дозволені: manager або contributor.");
            }

            var currentUser = await userRepository.GetUserByIdAsync(inviterUserId);
            if (currentUser == null)
            {
                throw new GraphQLException("Поточного користувача не знайдено.");
            }

            var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, inviterUserId);
            if (!isUserInProject && !currentUser.IsAdmin)
            {
                throw new GraphQLException("Тільки адміністратор або учасник проєкту може додавати користувачів.");
            }

            return await projectRepository.InviteUserToProjectAsync(projectId, email, projectRole);
        }
    }
}