using System.Security.Claims;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Services
{
    public interface IAccessService
    {
        Task<User> EnsureCurrentUserAsync(ClaimsPrincipal claimsPrincipal);
        Task<User> EnsureAdminAsync(ClaimsPrincipal claimsPrincipal, string? deniedMessage = null);
        Task<User> EnsureProjectMemberAsync(ClaimsPrincipal claimsPrincipal, Guid projectId, string? deniedMessage = null);
        Task<User> EnsureProjectManagerOrAdminAsync(ClaimsPrincipal claimsPrincipal, Guid projectId, Guid? ownerId = null, string? deniedMessage = null);
        Task<Project> EnsureProjectNotArchivedAsync(Guid projectId);
    }
}
