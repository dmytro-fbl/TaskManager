using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;
using TaskManager.API.Repositories.ProjectsRepository;

namespace TaskManager.API.GraphQL.Resolvers
{
    [ExtendObjectType("Project")]
    public class ProjectExtensions
    {
        /// <summary>
        /// Отримати власника проєкту
        /// </summary>
        public async Task<User?> GetOwner(
            [Parent] Project project,
            [Service] IUserRepository userRepository)
        {
            return await userRepository.GetUserByIdAsync(project.OwnerId);
        }

        /// <summary>
        /// Отримати список учасників проєкту
        /// </summary>
        public async Task<IEnumerable<User>> GetMembers(
            [Parent] Project project,
            [Service] IProjectRepository projectRepository)
        {
            return await projectRepository.GetProjectMembersAsync(project.Id);
        }

        /// <summary>
        /// Отримати членство в проєкті
        /// </summary>
        public async Task<IEnumerable<ProjectMembership>> GetMemberships(
            [Parent] Project project,
            [Service] IProjectRepository projectRepository)
        {
            return await projectRepository.GetProjectMembershipsAsync(project.Id);
        }
    }

    [ExtendObjectType("ProjectMembership")]
    public class ProjectMembershipExtensions
    {
        /// <summary>
        /// Отримати користувача за членством
        /// </summary>
        public async Task<User?> GetUser(
            [Parent] ProjectMembership membership,
            [Service] IUserRepository userRepository)
        {
            return await userRepository.GetUserByIdAsync(membership.UserId);
        }

        /// <summary>
        /// Отримати проєкт за членством
        /// </summary>
        public async Task<Project?> GetProject(
            [Parent] ProjectMembership membership,
            [Service] IProjectRepository projectRepository)
        {
            return await projectRepository.GetProjectByIdAsync(membership.ProjectId);
        }
    }
}