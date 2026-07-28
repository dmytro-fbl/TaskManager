using Microsoft.AspNetCore.Components.Web;
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
                INSERT INTO app.users (name, email,  invite_token, invite_expires_at, is_admin, is_active, 
                                        refresh_token, refresh_token_expiry_time) 
                VALUES (@name, @email, @invite_token, @invite_expires_at, @is_admin, @is_active,
                        @refresh_token, @refresh_token_expiry_time) 
)
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("name", user.Name);
            command.Parameters.AddWithValue("email", user.Email);
            command.Parameters.AddWithValue("is_admin", user.IsAdmin);
            command.Parameters.AddWithValue("is_active", user.IsActive);

            command.Parameters.AddWithValue("invite_token", user.InviteToken ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("invite_expires_at", user.InviteExpiresAt ?? (object)DBNull.Value);

            command.Parameters.AddWithValue("refresh_token", user.RefreshToken ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("refresh_token_expiry_time", user.RefreshTokenExpiryTime ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var users = new List<User>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, email, is_admin, is_active, created_at
                FROM app.users
                ORDER BY created_at DESC;
            
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                });
            }
            return users;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, email, password_hash, password_salt, is_admin, is_active,
                        refresh_token, refresh_token_expiry_time
                FROM app.users WHERE email = @email
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("email", email);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),

                    PasswordHash = reader.IsDBNull(reader.GetOrdinal("password_hash"))
                    ? null : reader.GetString(reader.GetOrdinal("password_hash")),

                    PasswordSalt = reader.IsDBNull(reader.GetOrdinal("password_salt"))
                    ? null : reader.GetString(reader.GetOrdinal("password_salt")),

                    RefreshToken = reader.IsDBNull(reader.GetOrdinal("refresh_token"))
                    ? null : reader.GetString(reader.GetOrdinal("refresh_token")),

                    RefreshTokenExpiryTime = reader.IsDBNull(reader.GetOrdinal("refresh_token_expiry_time"))
                    ? null : reader.GetFieldValue<DateTime>(reader.GetOrdinal("refresh_token_expiry_time"))
                };
            }

            return null;
        }

        public async Task<string> GenerateInviteAsync(string email, bool isAdmin)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string checkSql = "SELECT id FROM app.users WHERE email = @Email";

            await using var checkCommand = new NpgsqlCommand(checkSql, connection);

            checkCommand.Parameters.AddWithValue("email", email);

            var existingUser = await checkCommand.ExecuteScalarAsync();
            if (existingUser != null)
            {
                throw new GraphQLException("Користувач з таким email вже існує або отримав запрошення");
            }

            string inviteToken = Guid.NewGuid().ToString();

            DateTime expiresAt = DateTime.UtcNow.AddHours(24);

            const string sql = @"
                INSERT INTO app.users (email, name, invite_token, invite_expires_at, is_active, is_admin)
                VALUES (@Email, 'Pending User', @Token, @ExpiresAt, false, @IsAdmin)  
                RETURNING invite_token;                               
            ";

            await using var insertCommand = new NpgsqlCommand(sql, connection);

            insertCommand.Parameters.AddWithValue("Email", email);
            insertCommand.Parameters.AddWithValue("Token", inviteToken);
            insertCommand.Parameters.AddWithValue("ExpiresAt", expiresAt);
            insertCommand.Parameters.AddWithValue("IsAdmin", isAdmin);

            var resultToken = (string?)await insertCommand.ExecuteScalarAsync();

            return resultToken ?? throw new GraphQLException("Помилка генерації токена");
        }

        public async Task<string?> GetEmailByInviteTokenAsync(string token)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT email
                FROM app.users
                WHERE invite_token = @Token
                    AND invite_expires_at > now()
                    AND password_hash IS NULL;
                
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("Token", token);

            var email = (string?)await command.ExecuteScalarAsync();

            return email;
        }

        public async Task<bool> CompleteRegistrationAsync(string token, string name, string passwordHash, string passwordSalt)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET name = @Name,
                    password_hash = @Hash,
                    password_salt = @Salt,
                    invite_token = NULL,
                    invite_expires_at = NULL,
                    is_active = true
                WHERE invite_token = @Token
                    AND invite_expires_at > now();
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("Name", name);
            command.Parameters.AddWithValue("Hash", passwordHash);
            command.Parameters.AddWithValue("Salt", passwordSalt);
            command.Parameters.AddWithValue("Token", token);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, name, is_admin, email, refresh_token, refresh_token_expiry_time
                FROM app.users
                WHERE id = @id
                ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    RefreshToken = reader.IsDBNull(reader.GetOrdinal("refresh_token"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("refresh_token")),

                    RefreshTokenExpiryTime = reader.IsDBNull(reader.GetOrdinal("refresh_token_expiry_time"))
                    ? null
                    : reader.GetFieldValue<DateTime>(reader.GetOrdinal("refresh_token_expiry_time"))

                };
            }

            return null;
        }

        public async Task<bool> UpdateUserRoleAsync(Guid userId, bool isAdmin)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
            UPDATE app.users
            SET is_admin = @is_admin,
                updated_at = now()
            WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("is_admin", isAdmin);
            command.Parameters.AddWithValue("id", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;

        }

        public async Task<bool> UpdateUserStatusAsync(Guid userId, bool isActive)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
            UPDATE app.users
            SET is_active = @is_active,
                updated_at = now()
            WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("is_active", isActive);
            command.Parameters.AddWithValue("id", userId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateRefreshTokenAsync(Guid userId, string? refreshToken, DateTime? expiryTime)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.users
                SET refresh_token = @refresh_token,
                    refresh_token_expiry_time = @refresh_token_expiry_time
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("id", userId);
            command.Parameters.AddWithValue("refresh_token", refreshToken ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("refresh_token_expiry_time", expiryTime ?? (object)DBNull.Value);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<IEnumerable<User>> GetUsersPendingInviteAsync()
        {
            var users = new List<User>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT is_active = false
                FROM app.users
                ORDER BY created_at DESC;
            ";

            await using var command = new NpgsqlCommand( sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    InviteToken = reader.GetString(reader.GetOrdinal("invite_token")),
                    InviteExpiresAt = reader.GetDateTime(reader.GetOrdinal("invite_expires_at"))                    
                });
            }
            return users;
        }
    }
}
