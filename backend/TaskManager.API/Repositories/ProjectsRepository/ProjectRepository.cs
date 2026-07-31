using Npgsql;
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

        public async Task<Guid> CreateAsync(Project project)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            string sql = @"

                INSERT INTO app.projects
                    (title, description, budget_cap, status, owner_id, is_archived)
                VALUES (@title, @description, @budget_cap,
                        @status, @owner_id, @is_archived)
                RETURNING id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("title", project.Title);
            command.Parameters.AddWithValue("description", project.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("budget_cap", project.BudgetCap.HasValue 
                ? project.BudgetCap.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("status", project.Status);
            command.Parameters.AddWithValue("owner_id", project.OwnerId);
            command.Parameters.AddWithValue("is_archived", project.IsArchived);

            var result = await command.ExecuteScalarAsync();     
            
            if (result != null && Guid.TryParse(result.ToString(), out Guid insertedId))
            {
                return insertedId;
            }
            throw new GraphQLException("не вдалось отримати ID створеного користувача");
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
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty,
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
