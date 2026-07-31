using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IProjectRepository
    {
        Task<Guid> CreateAsync(Project project);
        Task<Project?> GetProjectByIdAsync(Guid projectId);
    }
}
