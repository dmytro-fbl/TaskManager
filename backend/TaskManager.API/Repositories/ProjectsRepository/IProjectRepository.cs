using TaskManager.API.DTOs;
using TaskManager.API.DTOs.Projects;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IProjectRepository
    {
        Task<Guid> CreateProjectWithOwnerAsync(Project project);
        Task<Project?> GetProjectByIdAsync(Guid projectId);
        Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId);
        Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId);
        Task<bool> InviteUserToProjectAsync(Guid projectId, string email, string projectRole);
        Task<bool> IsUserInProjectAsync(Guid projectId, Guid userId);
        Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(Guid projectId);
        Task<bool> AcceptProjectInvitationAsync(string token, Guid currentUserId);
        Task<bool> CreateProjectInvitationForExistingUserAsync(
            Guid projectId,
            string email,
            string projectRole,
            Guid invitedBy,
            Guid? roleLabelId = null);

        Task<bool> UpdateProjectMembershipRoleAsync(
            Guid projectId,
            Guid userId,
            string projectRole,
            Guid? roleLabelId,
            Guid updatedByUserId);

        Task<IEnumerable<AdminProjectDto>> GetAllProjectAsync();

        Task<bool> ToggleArchiveProjectAsync(Guid projectId, bool isArchived);

        Task<bool> UpdateProjectAsync(Guid projectId, string title, string? description, decimal? budgetCap, DateTimeOffset deadline);

        Task<string?> GetUserProjectRoleAsync(Guid projectId, Guid userId);
        Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync(Guid projectId );

        Task<bool> RemoveMemberAsync(Guid projectId, Guid userId);
        Task AddDefaultProjectRolesAsync(Guid projectId);

        Task<IEnumerable<ProjectRoleDTO>> GetProjectRolesAsync(Guid projectId);

    }
}
