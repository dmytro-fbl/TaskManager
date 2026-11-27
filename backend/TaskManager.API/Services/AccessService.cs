using System.Security.Claims;
using TaskManager.API.Extensions;
using TaskManager.API.Models;
using TaskManager.API.Models.Constants;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.Services
{
    public class AccessService : IAccessService
    {
        private readonly IUserRepository _userRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectMembershipRepository _membershipRepository;

        public AccessService(
            IUserRepository userRepository,
            IProjectRepository projectRepository,
            IProjectMembershipRepository membershipRepository)
        {
            _userRepository = userRepository;
            _projectRepository = projectRepository;
            _membershipRepository = membershipRepository;
        }

        public async Task<User> EnsureCurrentUserAsync(ClaimsPrincipal claimsPrincipal)
        {
            var userId = claimsPrincipal.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);

            return user ?? throw new GraphQLException("Користувача не знайдено.");
        }

        public async Task<User> EnsureAdminAsync(ClaimsPrincipal claimsPrincipal, string? deniedMessage = null)
        {
            var user = await EnsureCurrentUserAsync(claimsPrincipal);
            if (!user.IsAdmin)
            {
                throw new GraphQLException(deniedMessage ?? "Доступ заборонено!");
            }

            return user;
        }

        public async Task<User> EnsureProjectMemberAsync(
            ClaimsPrincipal claimsPrincipal,
            Guid projectId,
            string? deniedMessage = null)
        {
            var user = await EnsureCurrentUserAsync(claimsPrincipal);
            var hasAccess = user.IsAdmin || await _membershipRepository.IsUserInProjectAsync(projectId, user.Id);

            if (!hasAccess)
            {
                throw new GraphQLException(deniedMessage ?? "У вас немає доступу до цього проєкту.");
            }

            return user;
        }

        public async Task<User> EnsureProjectManagerOrAdminAsync(
            ClaimsPrincipal claimsPrincipal,
            Guid projectId,
            Guid? ownerId = null,
            string? deniedMessage = null)
        {
            var user = await EnsureCurrentUserAsync(claimsPrincipal);

            if (user.IsAdmin || (ownerId.HasValue && ownerId.Value == user.Id))
            {
                return user;
            }

            var userRole = await _membershipRepository.GetUserProjectRoleAsync(projectId, user.Id);
            if (userRole != ProjectRoles.Manager)
            {
                throw new GraphQLException(deniedMessage ?? "Доступ заборонено!");
            }

            return user;
        }

        public async Task<Project> EnsureProjectNotArchivedAsync(Guid projectId)
        {
            var project = await _projectRepository.GetProjectByIdAsync(projectId);
            if (project == null)
            {
                throw new GraphQLException("Проєкт не знайдено.");
            }

            if (project.IsArchived || project.Status == ProjectStatuses.Archived)
            {
                throw new GraphQLException("Проєкт в архіві. Будь-які зміни заборонені.");
            }

            return project;
        }
    }
}
