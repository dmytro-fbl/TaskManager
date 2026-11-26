using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TaskManager.API.Extensions;
using TaskManager.API.Models.Constants;
using TaskManager.API.Repositories;
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
            [Service] IProjectMembershipRepository projectRepository,
            [Service] IUserRepository userRepository,
            ClaimsPrincipal claimsPrincipal)
        { 
            var currentUserId = claimsPrincipal.GetUserId();
            var currentUser = await userRepository.GetUserByIdAsync(currentUserId);

            if (currentUser == null)
                throw new GraphQLException("Даний користувач не дійсний");

            if (currentUserId == userId)
            {
                throw new GraphQLException("Ви не можете змінювати власну роль у проєкті.");
            }

            if (!ProjectRoles.Allowed.Contains(projectRole))
            {
                throw new GraphQLException("Невірна роль проєкту.");
            }

            bool hasAccess = currentUser.IsAdmin;

            if (!hasAccess)
            {
                var currentUserRole = await projectRepository.GetUserProjectRoleAsync(projectId, currentUserId);
                if (currentUserRole == ProjectRoles.Manager)
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                throw new GraphQLException("Тільки менеджер проєкту або системний адміністратор може змінювати ролі інших учасників.");
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
