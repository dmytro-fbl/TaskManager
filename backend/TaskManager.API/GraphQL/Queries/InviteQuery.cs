using HotChocolate.Authorization;
using TaskManager.API.Repositories;
using TaskManager.API.Models;
using System.Security.Claims;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType("UserQuery")]
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
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(Guid.TryParse(userIdString, out Guid userId))
            {
                return await userRepository.GetUserByIdAsync(userId);
            }

            throw new GraphQLException("Користувача не знайдено");
        }
    }
}
