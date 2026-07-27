using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Services;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL.Mutations
{
    public class UserMutation
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public UserMutation(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<AuthPayload> Login(LoginRequest request, [Service] IAuthService authService)
        {
            return await authService.LoginAsync(request);
        }

        [Authorize]
        public async Task<bool> ToggleUserRoleAsync(Guid userId, bool isAdmin, ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository)
        {
            var currentUserIdStr = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if(Guid.TryParse(currentUserIdStr, out Guid currentUserId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(currentUserId);
                if(currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("Доступ заборонено!");
                }

                var targetUser = await userRepository.GetUserByIdAsync(userId);

                if(targetUser != null && targetUser.Email == "admin@tasktracker.com")
                {
                    throw new GraphQLException("Ви не можете змінити права головного адміністратора");
                }

                if(currentUserId == userId)
                {
                    throw new GraphQLException("Ви не можете змінити права доступу до власного акаунту");
                }

                return await userRepository.UpdateUserRoleAsync(userId, isAdmin);
            }

            throw new GraphQLException("Помилка авторизації");
        }

        [Authorize]
        public async Task<bool> ToggleUserStatusAsync(Guid userId, bool isActive, ClaimsPrincipal claimsPrincipal,

           [Service] IUserRepository userRepository)
        {
            var currentUserIdStr = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(currentUserIdStr, out Guid currentUserId))
            {
                var currentUser = await userRepository.GetUserByIdAsync(currentUserId);
                if (currentUser == null || !currentUser.IsAdmin)
                {
                    throw new GraphQLException("Доступ заборонено!");
                }

                var targetUser = await userRepository.GetUserByIdAsync(userId);
                if (targetUser != null && targetUser.Email == "admin@tasktracker.com")
                {
                    throw new GraphQLException("Ви не можете заблокувати головного адміністратора");
                }

                if (currentUserId == userId)
                {
                    throw new GraphQLException("Ви не можете заблокувати власний акаунт");
                }

                return await userRepository.UpdateUserStatusAsync(userId, isActive);
            }

            throw new GraphQLException("Помилка авторизації");
        }
    }
}
