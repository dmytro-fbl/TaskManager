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
    }
}
