using TaskManager.API.Repositories;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType("UserMutation")]
    public class InviteMutations
    {
        public async Task<string> GenerateInviteAsync(string email, bool isAdmin,
            [Service] IUserRepository userRepository)
        {
            return await userRepository.GenerateInviteAsync(email, isAdmin);
        }

        public async Task<bool> CompleteRegistrationAsync(string token, string name, string password,
            [Service] IUserRepository userRepository)
        {
            PasswordHasher.CreatePasswordHash(password, out string passwordHash, out string passwordSalt);

            var result = await userRepository.CompleteRegistrationAsync(token, name, passwordHash, passwordSalt);

            if(!result)
            {
                throw new GraphQLException("Недійсний токен запрошення або його термін дії минув");
            }
            return true;
        }
    }
}
