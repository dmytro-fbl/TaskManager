using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs;
using TaskManager.API.DTOs.Projects;
using TaskManager.API.Extensions;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Services;
using TaskManager.API.DTOs.Dashboard;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class ProjectQuery
    {
        [Authorize]
        public async Task<IEnumerable<Project>> GetMyProjectsAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository)
        {
            var userId = claimsPrincipal.GetUserId();
            return await projectRepository.GetUserProjectsAsync(userId);
        }

        [Authorize]
        public async Task<Project?> GetProjectAsync(
            Guid id,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureCurrentUserAsync(claimsPrincipal);
            return await projectRepository.GetProjectByIdAsync(id);
        }

        [Authorize]
        public async Task<IEnumerable<User>> GetProjectMembersAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectMembershipRepository membershipRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureCurrentUserAsync(claimsPrincipal);
            return await membershipRepository.GetProjectMembersAsync(projectId);
        }

        [Authorize]
        public async Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectMembershipRepository membershipRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureCurrentUserAsync(claimsPrincipal);
            return await membershipRepository.GetProjectMembershipsAsync(projectId);
        }

        [Authorize]
        public async Task<IEnumerable<AdminProjectDto>> GetAdminProjectsAsync(
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal, "Доступ заборонено.");
            return await projectRepository.GetAllProjectAsync();
        }

        [Authorize]
        public async Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureCurrentUserAsync(claimsPrincipal);
            return await projectRepository.GetProjectStatusesAsync(projectId);
        }

        [Authorize]
        public async Task<IEnumerable<ProjectRoleDTO>> GetProjectRolesAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRoleRepository projectRoleRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureCurrentUserAsync(claimsPrincipal);
            return await projectRoleRepository.GetProjectRolesAsync(projectId);
        }
    }
}