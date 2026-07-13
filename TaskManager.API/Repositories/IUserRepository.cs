

using TaskManager.API.Models;

namespace TaskManager.API.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmail(string email);
        Task Create(User user);
    }
}
