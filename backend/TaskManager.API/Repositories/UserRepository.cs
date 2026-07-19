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

            await using var command = new NpgsqlCommand(
                "INSERT INTO app.users (name, email,  invite_token, invite_expires_at, is_admin, is_active) " +
                "VALUES (@name, @email, @invite_token, @invite_expires_at, @is_admin, @is_active)", connection);

            command.Parameters.AddWithValue("name", user.Name);
            command.Parameters.AddWithValue("email", user.Email);
            command.Parameters.AddWithValue("is_admin", user.IsAdmin);
            command.Parameters.AddWithValue("is_active", user.IsActive);

            command.Parameters.AddWithValue("invite_token", user.InviteToken ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("invite_expires_at", user.InviteExpiresAt ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            await using var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, password_salt, is_admin, is_active " +
                "FROM app.users WHERE email = @email", connection);

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
                    ? null : reader.GetString(reader.GetOrdinal("password_salt"))
                };
            }

            return null;
        }
    }
}
