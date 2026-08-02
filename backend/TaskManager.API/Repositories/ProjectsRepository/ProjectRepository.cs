using Npgsql;
using TaskManager.API.Models;
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

        public async Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId)
        {
            var members = new List<User>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            string sql = @"
                SELECT u.id, u.name, u.email, u.is_admin, u.is_active, u.created_at
                FROM app.users u
                INNER JOIN app.project_memberships pm ON u.id = pm.user_id
                WHERE pm.project_id = @project_id
                ORDER BY u.name;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                members.Add(new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
                });
            }

            return members;
        }

        public async Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(Guid projectId)
        {
            var memberships = new List<ProjectMembership>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            string sql = @"
                SELECT id, project_id, user_id, project_role, role_label_id, joined_at
                FROM app.project_memberships
                WHERE project_id = @project_id
                ORDER BY joined_at DESC;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                memberships.Add(new ProjectMembership
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    ProjectId = reader.GetGuid(reader.GetOrdinal("project_id")),
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                    ProjectRole = reader.GetString(reader.GetOrdinal("project_role")),
                    RoleLabelId = reader.IsDBNull(reader.GetOrdinal("role_label_id"))
                        ? null : reader.GetGuid(reader.GetOrdinal("role_label_id")),
                    JoinedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("joined_at"))
                });
            }

            return memberships;
        }

        public async Task<bool> IsUserInProjectAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT COUNT(1) FROM app.project_memberships
                WHERE project_id = @project_id AND user_id = @user_id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("user_id", userId);

            var result = (long?)await command.ExecuteScalarAsync() ?? 0;
            return result > 0;
        }

        public async Task<bool> InviteUserToProjectAsync(Guid projectId, string email, string projectRole)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            try
            {
                // Check if project exists
                const string projectCheckSql = "SELECT id FROM app.projects WHERE id = @project_id;";
                await using var projectCheckCmd = new NpgsqlCommand(projectCheckSql, connection);
                projectCheckCmd.Parameters.AddWithValue("project_id", projectId);

                var projectExists = await projectCheckCmd.ExecuteScalarAsync();
                if (projectExists == null)
                {
                    throw new GraphQLException("Проект не знайдено");
                }

                // Get user by email
                const string userSql = "SELECT id FROM app.users WHERE email = @email AND is_active = true;";
                await using var userCmd = new NpgsqlCommand(userSql, connection);
                userCmd.Parameters.AddWithValue("email", email);

                var userIdObj = await userCmd.ExecuteScalarAsync();
                if (userIdObj == null || !Guid.TryParse(userIdObj.ToString(), out var userId))
                {
                    throw new GraphQLException($"Користувач з email '{email}' не знайдено або не активний");
                }

                // Check if user already in project
                const string memberCheckSql = "SELECT COUNT(1) FROM app.project_memberships WHERE project_id = @project_id AND user_id = @user_id;";
                await using var memberCheckCmd = new NpgsqlCommand(memberCheckSql, connection);
                memberCheckCmd.Parameters.AddWithValue("project_id", projectId);
                memberCheckCmd.Parameters.AddWithValue("user_id", userId);

                var memberCount = (long?)await memberCheckCmd.ExecuteScalarAsync() ?? 0;
                if (memberCount > 0)
                {
                    throw new GraphQLException("Користувач вже є членом цього проекту");
                }

                // Add user to project
                const string addMemberSql = @"
                    INSERT INTO app.project_memberships (project_id, user_id, project_role)
                    VALUES (@project_id, @user_id, @project_role);
                ";

                await using var addCmd = new NpgsqlCommand(addMemberSql, connection);
                addCmd.Parameters.AddWithValue("project_id", projectId);
                addCmd.Parameters.AddWithValue("user_id", userId);
                addCmd.Parameters.AddWithValue("project_role", projectRole ?? "member");

                await addCmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex) when (!(ex is GraphQLException))
            {
                throw new GraphQLException($"Помилка при додаванні користувача до проекту: {ex.Message}");
            }
        }
    }
}
