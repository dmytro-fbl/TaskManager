using TaskManager.API.DTOs.Dashboard;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public interface IDashboardRepository
    {
        Task<IEnumerable<MyProjectDashboardDto>> GetMyProjectsWithHoursAsync(Guid userId);
        Task<IEnumerable<AvailableProjectDto>> GetAvailableProjectsAsync(Guid userId);
        Task<IEnumerable<ManagerProjectDashboardDto>> GetManagerProjectsWithRoleHoursAsync(Guid userId);
        Task<DashboardStatsDto> GetDashboardStatsAsync(Guid userId);
    }
}
