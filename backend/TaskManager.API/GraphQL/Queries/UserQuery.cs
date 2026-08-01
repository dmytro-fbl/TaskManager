using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.Models;
using TaskManager.API.Repositories;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class UserQuery
    {
        [Authorize]
        public async Task<IEnumerable<User>> GetUsersAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(Guid.TryParse(userIdString, out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);

                if(currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("Доступ заборонено.");
                }

                return await userRepository.GetAllUsersAsync();
            }
            throw new GraphQLException("Неавторизований запит");
        }

        [Authorize]
        public async Task<IEnumerable<User>> GetPendingInviteUsersAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdString, out var userId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(userId);

                if(currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("Доступ заборонено.");
                }
                return await userRepository.GetUsersPendingInviteAsync();
            }
            throw new GraphQLException("Неавторизований запит");
        }


    }
}
