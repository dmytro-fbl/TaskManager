using TaskManager.API.DTOs;
using TaskManager.API.Repositories;
using TaskManager.API.Utils;


namespace TaskManager.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<AuthPayload> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmail(request.Email);

            if ((user == null || user.PasswordHash == null || user.PasswordSalt == null))
            {
                throw new GraphQLException("Неправильний email або пароль");
            }

            if (!user.IsActive)
            {
                throw new GraphQLException("Ваш аккаунт не активовано");
            }

            bool isPasswordValid = PasswordHasher.VerifyPasswordHash(
                request.Password,
                user.PasswordHash,
                user.PasswordSalt);

            if (!isPasswordValid)
            {
                throw new GraphQLException("Неправильний email або пароль");
            }

            string token = _tokenService.CreateToken(user);

            return new AuthPayload
            {
                Token = token
            };
        }
    }
}
