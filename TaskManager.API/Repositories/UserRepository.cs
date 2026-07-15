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

        public Task<User?> GetUserByEmail(string email)
        {
            throw new NotImplementedException();
        }
    }
}
