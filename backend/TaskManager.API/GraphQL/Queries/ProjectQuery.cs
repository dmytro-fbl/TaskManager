using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

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
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdString, out var userId))
            {

                var isUserInProject = await projectRepository.IsUserInProjectAsync(id, userId);
                var currentUser = await userRepository.GetUserByIdAsync(userId);
                if (currentUser == null)
                {
                    throw new GraphQLException("Даного користувача не існує");
                }


                //if (!isUserInProject && !currentUser.IsAdmin)
                //{
                //    throw new GraphQLException("У вас немає доступу до цього проекту.");
                //}


                return await projectRepository.GetProjectByIdAsync(id);
            }
            throw new GraphQLException("Не вдалось авторизувати користувача.");


        }

        [Authorize]
        public async Task<IEnumerable<User>> GetProjectMembersAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdString, out var userId))
            {

                var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, userId);
                var currentUser = await userRepository.GetUserByIdAsync(userId);

                if (currentUser == null)
                {
                    throw new GraphQLException("Даного користувача не існує");
                }

                //if (!isUserInProject && !currentUser.IsAdmin)
                //{
                //    throw new GraphQLException("У вас немає доступу до цього проекту.");
                //}

                return await projectRepository.GetProjectMembersAsync(projectId);
            }
            throw new GraphQLException("Не вдалось авторизувати користувача.");
        }

        [Authorize]
        public async Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdString, out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);

                if (currentUser == null)
                {
                    throw new GraphQLException("Даного користувача не існує");
                }
                var isUserInProject = await projectRepository.IsUserInProjectAsync(projectId, userId);
                if (!isUserInProject && !currentUser.IsAdmin)
                {
                    throw new GraphQLException("У вас немає доступу до цього проекту.");
                }

                return await projectRepository.GetProjectMembershipsAsync(projectId);
            }

            

            throw new GraphQLException("Не вдалось авторизувати користувача.");
        }

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
