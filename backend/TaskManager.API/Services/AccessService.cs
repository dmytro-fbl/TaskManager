using System.Security.Claims;
using TaskManager.API.Extensions;
using TaskManager.API.Models;
using TaskManager.API.Models.Constants;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.Services
{
    public class AccessService : IAccessService
    {
        private readonly IUserRepository _userRepository;
        private readonly IProjectMembershipRepository _membershipRepository;

        public AccessService(
            IUserRepository userRepository,
            IProjectMembershipRepository membershipRepository)
        {
            _userRepository = userRepository;
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
    }
}
