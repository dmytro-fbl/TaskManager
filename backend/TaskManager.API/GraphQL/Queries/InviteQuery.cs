using TaskManager.API.Repositories;

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
    }
}
