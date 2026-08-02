using TaskManager.API.DTOs;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IProjectRepository
    {
        Task<Guid> CreateProjectWithOwnerAsync(Project project);
        Task<Project?> GetProjectByIdAsync(Guid projectId);
        Task<IEnumerable<AdminProjectDto>> GetAllProjectAsync();
    }
}
