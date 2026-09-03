using HotChocolate.Authorization;
using System.Security.Claims;
using TaskManager.API.Repositories;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class InviteMutations
    {
        [Authorize]
        public async Task<string> GenerateInviteAsync(
            string email, 
            bool isAdmin,
            ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository)
        {
            var userIdValue = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdValue, out var currentUserId))
            {
                throw new GraphQLException("Помилка авторизації.");
            }

            var currentUser = await userRepository.GetUserByIdAsync(currentUserId);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                throw new GraphQLException("Доступ заборонено. Тільки адміністратор може генерувати запрошення.");
            }

            try
            {
                return await userRepository.GenerateInviteAsync(email, isAdmin);
            }
            catch (Exception ex)
            {
                throw new GraphQLException($"Помилка беку: {ex.Message}");
            }
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
