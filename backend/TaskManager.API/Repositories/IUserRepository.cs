using TaskManager.API.Models;


namespace TaskManager.API.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmail(string email);
        Task<User?> GetUserByIdAsync(Guid id);
        Task Create(User user);
        Task<bool> UpdateUserRoleAsync(Guid userId, bool isAdmin);
        Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive);

        Task<IEnumerable<User>> GetAllUsersAsync();

        Task<string> GenerateInviteAsync(string email, bool isAdmin);
        Task<string?> GetEmailByInviteTokenAsync(string token);
        Task<bool> CompleteRegistrationAsync(string token, string name, string passwordHash, string passwordSalt);

        Task<bool> UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiryTime);
    }
}
