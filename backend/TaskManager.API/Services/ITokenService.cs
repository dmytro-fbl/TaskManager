using TaskManager.API.Models;

namespace TaskManager.API.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
        string GenerateRefreshToken();
    }
}
