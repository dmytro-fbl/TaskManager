

using TaskManager.API.Models;
using TaskManager.API.Repositories;

namespace TaskManager.API.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;
        public UserServices(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task InviteAdminAsync(string email, string name)
        {
            string token = Guid.NewGuid().ToString();

            DateTimeOffset linkLifetime = DateTimeOffset.UtcNow.AddHours(24);

            User user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                IsAdmin = true,
                IsActive = true,
                InviteToken = token,
                InviteExpiresAt = linkLifetime,
            };

            await _userRepository.Create(user);
        }

        
    }
}
