using Dapper;
using Npgsql;
using TaskManager.API.DTOs.Projects;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public class ProjectRoleRepository : IProjectRoleRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public ProjectRoleRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IEnumerable<ProjectRoleDTO>> GetProjectRolesAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, project_id, name
                FROM app.project_role_labels
                WHERE project_id = @projectId
                ORDER BY name ASC;";

            return await connection.QueryAsync<ProjectRoleDTO>(sql, new { projectId });
        }

        public async Task<ProjectRoleDTO> CreateProjectRoleAsync(Guid projectId, string roleName)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var trimmedName = roleName.Trim();

            const string checkSql = @"
                SELECT EXISTS(
                    SELECT 1
                    FROM app.project_role_labels
                    WHERE project_id = @projectId AND name = @name
                );";

            if (await connection.ExecuteScalarAsync<bool>(checkSql, new { projectId, name = trimmedName }))
            {
                throw new GraphQLException("Роль з такою назвою вже існує в цьому проєкті.");
            }

            const string insertSql = @"
                INSERT INTO app.project_role_labels (project_id, name)
                VALUES (@projectId, @name)
                RETURNING id, project_id, name;";

            var created = await connection.QuerySingleOrDefaultAsync<ProjectRoleDTO>(
                insertSql,
                new { projectId, name = trimmedName });

            return created ?? throw new GraphQLException("Не вдалося створити роль.");
        }

        public async Task<ProjectRoleDTO> UpdateProjectRoleAsync(Guid projectId, Guid roleId, string newName)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var trimmedName = newName.Trim();

            const string checkSql = @"
                SELECT EXISTS(
                    SELECT 1
                    FROM app.project_role_labels
                    WHERE project_id = @projectId AND name = @name AND id != @roleId
                );";

            if (await connection.ExecuteScalarAsync<bool>(checkSql, new { projectId, name = trimmedName, roleId }))
            {
                throw new GraphQLException("Роль з такою назвою вже існує на цьому проекті.");
            }

            const string updateSql = @"
                UPDATE app.project_role_labels
                SET name = @name
                WHERE id = @roleId AND project_id = @projectId
                RETURNING id, project_id, name;";

            var updated = await connection.QuerySingleOrDefaultAsync<ProjectRoleDTO>(
                updateSql,
                new { name = trimmedName, roleId, projectId });

            return updated ?? throw new GraphQLException("Не вдалося оновити роль.");
        }

        public async Task<bool> DeleteProjectRoleAsync(Guid roleId, Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.project_role_labels
                WHERE id = @roleId AND project_id = @projectId;";

            return await connection.ExecuteAsync(sql, new { roleId, projectId }) > 0;
        }
    }
}
