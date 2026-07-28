using System.Security.Claims;
using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Services;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType("UserMutation")]
    public class AuthMutations
    {
        public async Task<AuthPayload> Login(LoginRequest request,
            [Service] IAuthService authService)
        {
            return await authService.LoginAsync(request);
        }

        public async Task<AuthPayload> RefreshTokenAsync(string email, string refreshToken,
            [Service] IUserRepository userRepository, [Service] ITokenService tokenService)
        {
            var user = await userRepository.GetUserByEmail(email);

            if(user == null || user.RefreshToken != refreshToken)
            {
                throw new GraphQLException("Не вілідний токен оновлення");
            }

            if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                throw new GraphQLException("Час дії оновлення токена вичерпано. Увійдіть у систему");
            }

            var newAccessToken = tokenService.CreateToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            var newExpiryTime = DateTime.UtcNow.AddDays(7); 

            await userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, newExpiryTime);

            return new AuthPayload
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<bool> LogoutAsync(ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository)
        {
            var userIdStr = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                await userRepository.UpdateRefreshTokenAsync(userId, null , null);
                return true;
            }

            throw new GraphQLException("Не вдалося ідентифікувати користувача для виходу");
        }
    }
}
