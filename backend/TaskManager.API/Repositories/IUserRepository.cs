using TaskManager.API.Models;


namespace TaskManager.API.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmail(string email);
        Task Create(User user);

        Task<string> CreateInviteAsync(string email);
        Task<string?> GetEmailByInviteTokenAsync(string token);
        Task<bool> CompleteRegistrationAsync(string token, string name, string passwordHash, string passwordSalt);
    }
}
