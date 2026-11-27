using Dapper;
using Npgsql;
using TaskManager.API.Models;
using TaskManager.API.Models.Constants;
using TaskManager.API.Models.ProjectsTables;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public class ProjectMembershipRepository : IProjectMembershipRepository
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<ProjectMembershipRepository> _logger;

        public ProjectMembershipRepository(
            NpgsqlDataSource dataSource,
            ILogger<ProjectMembershipRepository> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<IEnumerable<User>> GetProjectMembersAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT u.id, u.name, u.email, u.is_admin, u.is_active, u.created_at
                FROM app.users u
                INNER JOIN app.project_memberships pm ON u.id = pm.user_id
                WHERE pm.project_id = @projectId
                ORDER BY u.name;";

            return await connection.QueryAsync<User>(sql, new { projectId });
        }

        public async Task<IEnumerable<ProjectMembership>> GetProjectMembershipsAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, project_id, user_id, project_role, role_label_id, joined_at
                FROM app.project_memberships
                WHERE project_id = @projectId
                ORDER BY joined_at DESC;";

            return await connection.QueryAsync<ProjectMembership>(sql, new { projectId });
        }

        public async Task<bool> IsUserInProjectAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.projects p
                    LEFT JOIN app.project_memberships pm ON p.id = pm.project_id AND pm.user_id = @userId
                    WHERE p.id = @projectId
                      AND (p.owner_id = @userId OR pm.user_id = @userId)
                );";

            return await connection.ExecuteScalarAsync<bool>(sql, new { projectId, userId });
        }

        public async Task<string?> GetUserProjectRoleAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    CASE
                        WHEN p.owner_id = @userId THEN 'manager'
                        ELSE pm.project_role
                    END
                FROM app.projects p
                LEFT JOIN app.project_memberships pm ON p.id = pm.project_id AND pm.user_id = @userId
                WHERE p.id = @projectId;";

            return await connection.ExecuteScalarAsync<string?>(sql, new { projectId, userId });
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
                SET project_role = @projectRole,
                    role_label_id = @roleLabelId
                WHERE project_id = @projectId AND user_id = @userId;";

            return await connection.ExecuteAsync(sql, new { projectId, userId, projectRole, roleLabelId }) > 0;
        }

        public async Task<bool> RemoveMemberAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.project_memberships
                WHERE project_id = @projectId AND user_id = @userId;";

            return await connection.ExecuteAsync(sql, new { projectId, userId }) > 0;
        }

        public async Task<bool> InviteUserToProjectAsync(Guid projectId, string email, string projectRole)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            try
            {
                const string projectCheckSql = "SELECT EXISTS(SELECT 1 FROM app.projects WHERE id = @projectId);";
                if (!await connection.ExecuteScalarAsync<bool>(projectCheckSql, new { projectId }))
                {
                    throw new GraphQLException("Проект не знайдено");
                }

                const string userSql = "SELECT id FROM app.users WHERE email = @email AND is_active = true;";
                var userId = await connection.QuerySingleOrDefaultAsync<Guid?>(userSql, new { email });
                if (userId is null)
                {
                    throw new GraphQLException($"Користувач з email '{email}' не знайдено або не активний");
                }

                const string memberCheckSql = @"
                    SELECT EXISTS(
                        SELECT 1 FROM app.project_memberships
                        WHERE project_id = @projectId AND user_id = @userId
                    );";
                if (await connection.ExecuteScalarAsync<bool>(memberCheckSql, new { projectId, userId }))
                {
                    throw new GraphQLException("Користувач вже є членом цього проекту");
                }

                const string addMemberSql = @"
                    INSERT INTO app.project_memberships (project_id, user_id, project_role)
                    VALUES (@projectId, @userId, @projectRole);";

                await connection.ExecuteAsync(addMemberSql, new
                {
                    projectId,
                    userId = userId.Value,
                    projectRole = projectRole ?? ProjectRoles.Contributor
                });

                return true;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                _logger.LogError(ex, "Failed to add user {Email} to project {ProjectId}", email, projectId);
                throw new GraphQLException("Не вдалося додати користувача до проєкту. Спробуйте пізніше.");
            }
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
                const string projectCheckSql = "SELECT EXISTS(SELECT 1 FROM app.projects WHERE id = @projectId);";
                if (!await connection.ExecuteScalarAsync<bool>(projectCheckSql, new { projectId }))
                {
                    throw new GraphQLException("Проект не знайдено");
                }

                const string userSql = @"
                    SELECT id
                    FROM app.users
                    WHERE email = @email
                      AND is_active = true
                      AND password_hash IS NOT NULL;";

                var userId = await connection.QuerySingleOrDefaultAsync<Guid?>(userSql, new { email });
                if (userId is null)
                {
                    throw new GraphQLException($"Користувач з email '{email}' не знайдено або він не активний/не зареєстрований.");
                }

                const string memberCheckSql = @"
                    SELECT EXISTS(
                        SELECT 1 FROM app.project_memberships
                        WHERE project_id = @projectId AND user_id = @userId
                    );";
                if (await connection.ExecuteScalarAsync<bool>(memberCheckSql, new { projectId, userId }))
                {
                    throw new GraphQLException("Користувач вже є членом цього проекту.");
                }

                var token = Guid.NewGuid().ToString("N");

                const string insertInviteSql = @"
                    INSERT INTO app.invitations (
                        id, project_id, email, token,
                        project_role, role_label_id,
                        invited_by, expires_at, created_at
                    )
                    VALUES (
                        gen_random_uuid(), @projectId, @email, @token,
                        @projectRole, @roleLabelId,
                        @invitedBy, now() + interval '7 days', now()
                    );";

                var rows = await connection.ExecuteAsync(insertInviteSql, new
                {
                    projectId,
                    email,
                    token,
                    projectRole,
                    roleLabelId,
                    invitedBy
                });

                return rows == 1;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                _logger.LogError(ex, "Failed to create invitation for project {ProjectId}", projectId);
                throw new GraphQLException("Не вдалося створити запрошення. Спробуйте пізніше.");
            }
        }

        public async Task<bool> AcceptProjectInvitationAsync(string token, Guid currentUserId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var tx = await connection.BeginTransactionAsync();

            try
            {
                const string selectSql = @"
                    SELECT i.id, i.project_id, i.email, i.project_role, i.role_label_id, i.expires_at, i.accepted_at,
                           p.is_archived, p.status AS project_status
                    FROM app.invitations i
                    JOIN app.projects p ON p.id = i.project_id
                    WHERE i.token = @token;";

                var invitation = await connection.QuerySingleOrDefaultAsync<InvitationRow>(selectSql, new { token }, tx);
                if (invitation is null || invitation.ExpiresAt <= DateTime.UtcNow || invitation.AcceptedAt is not null)
                {
                    return false;
                }

                if (invitation.IsArchived || invitation.ProjectStatus == ProjectStatuses.Archived)
                {
                    throw new GraphQLException("Проєкт в архіві. Будь-які зміни заборонені.");
                }

                const string userSql = "SELECT email FROM app.users WHERE id = @userId;";
                var currentEmail = await connection.QuerySingleOrDefaultAsync<string?>(userSql, new { userId = currentUserId }, tx);
                if (currentEmail is null)
                {
                    return false;
                }

                if (!string.Equals(currentEmail.Trim(), invitation.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new GraphQLException("Це запрошення створено для іншого email.");
                }

                const string insertMembershipSql = @"
                    INSERT INTO app.project_memberships (
                        id, project_id, user_id, project_role, role_label_id, joined_at
                    )
                    VALUES (
                        gen_random_uuid(), @projectId, @userId, @projectRole, @roleLabelId, now()
                    )
                    ON CONFLICT (project_id, user_id) DO NOTHING;";

                await connection.ExecuteAsync(insertMembershipSql, new
                {
                    projectId = invitation.ProjectId,
                    userId = currentUserId,
                    projectRole = invitation.ProjectRole,
                    roleLabelId = invitation.RoleLabelId
                }, tx);

                const string updateInviteSql = @"
                    UPDATE app.invitations
                    SET accepted_at = now()
                    WHERE id = @invitationId;";

                await connection.ExecuteAsync(updateInviteSql, new { invitationId = invitation.Id }, tx);

                await tx.CommitAsync();
                return true;
            }
            catch (Exception ex) when (ex is not GraphQLException)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to accept project invitation");
                throw new GraphQLException("Не вдалося прийняти запрошення. Спробуйте пізніше.", ex);
            }
        }

        private sealed class InvitationRow
        {
            public Guid Id { get; set; }
            public Guid ProjectId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string ProjectRole { get; set; } = string.Empty;
            public Guid? RoleLabelId { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime? AcceptedAt { get; set; }
            public bool IsArchived { get; set; }
            public string ProjectStatus { get; set; } = string.Empty;
        }
    }
}
