using TaskManager.API.DTOs.Projects;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IProjectRoleRepository
    {
        Task<IEnumerable<ProjectRoleDTO>> GetProjectRolesAsync(Guid projectId);
        Task<ProjectRoleDTO> CreateProjectRoleAsync(Guid projectId, string roleName);
        Task<ProjectRoleDTO> UpdateProjectRoleAsync(Guid projectId, Guid roleId, string newName);
        Task<bool> DeleteProjectRoleAsync(Guid roleId, Guid projectId);
    }
}
