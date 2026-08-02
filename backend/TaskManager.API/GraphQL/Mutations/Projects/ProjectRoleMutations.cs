using HotChocolate.Authorization;
using System.Security.Claims;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class ProjectRoleMutations
    {
        [Authorize]
        public async Task<bool> UpdateProjectMemberRoleAsync(
            Guid projectId,
            Guid userId,
            string projectRole,
            Guid? roleLabelId,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        { 
            var currentUserIdStr = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value?? claimsPrincipal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            if (currentUserId == userId)
            {
                throw new GraphQLException("Ви не можете змінювати власну роль у проєкті.");
            }

            if (projectRole is not ("manager" or "contributor"))
            {
                throw new GraphQLException("Невірна роль проєкту.");
            }

            var success = await projectRepository.UpdateProjectMembershipRoleAsync(
                projectId,
                userId,
                projectRole,
                roleLabelId,
                currentUserId
            );

            if (!success)
            {
                throw new GraphQLException("Не вдалося оновити роль користувача в проекті.");
            }

            return true;

        }
    }
}
