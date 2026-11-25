using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Services;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class UserMutation
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public UserMutation(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        

        [Authorize]
        public async Task<bool> ToggleUserRoleAsync(Guid userId, bool isAdmin, ClaimsPrincipal claimsPrincipal,
            [Service] IUserRepository userRepository,
            [Service] IAccessService accessService)
        {
            var currentUser = await accessService.EnsureAdminAsync(claimsPrincipal);
            var targetUser = await userRepository.GetUserByIdAsync(userId);

            if (targetUser != null && targetUser.Email == "admin@tasktracker.com")
            {
                throw new GraphQLException("Ви не можете змінити права головного адміністратора");
            }

            if (currentUser.Id == userId)
            {
                throw new GraphQLException("Ви не можете змінити права доступу до власного акаунту");
            }

            return await userRepository.UpdateUserRoleAsync(userId, isAdmin);
        }

        [Authorize]
        public async Task<bool> ToggleUserStatusAsync(Guid userId, bool isActive, ClaimsPrincipal claimsPrincipal,
           [Service] IUserRepository userRepository,
           [Service] IAccessService accessService)
        {
            var currentUser = await accessService.EnsureAdminAsync(claimsPrincipal);
            var targetUser = await userRepository.GetUserByIdAsync(userId);

            if (targetUser != null && targetUser.Email == "admin@tasktracker.com")
            {
                throw new GraphQLException("Ви не можете заблокувати головного адміністратора");
            }

            if (currentUser.Id == userId)
            {
                throw new GraphQLException("Ви не можете заблокувати власний акаунт");
            }

            return await userRepository.UpdateUserStatusAsync(userId, isActive);
        }
    }
}
