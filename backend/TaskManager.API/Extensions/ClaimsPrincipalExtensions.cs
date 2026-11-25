using System.Security.Claims;

namespace TaskManager.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool TryGetUserId(this ClaimsPrincipal claimsPrincipal, out Guid userId)
        {
            var userIdValue =
                claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdValue, out userId);
        }

        public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.TryGetUserId(out var userId))
            {
                return userId;
            }

            throw new GraphQLException("Не вдалося авторизувати користувача.");
        }
    }
}
