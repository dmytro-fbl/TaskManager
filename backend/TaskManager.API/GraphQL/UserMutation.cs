using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Services;
using TaskManager.API.Utils;

namespace TaskManager.API.GraphQL
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
    }
}
