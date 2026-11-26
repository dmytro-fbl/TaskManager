using Dapper;
using Npgsql;
using TaskManager.API.Models;

namespace TaskManager.API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public UserRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task Create(User user)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO app.users (name, email, invite_token, invite_expires_at, is_admin, is_active,
                                       refresh_token, refresh_token_expiry_time)
                VALUES (@Name, @Email, @InviteToken, @InviteExpiresAt, @IsAdmin, @IsActive,
                        @RefreshToken, @RefreshTokenExpiryTime);";

            await connection.ExecuteAsync(sql, user);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, email, is_admin, is_active, created_at
                FROM app.users
                ORDER BY created_at DESC;";

            return await connection.QueryAsync<User>(sql);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, email, password_hash, password_salt, is_admin, is_active,
                       refresh_token, refresh_token_expiry_time
                FROM app.users
                WHERE email = @email;";

            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email });
        }

        public async Task<string> GenerateInviteAsync(string email, bool isAdmin)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string checkSql = "SELECT is_active FROM app.users WHERE email = @email;";
            var isActive = await connection.QuerySingleOrDefaultAsync<bool?>(checkSql, new { email });

            if (isActive == true)
            {
                throw new GraphQLException("Користувач з таким email вже зареєстрований та активний.");
            }

            var inviteToken = Guid.NewGuid().ToString("N");
            var expiresAt = DateTime.UtcNow.AddHours(24);

            if (isActive.HasValue)
            {
                const string updateSql = @"
                    UPDATE app.users
                    SET invite_token = @inviteToken,
                        invite_expires_at = @expiresAt,
                        is_admin = @isAdmin
                    WHERE email = @email
                    RETURNING invite_token;";

                var updatedToken = await connection.ExecuteScalarAsync<string?>(
                    updateSql,
                    new { email, inviteToken, expiresAt, isAdmin });

                return updatedToken ?? throw new GraphQLException("Помилка оновлення токена.");
            }

            const string insertSql = @"
                INSERT INTO app.users (email, name, invite_token, invite_expires_at, is_active, is_admin)
                VALUES (@email, 'Pending User', @inviteToken, @expiresAt, false, @isAdmin)
                RETURNING invite_token;";

            var insertedToken = await connection.ExecuteScalarAsync<string?>(
                insertSql,
                new { email, inviteToken, expiresAt, isAdmin });

            return insertedToken ?? throw new GraphQLException("Помилка генерації токена.");
        }

        public async Task<string?> GetEmailByInviteTokenAsync(string token)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT email
                FROM app.users
                WHERE invite_token = @token
                  AND invite_expires_at > now()
                  AND password_hash IS NULL;";

            return await connection.QuerySingleOrDefaultAsync<string?>(sql, new { token });
        }

        public async Task<bool> CompleteRegistrationAsync(string token, string name, string passwordHash, string passwordSalt)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET name = @name,
                    password_hash = @passwordHash,
                    password_salt = @passwordSalt,
                    invite_token = NULL,
                    invite_expires_at = NULL,
                    is_active = true
                WHERE invite_token = @token
                  AND invite_expires_at > now();";

            return await connection.ExecuteAsync(sql, new { name, passwordHash, passwordSalt, token }) > 0;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, is_admin, email, refresh_token, refresh_token_expiry_time
                FROM app.users
                WHERE id = @id;";

            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { id });
        }

        public async Task<bool> UpdateUserRoleAsync(Guid userId, bool isAdmin)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET is_admin = @isAdmin,
                    updated_at = now()
                WHERE id = @userId;";

            return await connection.ExecuteAsync(sql, new { userId, isAdmin }) > 0;
        }

        public async Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET is_active = @isActive,
                    updated_at = now()
                WHERE id = @userId;";

            return await connection.ExecuteAsync(sql, new { userId, isActive }) > 0;
        }

        public async Task<bool> UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiryTime)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET refresh_token = @refreshToken,
                    refresh_token_expiry_time = @expiryTime
                WHERE id = @userId;";

            return await connection.ExecuteAsync(sql, new { userId, refreshToken, expiryTime }) > 0;
        }

        public async Task<IEnumerable<User>> GetUsersPendingInviteAsync()
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, email, is_admin, is_active, created_at, invite_token, invite_expires_at
                FROM app.users
                WHERE invite_token IS NOT NULL AND password_hash IS NULL
                ORDER BY created_at DESC;";

            return await connection.QueryAsync<User>(sql);
        }
    }
}
