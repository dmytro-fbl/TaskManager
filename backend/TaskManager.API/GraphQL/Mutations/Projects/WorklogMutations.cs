using HotChocolate.Authorization;
using System.Security.Claims;
using TaskManager.API.DTOs.Worklog;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class WorklogMutations
    {
        [Authorize]
        public async Task<bool> AddProjectHoursAsync(
    AddProjectHoursInput input,
    ClaimsPrincipal claimsPrincipal,
    [Service] ITaskRepository taskRepository,
    [Service] IProjectRepository projectRepository,
    [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            var currentUser = await userRepository.GetUserByIdAsync(userId);
            if (currentUser == null)
            {
                throw new GraphQLException("Користувача не знайдено.");
            }

            var hasAccess = currentUser.IsAdmin ||
                            await projectRepository.IsUserInProjectAsync(input.ProjectId, userId);

            if (!hasAccess)
            {
                throw new GraphQLException("У вас немає доступу до цього проєкту.");
            }

            if (input.Hours <= 0)
            {
                throw new GraphQLException("Години мають бути більше 0.");
            }

            await taskRepository.AddProjectHoursAsync(
                input.ProjectId,
                userId,
                input.Hours);

            return true;
        }
    }
}