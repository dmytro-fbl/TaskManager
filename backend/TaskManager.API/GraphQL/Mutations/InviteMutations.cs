using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.Repositories;
using TaskManager.API.Services;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class InviteMutations
    {
        [Authorize]
        public async Task<string> GenerateInviteAsync(string email, bool isAdmin,
            ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal, "Тільки адміністратор може створювати запрошення.");

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
