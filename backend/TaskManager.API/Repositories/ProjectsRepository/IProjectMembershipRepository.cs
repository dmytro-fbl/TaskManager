using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IProjectMembershipRepository
    {
        Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId);
        Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(Guid projectId);
        Task<bool> IsUserInProjectAsync(Guid projectId, Guid userId);
        Task<string?> GetUserProjectRoleAsync(Guid projectId, Guid userId);
        Task<bool> UpdateProjectMembershipRoleAsync(
            Guid projectId,
            Guid userId,
            string projectRole,
            Guid? roleLabelId,
            Guid updatedByUserId);
        Task<bool> RemoveMemberAsync(Guid projectId, Guid userId);
        Task<bool> InviteUserToProjectAsync(Guid projectId, string email, string projectRole);
        Task<bool> CreateProjectInvitationForExistingUserAsync(
            Guid projectId,
            string email,
            string projectRole,
            Guid invitedBy,
            Guid? roleLabelId = null);
        Task<bool> AcceptProjectInvitationAsync(string token, Guid currentUserId);
    }
}
