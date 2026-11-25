using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.Models;
using TaskManager.API.Repositories;
using TaskManager.API.Services;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class UserQuery
    {
        [Authorize]
        public async Task<IEnumerable<User>> GetUsersAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal, "Доступ заборонено.");
            return await userRepository.GetAllUsersAsync();
        }

        [Authorize]
        public async Task<IEnumerable<User>> GetPendingInviteUsersAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal, "Доступ заборонено.");
            return await userRepository.GetUsersPendingInviteAsync();
        }


    }
}
