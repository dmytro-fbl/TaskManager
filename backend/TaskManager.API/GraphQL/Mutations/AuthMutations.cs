using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Services;

namespace TaskManager.API.GraphQL.Mutations
{
    public class AuthMutations
    {
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
    }
}
