using Dapper;
using Npgsql;
using TaskManager.API.DTOs;
using TaskManager.API.Models.Constants;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public ProjectRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IEnumerable<AdminProjectDto>> GetAllProjectAsync()
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT p.id,
                       p.title,
                       p.description,
                       p.budget_cap,
                       p.status,
                       p.owner_id,
                       p.is_archived,
                       p.deadline,
                       p.created_at,
                       u.name AS owner_name,
                       u.email AS owner_email
                FROM app.projects p
                INNER JOIN app.users u ON p.owner_id = u.id;";

            return await connection.QueryAsync<AdminProjectDto>(sql);
        }

        public async Task<Project?> GetProjectByIdAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, title, COALESCE(description, '') AS description, budget_cap, deadline,
                       status, owner_id, is_archived, created_at, updated_at
                FROM app.projects
                WHERE id = @projectId;";

            return await connection.QuerySingleOrDefaultAsync<Project>(sql, new { projectId });
        }

        public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT DISTINCT p.id, p.title, COALESCE(p.description, '') AS description, p.budget_cap,
                                p.status, p.owner_id, p.is_archived, p.created_at, p.updated_at
                FROM app.projects p
                INNER JOIN app.project_memberships pm ON p.id = pm.project_id
                WHERE pm.user_id = @userId
                ORDER BY p.created_at DESC;";

            return await connection.QueryAsync<Project>(sql, new { userId });
        }

        public async Task<Guid> CreateProjectWithOwnerAsync(Project project)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            const string sql = @"
                INSERT INTO app.projects (
                    id, title, description, budget_cap, status, owner_id, is_archived, deadline, created_at, updated_at
                )
                VALUES (
                    @id, @title, @description, @budgetCap, @status, @ownerId, @isArchived, @deadline, @createdAt, @updatedAt
                );

                INSERT INTO app.project_memberships (
                    project_id, user_id, project_role, joined_at
                )
                VALUES (
                    @id, @ownerId, 'manager', @createdAt
                );

                INSERT INTO app.project_statuses (
                    id, project_id, name, category, color, sort_order, is_final, created_at
                )
                VALUES
                    (gen_random_uuid(), @id, 'To do', 'todo', '#64748b', 0, false, @createdAt),
                    (gen_random_uuid(), @id, 'In progress', 'in_progress', '#2563eb', 1, false, @createdAt),
                    (gen_random_uuid(), @id, 'Done', 'done', '#16a34a', 2, true, @createdAt);";

            var projectId = project.Id == Guid.Empty ? Guid.NewGuid() : project.Id;
            var now = DateTimeOffset.UtcNow;

            try
            {
                await connection.ExecuteAsync(sql, new
                {
                    id = projectId,
                    title = project.Title,
                    description = project.Description,
                    budgetCap = project.BudgetCap,
                    status = project.Status,
                    ownerId = project.OwnerId,
                    isArchived = project.IsArchived,
                    deadline = project.Deadline,
                    createdAt = now,
                    updatedAt = now
                }, transaction);

                await transaction.CommitAsync();
                return projectId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ToggleArchiveProjectAsync(Guid projectId, bool isArchived)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var newStatus = isArchived ? ProjectStatuses.Archived : ProjectStatuses.Active;

            const string sql = @"
                UPDATE app.projects
                SET is_archived = @isArchived,
                    status = @status,
                    updated_at = now()
                WHERE id = @projectId;";

            return await connection.ExecuteAsync(sql, new { projectId, isArchived, status = newStatus }) > 0;
        }

        public async Task<bool> UpdateProjectAsync(
            Guid projectId,
            string title,
            string? description,
            decimal? budgetCap,
            DateTimeOffset? deadline)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.projects
                SET title = @title,
                    description = @description,
                    budget_cap = @budgetCap,
                    deadline = @deadline,
                    updated_at = now()
                WHERE id = @projectId;";

            return await connection.ExecuteAsync(sql, new { projectId, title, description, budgetCap, deadline }) > 0;
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.projects
                WHERE id = @projectId;";

            return await connection.ExecuteAsync(sql, new { projectId }) > 0;
        }

        public async Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, project_id, name, category, color, sort_order, is_final, created_at
                FROM app.project_statuses
                WHERE project_id = @projectId
                ORDER BY sort_order;";

            return await connection.QueryAsync<ProjectStatus>(sql, new { projectId });
        }
    }
}
