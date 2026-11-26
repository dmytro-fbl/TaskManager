using HotChocolate.Fusion.Rewriters;
using Npgsql;
using System.Data;
using TaskManager.API.DTOs;
using TaskManager.API.DTOs.Dashboard;
using TaskManager.API.DTOs.Projects;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using NpgsqlTypes;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<ProjectRepository> _logger;

        public ProjectRepository(NpgsqlDataSource dataSource, ILogger<ProjectRepository> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<IEnumerable<AdminProjectDto>> GetAllProjectAsync()
        {
            var projects = new List<AdminProjectDto>();

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
            u.name,
            u.email
        FROM app.projects p
        INNER JOIN app.users u
        ON p.owner_id = u.id;
    ";

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var deadlineOrdinal = reader.GetOrdinal("deadline");

                projects.Add(new AdminProjectDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("description")),
                    BudgetCap = reader.IsDBNull(reader.GetOrdinal("budget_cap"))
                        ? null
                        : reader.GetDecimal(reader.GetOrdinal("budget_cap")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    OwnerId = reader.GetGuid(reader.GetOrdinal("owner_id")),
                    IsArchived = reader.GetBoolean(reader.GetOrdinal("is_archived")),
                    Deadline = reader.IsDBNull(deadlineOrdinal)
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(deadlineOrdinal),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    OwnerName = reader.GetString(reader.GetOrdinal("name")),
                    OwnerEmail = reader.GetString(reader.GetOrdinal("email"))
                });
            }

            return projects;
        }

        public async Task<Project?> GetProjectByIdAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            string sql = @"
                SELECT id, title, description, budget_cap, deadline, status, owner_id, is_archived, created_at, updated_at 
                FROM app.projects
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", projectId);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Project
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                    BudgetCap = reader.IsDBNull(reader.GetOrdinal("budget_cap"))
                        ? null : reader.GetDecimal(reader.GetOrdinal("budget_cap")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    OwnerId = reader.GetGuid(reader.GetOrdinal("owner_id")),
                    IsArchived = reader.GetBoolean(reader.GetOrdinal("is_archived")),
                    Deadline = reader.IsDBNull(reader.GetOrdinal("deadline")) ? (DateTimeOffset?)null 
                    : reader.GetFieldValue<DateTimeOffset>(
                        reader.GetOrdinal("deadline")
                      ),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? default : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
                };
            }

            return null;
        }

        public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId)
        {
            var projects = new List<Project>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            string sql = @"
                SELECT DISTINCT p.id, p.title, p.description, p.budget_cap, p.status, p.owner_id, p.is_archived, p.created_at, p.updated_at
                FROM app.projects p
                INNER JOIN app.project_memberships pm ON p.id = pm.project_id
                WHERE pm.user_id = @user_id
                ORDER BY p.created_at DESC;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                    BudgetCap = reader.IsDBNull(reader.GetOrdinal("budget_cap"))
                        ? null : reader.GetDecimal(reader.GetOrdinal("budget_cap")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    OwnerId = reader.GetGuid(reader.GetOrdinal("owner_id")),
                    IsArchived = reader.GetBoolean(reader.GetOrdinal("is_archived")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? default : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
                });
            }

            return projects;
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
                    @id, @title, @description, @budget_cap, @status, @owner_id, @is_archived, @deadline, @created_at, @updated_at
                );

            INSERT INTO app.project_memberships (
                project_id, user_id, project_role, joined_at
            )
            VALUES (
                @id, @owner_id, 'manager', @created_at
            );

            INSERT INTO app.project_statuses (
                id, project_id, name, category, color, sort_order, is_final, created_at
            )
            VALUES
            (
                gen_random_uuid(), @id, 'To do', 'todo', '#64748b', 0, false, @created_at
            ),
            (
                gen_random_uuid(), @id, 'In progress', 'in_progress', '#2563eb', 1, false, @created_at
            ),
            (
                gen_random_uuid(), @id, 'Done', 'done', '#16a34a', 2, true, @created_at
            );
    ";

            var projectId = project.Id == Guid.Empty
                ? Guid.NewGuid()
                : project.Id;

            var now = DateTimeOffset.UtcNow;

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("id", projectId);
            command.Parameters.AddWithValue("title", project.Title);
            command.Parameters.AddWithValue(
                "description",
                (object?)project.Description ?? DBNull.Value
            );
            command.Parameters.AddWithValue(
                "budget_cap",
                (object?)project.BudgetCap ?? DBNull.Value
            );
            command.Parameters.AddWithValue("status", project.Status);
            command.Parameters.AddWithValue("owner_id", project.OwnerId);
            command.Parameters.AddWithValue("is_archived", project.IsArchived);
            command.Parameters.AddWithValue("deadline", (object?)project.Deadline ?? DBNull.Value);
            command.Parameters.AddWithValue("created_at", now);
            command.Parameters.AddWithValue("updated_at", now);

            try
            {
                await command.ExecuteNonQueryAsync();
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

            string newStatus = isArchived ? "archived" : "active";

            const string sql = @"
                UPDATE app.projects
                SET is_archived = @is_archived,
                    status = @status,
                    updated_at = now()    
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("is_archived", isArchived);
            command.Parameters.AddWithValue("status", newStatus);
            command.Parameters.AddWithValue("id", projectId);

            var rowAffected = await command.ExecuteNonQueryAsync();
            return rowAffected > 0;
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
                budget_cap = @budget_cap,
                deadline = @deadline,
                updated_at = now()
            WHERE id = @project_id;
        ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue(
                "description",
                NpgsqlDbType.Text,
                (object?)description ?? DBNull.Value
            );
            command.Parameters.AddWithValue(
                "budget_cap",
                NpgsqlDbType.Numeric,
                (object?)budgetCap ?? DBNull.Value
            );
            command.Parameters.AddWithValue(
                "deadline",
                NpgsqlDbType.TimestampTz,
                (object?)deadline ?? DBNull.Value
            );

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteProjectAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.projects
                WHERE id = @project_id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<ProjectStatus>> GetProjectStatusesAsync(Guid projectId)
        {
            var statuses = new List<ProjectStatus>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, project_id, name, category, color,
                       sort_order, is_final, created_at
                FROM app.project_statuses
                WHERE project_id = @project_id
                ORDER BY sort_order;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                statuses.Add(new ProjectStatus
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    ProjectId = reader.GetGuid(reader.GetOrdinal("project_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Category = reader.GetString(reader.GetOrdinal("category")),
                    Color = reader.IsDBNull(reader.GetOrdinal("color"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("color")),
                    SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
                    IsFinal = reader.GetBoolean(reader.GetOrdinal("is_final")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(
                        reader.GetOrdinal("created_at")
                    )
                });
            }

            return statuses;
        }
    }
}
