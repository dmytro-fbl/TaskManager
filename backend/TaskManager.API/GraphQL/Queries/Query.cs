using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Dashboard;
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
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            return await projectRepository.GetDashboardStatsAsync(userId);
        }

        [Authorize]
        public async Task<IEnumerable<MyProjectDashboardDto>> GetMyProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            return await projectRepository.GetMyProjectsWithHoursAsync(userId);
        }

        [Authorize]
        public async Task<IEnumerable<ManagerProjectDashboardDto>> GetManagerProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? claimsPrincipal.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new GraphQLException("Не вдалось авторизувати користувача.");
            }

            return await projectRepository.GetManagerProjectsWithRoleHoursAsync(userId);
        }

        // БЕЗ [Authorize] тимчасово
        public async Task<IEnumerable<AvailableProjectDto>> GetAvailableProjectsDashboardAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userIdString =
                claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst("sub")?.Value
                ?? claimsPrincipal.FindFirst("email")?.Value;

            if (string.IsNullOrEmpty(userIdString))
            {
                return Enumerable.Empty<AvailableProjectDto>();
            }

            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Enumerable.Empty<AvailableProjectDto>();
            }

            return await projectRepository.GetAvailableProjectsAsync(userId);
        }
    }
}