using HotChocolate;

namespace TaskManager.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [GraphQLIgnore]
        public string? PasswordHash { get; set; }

        [GraphQLIgnore]
        public string? PasswordSalt { get; set; }

        [GraphQLIgnore]
        public string? InviteToken { get; set; }

        [GraphQLIgnore]
        public DateTimeOffset? InviteExpiresAt { get; set; }

        public string? AvatarUrl { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; }

        [GraphQLIgnore]
        public string? RefreshToken { get; set; }

        [GraphQLIgnore]
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
