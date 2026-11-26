using System.Security.Claims;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Projects;
using TaskManager.API.Extensions;
using TaskManager.API.Models.Constants;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories.ProjectsRepository;
using TaskManager.API.Repositories.TasksRepository;
using TaskManager.API.Services;

namespace TaskManager.API.GraphQL.Mutations.Projects
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class ProjectMutations
    {
        [Authorize]
        public async Task<Project> CreateProjectAsync(
            CreateProjectInput input,
            [Service] IProjectRepository projectRepository,
            ClaimsPrincipal claimsPrincipal)
        {
            var ownerId = claimsPrincipal.GetUserId();

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                throw new GraphQLException("Назва проекту не може бути порожньою.");
            }

            if (input.Deadline.Date < DateTimeOffset.UtcNow.Date)
            {
                throw new GraphQLException("Дедлайн не може бути встановлений у минулому.");
            }

            var newProject = new Project
            {
                Title = input.Title.Trim(),
                Description = input.Description?.Trim() ?? string.Empty,
                BudgetCap = input.BudgetCap,
                Status = ProjectStatuses.Active,
                OwnerId = ownerId,
                IsArchived = false,
                Deadline = input.Deadline,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createdProjectId = await projectRepository.CreateProjectWithOwnerAsync(newProject);

            var createdProject = await projectRepository.GetProjectByIdAsync(createdProjectId);

            return createdProject
                ?? throw new GraphQLException("Помилка під час зчитування створеного проекту.");
        }

        [Authorize]
        public async Task<bool> ToggleProjectIsArchivedAsync(
            Guid projectId,
            bool isArchived,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal);

            var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
            if (currentProject == null)
            {
                throw new GraphQLException("Проєкт не знайдено.");
            }

            return await projectRepository.ToggleArchiveProjectAsync(projectId, isArchived);
        }


        [Authorize]
        public async Task<bool> UpdateProjectAsync(
            Guid projectId,
            string title,
            string? description,
            decimal? budgetCap,
            DateTimeOffset? deadline,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
            if (currentProject == null || currentProject.Status == ProjectStatuses.Archived)
                throw new GraphQLException("Цей проєкт архівний або не знайдений.");

            await accessService.EnsureProjectManagerOrAdminAsync(
                claimsPrincipal,
                projectId,
                currentProject.OwnerId,
                "У вас немає прав на редагування цього проєкту.");

            return await projectRepository.UpdateProjectAsync(projectId, title, description, budgetCap, deadline);
        }

        [Authorize]
        public async Task<bool> RemoveProjectMemberAsync(
            Guid projectId,
            Guid memberUserId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IProjectMembershipRepository membershipRepository,
            [Service] IAccessService accessService,
            [Service] ITaskRepository taskRepository)
        {
            var currentProject = await projectRepository.GetProjectByIdAsync(projectId);
            if (currentProject == null || currentProject.Status == ProjectStatuses.Archived || currentProject.IsArchived)
                throw new GraphQLException("Цей проєкт архівний або не знайдений.");

            if (currentProject.OwnerId == memberUserId)
                throw new GraphQLException("Не можна видалити власника проєкту.");

            await accessService.EnsureProjectManagerOrAdminAsync(
                claimsPrincipal,
                projectId,
                currentProject.OwnerId,
                "У вас немає прав на видалення учасників з цього проєкту.");

            bool hasTasks = await taskRepository.HasUserTasksInProjectAsync(projectId, memberUserId);
            if (hasTasks)
                throw new GraphQLException("Неможливо видалити користувача, оскільки на нього призначені завдання або є витрачений час у цьому проєкті.");

            return await membershipRepository.RemoveMemberAsync(projectId, memberUserId);
        }

        [Authorize]
        public async Task<ProjectRoleDTO> CreateProjectRoleAsync(
            Guid projectId,
            string name,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRoleRepository projectRoleRepository,
            [Service] IAccessService accessService)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            {
                throw new GraphQLException("Назва ролі має містити більше 2 символів.");
            }

            await accessService.EnsureProjectManagerOrAdminAsync(
                claimsPrincipal,
                projectId,
                deniedMessage: "Тільки адміністратор або менеджер може створювати кастомні ролі.");

            try
            {
                return await projectRoleRepository.CreateProjectRoleAsync(projectId, name);
            }
            catch (InvalidOperationException ex)
            {
                throw new GraphQLException(ex.Message, ex);
            }
        }

        [Authorize]
        public async Task<ProjectRoleDTO> UpdateProjectroleAsync(
            Guid projectId,
            Guid roleId,
            string newName,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRoleRepository projectRoleRepository,
            [Service] IAccessService accessService)
        {
            if (string.IsNullOrWhiteSpace(newName) || newName.Trim().Length < 2)
                throw new GraphQLException("Назва ролі має містити більше 2 символів.");

            await accessService.EnsureProjectManagerOrAdminAsync(
                claimsPrincipal,
                projectId,
                deniedMessage: "Тільки менеджер проєкту або адміністратор може редагувати ролі.");

            try
            {
                return await projectRoleRepository.UpdateProjectRoleAsync(projectId, roleId, newName);
            }
            catch (InvalidOperationException ex)
            {
                throw new GraphQLException(ex.Message);
            }
        }

        [Authorize]
        public async Task<bool> DeleteProjectRoleAsync(
            Guid projectId,
            Guid roleId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRoleRepository projectRoleRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureProjectManagerOrAdminAsync(
                claimsPrincipal,
                projectId,
                deniedMessage: "Тільки менеджер проєкту або адміністратор може видаляти ролі.");

            try
            {
                return await projectRoleRepository.DeleteProjectRoleAsync(roleId, projectId);
            }
            catch (Exception ex)
            {
                throw new GraphQLException(ex.Message);
            }
        }

        [Authorize]
        public async Task<bool> DeleteProjectAsync(
            Guid projectId,
            ClaimsPrincipal claimsPrincipal,
            [Service] IProjectRepository projectRepository,
            [Service] IAccessService accessService)
        {
            await accessService.EnsureAdminAsync(claimsPrincipal, "Тільки адміністратор може видаляти проєкт.");

            var project = await projectRepository.GetProjectByIdAsync(projectId);
            if (project == null)
                throw new GraphQLException("Проєкт не знайдено.");

            return await projectRepository.DeleteProjectAsync(projectId);
        }
    }
}