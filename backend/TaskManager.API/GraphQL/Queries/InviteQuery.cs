using HotChocolate.Authorization;
using TaskManager.API.Extensions;
using TaskManager.API.Repositories;
using TaskManager.API.Models;
using System.Security.Claims;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class InviteQuery
    {
        public async Task<string?> VerifyInviteTokenAsync(string  token, [Service] IUserRepository userRepository)
        {
            var email = await userRepository.GetEmailByInviteTokenAsync(token);
            if (email == null)
            {
                throw new GraphQLException("Посилання не дійсне або протерміноване");
            }

            return email;
        }

        [Authorize]
        public async Task<User> GetMeAsync(ClaimsPrincipal claimsPrincipal, [Service] IUserRepository userRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            var user = await userRepository.GetUserByIdAsync(userId);
            return user ?? throw new GraphQLException("Користувача не знайдено");
        }
    }
}
