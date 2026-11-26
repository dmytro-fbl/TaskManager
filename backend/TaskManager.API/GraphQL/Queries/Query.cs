using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Dashboard;
using TaskManager.API.Extensions;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.GraphQL.Queries
{
    public partial class Query
    {
        [Authorize]
        public async Task<DashboardStatsDto> GetDashboardStatsAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            return await projectRepository.GetDashboardStatsAsync(userId);
        }

        [Authorize]
        public async Task<IEnumerable<MyProjectDashboardDto>> GetMyProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            return await projectRepository.GetMyProjectsWithHoursAsync(userId);
        }

        [Authorize]
        public async Task<IEnumerable<ManagerProjectDashboardDto>> GetManagerProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            return await projectRepository.GetManagerProjectsWithRoleHoursAsync(userId);
        }

        [Authorize]
        public async Task<IEnumerable<AvailableProjectDto>> GetAvailableProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            return await projectRepository.GetAvailableProjectsAsync(userId);
        }
    }
}