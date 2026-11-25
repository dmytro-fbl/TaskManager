using System.Security.Claims;
using TaskManager.API.Models;

namespace TaskManager.API.Services
{
    public interface IAccessService
    {
        Task<User> EnsureCurrentUserAsync(ClaimsPrincipal claimsPrincipal);
        Task<User> EnsureAdminAsync(ClaimsPrincipal claimsPrincipal, string? deniedMessage = null);
        Task<User> EnsureProjectMemberAsync(ClaimsPrincipal claimsPrincipal, Guid projectId, string? deniedMessage = null);
        Task<User> EnsureProjectManagerOrAdminAsync(ClaimsPrincipal claimsPrincipal, Guid projectId, Guid? ownerId = null, string? deniedMessage = null);
    }
}
