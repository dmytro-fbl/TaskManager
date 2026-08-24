using Npgsql;
using TaskManager.API.DTOs;
using TaskManager.API.DTOs.Dashboard;
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

        public async Task<bool> CreateProjectInvitationForExistingUserAsync(
            Guid projectId,
            string email,
            string projectRole,
            Guid invitedBy,
            Guid? roleLabelId = null)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            try
            {
                // Перевірка існування проекту
                const string projectCheckSql = "SELECT id FROM app.projects WHERE id = @project_id;";
                await using var projectCheckCmd = new NpgsqlCommand(projectCheckSql, connection);
                projectCheckCmd.Parameters.AddWithValue("project_id", projectId);

                var projectExists = await projectCheckCmd.ExecuteScalarAsync();
                if (projectExists == null)
                {
                    throw new GraphQLException("Проект не знайдено");
                }

                // Знайти існуючого користувача по email (зареєстрований, активний)
                const string userSql = @"
                    SELECT id
                    FROM app.users
                    WHERE email = @email
                      AND is_active = true
                      AND password_hash IS NOT NULL;
                ";
                await using var userCmd = new NpgsqlCommand(userSql, connection);
                userCmd.Parameters.AddWithValue("email", email);

                var userIdObj = await userCmd.ExecuteScalarAsync();
                if (userIdObj == null || !Guid.TryParse(userIdObj.ToString(), out var userId))
                {
                    throw new GraphQLException($"Користувач з email '{email}' не знайдено або він не активний/не зареєстрований.");
                }

                // Перевірити, що юзер ще не в проекті
                const string memberCheckSql = @"
                    SELECT COUNT(1)
                    FROM app.project_memberships
                    WHERE project_id = @project_id AND user_id = @user_id;
                ";
                await using var memberCheckCmd = new NpgsqlCommand(memberCheckSql, connection);
                memberCheckCmd.Parameters.AddWithValue("project_id", projectId);
                memberCheckCmd.Parameters.AddWithValue("user_id", userId);

                var memberCount = (long?)await memberCheckCmd.ExecuteScalarAsync() ?? 0;
                if (memberCount > 0)
                {
                    throw new GraphQLException("Користувач вже є членом цього проекту.");
                }

                // Згенерувати токен інвайту
                var token = Guid.NewGuid().ToString("N");

                const string insertInviteSql = @"
                    INSERT INTO app.invitations (
                        id, project_id, email, token,
                        project_role, role_label_id,
                        invited_by, expires_at, created_at
                    )
                    VALUES (
                        gen_random_uuid(), @project_id, @email, @token,
                        @project_role, @role_label_id,
                        @invited_by, now() + interval '7 days', now()
                    );
                ";

                await using var insertCmd = new NpgsqlCommand(insertInviteSql, connection);
                insertCmd.Parameters.AddWithValue("project_id", projectId);
                insertCmd.Parameters.AddWithValue("email", email);
                insertCmd.Parameters.AddWithValue("token", token);
                insertCmd.Parameters.AddWithValue("project_role", projectRole);
                insertCmd.Parameters.AddWithValue("role_label_id", (object?)roleLabelId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("invited_by", invitedBy);

                var rows = await insertCmd.ExecuteNonQueryAsync();
                return rows == 1;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                throw new GraphQLException($"Помилка при створенні запрошення: {ex.Message}");
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

            while (await reader.ReadAsync())
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
                addCmd.Parameters.AddWithValue("project_role", projectRole ?? "contributor");

                await addCmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex) when (!(ex is GraphQLException))
            {
                throw new GraphQLException($"Помилка при додаванні користувача до проекту: {ex.Message}");
            }
        }

        public async Task<Guid> CreateProjectWithOwnerAsync(Project project)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            const string sql = @"
            INSERT INTO app.projects ( 
                id, title,description, budget_cap,status,owner_id,is_archived,created_at, updated_at
            )
            VALUES (
                @id, @title, @description, @budget_cap, @status, @owner_id, @is_archived, @created_at, @updated_at
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

        public async Task<bool> AcceptProjectInvitationAsync(string token, Guid currentUserId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var tx = await connection.BeginTransactionAsync();

            try
            {
                // 1. Знайти інвайт по токену
                const string selectSql = @"
                    SELECT id, project_id, email, project_role, role_label_id, expires_at, accepted_at
                    FROM app.invitations
                    WHERE token = @token;
                ";

                await using var selectCmd = new NpgsqlCommand(selectSql, connection, tx);
                selectCmd.Parameters.AddWithValue("token", token);

                await using var reader = await selectCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return false;
                }

                var invitationId = reader.GetGuid(reader.GetOrdinal("id"));
                var projectId = reader.GetGuid(reader.GetOrdinal("project_id"));
                var email = reader.GetString(reader.GetOrdinal("email"));
                var projectRole = reader.GetString(reader.GetOrdinal("project_role"));
                var roleLabelId = reader.IsDBNull(reader.GetOrdinal("role_label_id"))
                    ? (Guid?)null
                    : reader.GetGuid(reader.GetOrdinal("role_label_id"));
                var expiresAt = reader.GetDateTime(reader.GetOrdinal("expires_at"));
                var acceptedAt = reader.IsDBNull(reader.GetOrdinal("accepted_at"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("accepted_at"));

                if (expiresAt <= DateTime.UtcNow || acceptedAt is not null)
                {
                    return false;
                }

                await reader.CloseAsync();

                // Перевірити, що поточний юзер відповідає email інвайту
                const string userSql = "SELECT email FROM app.users WHERE id = @user_id;";
                await using var userCmd = new NpgsqlCommand(userSql, connection, tx);
                userCmd.Parameters.AddWithValue("user_id", currentUserId);

                var currentEmailObj = await userCmd.ExecuteScalarAsync();
                if (currentEmailObj == null)
                {
                    return false;
                }

                var currentEmail = (string)currentEmailObj;
                if (!string.Equals(currentEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new GraphQLException("Це запрошення створено для іншого email.");
                }

                // Створити членство, якщо його ще немає
                const string insertMembershipSql = @"
                    INSERT INTO app.project_memberships (
                        id, project_id, user_id, project_role, role_label_id, joined_at
                    )
                    VALUES (
                        gen_random_uuid(), @project_id, @user_id, @project_role, @role_label_id, now()
                    )
                    ON CONFLICT (project_id, user_id) DO NOTHING;
                ";

                await using var membershipCmd = new NpgsqlCommand(insertMembershipSql, connection, tx);
                membershipCmd.Parameters.AddWithValue("project_id", projectId);
                membershipCmd.Parameters.AddWithValue("user_id", currentUserId);
                membershipCmd.Parameters.AddWithValue("project_role", projectRole);
                membershipCmd.Parameters.AddWithValue("role_label_id", (object?)roleLabelId ?? DBNull.Value);

                await membershipCmd.ExecuteNonQueryAsync();

                // Позначити інвайт як прийнятий
                const string updateInviteSql = @"
                    UPDATE app.invitations
                    SET accepted_at = now()
                    WHERE id = @invitation_id;
                ";

                await using var updateCmd = new NpgsqlCommand(updateInviteSql, connection, tx);
                updateCmd.Parameters.AddWithValue("invitation_id", invitationId);
                await updateCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                await tx.RollbackAsync();
                throw new GraphQLException($"Помилка при прийнятті запрошення: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateProjectMembershipRoleAsync(
            Guid projectId,
            Guid userId,
            string projectRole,
            Guid? roleLabelId,
            Guid updatedByUserId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.project_memberships
                SET project_role = @project_role,
                    role_label_id = @role_label_id
                WHERE project_id = @project_id AND user_id = @user_id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("project_role", projectRole);
            command.Parameters.AddWithValue("role_label_id", (object?)roleLabelId ?? DBNull.Value);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
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

        public async Task<bool> UpdateProjectAsync(Guid projectId, string title, string? description, decimal? budgetCap)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.projects
                SET title = @title,
                    description = @description,
                    budget_cap= @budget_cap,
                    updated_at = now()
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand( sql, connection);

            command.Parameters.AddWithValue("id", projectId);
            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("budget_cap", budgetCap ?? (object)DBNull.Value);

            var rowAffected = await command.ExecuteNonQueryAsync();

            return rowAffected > 0;
        }

        public async Task<string?> GetUserProjectRoleAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT project_role
                FROM app.project_memberships
                WHERE project_id = @project_id AND user_id = @user_id;
            ";

            await using var command = new NpgsqlCommand( sql, connection);

            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("user_id", userId);

            var result = await command.ExecuteScalarAsync();

            return result?.ToString();
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

        public async Task<IEnumerable<MyProjectDashboardDto>> GetMyProjectsWithHoursAsync(Guid userId)
        {
            var projects = new List<MyProjectDashboardDto>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    p.id,
                    p.title,
                    p.description,
                    p.budget_cap AS budget_hours,
                    p.status,
                    pm.project_role AS my_role,
                    COALESCE(SUM(w.hours_spent), 0) AS used_hours
                FROM app.projects p
                JOIN app.project_memberships pm ON pm.project_id = p.id
                LEFT JOIN app.tasks t ON t.project_id = p.id
                LEFT JOIN app.worklogs w ON w.task_id = t.id
                WHERE pm.user_id = @user_id
                  AND p.is_archived = false
                GROUP BY
                    p.id, p.title, p.description, p.budget_cap, p.status, pm.project_role
                ORDER BY p.created_at DESC;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                projects.Add(new MyProjectDashboardDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("description")),
                    BudgetHours = reader.IsDBNull(reader.GetOrdinal("budget_hours"))
                        ? null
                        : reader.GetDecimal(reader.GetOrdinal("budget_hours")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    MyRole = reader.GetString(reader.GetOrdinal("my_role")),
                    UsedHours = reader.GetDecimal(reader.GetOrdinal("used_hours"))
                });
            }

            return projects;
        }

        public async Task<IEnumerable<AvailableProjectDto>> GetAvailableProjectsAsync(Guid userId)
        {
            var projects = new List<AvailableProjectDto>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    p.id,
                    p.title,
                    p.description,
                    p.status,
                    u.id AS manager_id,
                    u.name AS manager_name,
                    u.email AS manager_email
                FROM app.projects p
                JOIN app.users u ON u.id = p.owner_id
                WHERE p.is_archived = false
                  AND NOT EXISTS (
                      SELECT 1
                      FROM app.project_memberships pm
                      WHERE pm.project_id = p.id
                        AND pm.user_id = @user_id
                  )
                ORDER BY p.created_at DESC;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                projects.Add(new AvailableProjectDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("description")),
                    Status = reader.GetString(reader.GetOrdinal("status")),
                    ManagerId = reader.GetGuid(reader.GetOrdinal("manager_id")),
                    ManagerName = reader.GetString(reader.GetOrdinal("manager_name")),
                    ManagerEmail = reader.GetString(reader.GetOrdinal("manager_email"))
                });
            }

            return projects;
        }

        public async Task<IEnumerable<ManagerProjectDashboardDto>> GetManagerProjectsWithRoleHoursAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                WITH project_hours AS (
                    SELECT
                        p.id AS project_id,
                        p.title,
                        p.status,
                        p.budget_cap AS budget_hours,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    WHERE pm.user_id = @user_id
                      AND pm.project_role = 'manager'
                      AND p.is_archived = false
                    GROUP BY p.id, p.title, p.status, p.budget_cap
                ),
                role_hours AS (
                    SELECT
                        p.id AS project_id,
                        prl.name AS role_name,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    LEFT JOIN app.project_role_labels prl ON w.role_label_id = prl.id
                    WHERE pm.user_id = @user_id
                      AND pm.project_role = 'manager'
                      AND p.is_archived = false
                    GROUP BY p.id, prl.name
                )
                SELECT
                    ph.project_id,
                    ph.title,
                    ph.status,
                    ph.budget_hours,
                    ph.used_hours,
                    rh.role_name,
                    rh.used_hours AS role_used_hours
                FROM project_hours ph
                LEFT JOIN role_hours rh ON rh.project_id = ph.project_id
                ORDER BY ph.project_id, rh.role_name;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync();

            var projectsDict = new Dictionary<Guid, ManagerProjectDashboardDto>();

            while (await reader.ReadAsync())
            {
                var projectId = reader.GetGuid(reader.GetOrdinal("project_id"));

                if (!projectsDict.TryGetValue(projectId, out var project))
                {
                    project = new ManagerProjectDashboardDto
                    {
                        Id = projectId,
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        Status = reader.GetString(reader.GetOrdinal("status")),
                        BudgetHours = reader.GetDecimal(reader.GetOrdinal("budget_hours")),
                        UsedHours = reader.GetDecimal(reader.GetOrdinal("used_hours")),
                        RolesHours = new List<RoleHoursDto>()
                    };

                    projectsDict[projectId] = project;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("role_name")))
                {
                    project.RolesHours.Add(new RoleHoursDto
                    {
                        RoleName = reader.GetString(reader.GetOrdinal("role_name")),
                        UsedHours = reader.GetDecimal(reader.GetOrdinal("role_used_hours"))
                    });
                }
            }

            return projectsDict.Values;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                WITH user_projects AS (
                    SELECT
                        p.id,
                        p.budget_cap AS budget_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    WHERE pm.user_id = @user_id
                      AND p.is_archived = false
                ),
                used AS (
                    SELECT
                        p.id AS project_id,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    WHERE pm.user_id = @user_id
                      AND p.is_archived = false
                    GROUP BY p.id
                ),
                project_stats AS (
                    SELECT
                        up.id AS project_id,
                        up.budget_hours,
                        u.used_hours,
                        CASE
                            WHEN up.budget_hours IS NULL OR up.budget_hours = 0 THEN 'no_budget'
                            WHEN u.used_hours / up.budget_hours < 0.8 THEN 'on_track'
                            WHEN u.used_hours / up.budget_hours < 1.0 THEN 'at_risk'
                            ELSE 'over_budget'
                        END AS budget_status
                    FROM user_projects up
                    JOIN used u ON u.project_id = up.id
                )
                SELECT
                    COUNT(*) AS total_projects,
                    COALESCE(SUM(budget_hours), 0) AS total_budget_hours,
                    COALESCE(SUM(used_hours), 0) AS total_used_hours,
                    SUM(CASE WHEN budget_status = 'on_track' THEN 1 ELSE 0 END) AS projects_on_track,
                    SUM(CASE WHEN budget_status = 'at_risk' THEN 1 ELSE 0 END) AS projects_at_risk,
                    SUM(CASE WHEN budget_status = 'over_budget' THEN 1 ELSE 0 END) AS projects_over_budget
                FROM project_stats;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new DashboardStatsDto
                {
                    TotalProjects = reader.GetInt32(reader.GetOrdinal("total_projects")),
                    TotalBudgetHours = reader.GetDecimal(reader.GetOrdinal("total_budget_hours")),
                    TotalUsedHours = reader.GetDecimal(reader.GetOrdinal("total_used_hours")),
                    ProjectsOnTrack = reader.GetInt32(reader.GetOrdinal("projects_on_track")),
                    ProjectsAtRisk = reader.GetInt32(reader.GetOrdinal("projects_at_risk")),
                    ProjectsOverBudget = reader.GetInt32(reader.GetOrdinal("projects_over_budget"))
                };
            }

            return new DashboardStatsDto();
        }
    }
}
