using TaskManager.API.DTOs;

namespace TaskManager.API.Services
{
    public interface IAuthService
    {
        Task<AuthPayload> LoginAsync(LoginRequest request);
    }
}
