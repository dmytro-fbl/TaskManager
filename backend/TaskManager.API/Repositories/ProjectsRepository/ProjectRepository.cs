using Npgsql;
using TaskManager.API.DTOs;
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

        public async Task<Guid> CreateProjectWithOwnerAsync(Project project)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                string insertProjectSql = @"

                INSERT INTO app.projects
                    (title, description, budget_cap, status, owner_id, is_archived)
                VALUES (@title, @description, @budget_cap,
                        @status, @owner_id, @is_archived)
                RETURNING id;
            ";

                await using var projectCmd = new NpgsqlCommand(insertProjectSql, connection);

                projectCmd.Parameters.AddWithValue("title", project.Title);
                projectCmd.Parameters.AddWithValue("description", project.Description ?? (object)DBNull.Value);
                projectCmd.Parameters.AddWithValue("budget_cap", project.BudgetCap.HasValue
                    ? project.BudgetCap.Value : (object)DBNull.Value);
                projectCmd.Parameters.AddWithValue("status", project.Status);
                projectCmd.Parameters.AddWithValue("owner_id", project.OwnerId);
                projectCmd.Parameters.AddWithValue("is_archived", project.IsArchived);

                var result = await projectCmd.ExecuteScalarAsync();

                if (result == null || !Guid.TryParse(result.ToString(), out Guid projectId))
                {
                    throw new GraphQLException("не вдалось отримати ID створеного користувача");
                }

                string insertMemberSql = @"
                INSERT INTO app.project_memberships
                    (project_id, user_id, project_role)
                VALUES (@project_id, @user_id, 'manager');
            ";

                await using var memberCmd = new NpgsqlCommand(insertMemberSql, connection);
                memberCmd.Parameters.AddWithValue("project_id", projectId);
                memberCmd.Parameters.AddWithValue("user_id", project.OwnerId);

                await memberCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return projectId;
            }catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new GraphQLException($"Помилка при створенні проекту: {ex.Message}", ex);
            }             
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
                    p.created_at,
                    u.name,
                    u.email
                FROM app.projects p
                INNER JOIN app.users u
                ON p.owner_id = u.id;
            ";
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while ( await reader.ReadAsync())
            {
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
                SELECT id, title, description, budget_cap, status, owner_id, is_archived, created_at, updated_at 
                FROM app.projects
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand( sql, connection);

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

                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? default : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at"))
                };
            }

            return null;
        }
    }
}
