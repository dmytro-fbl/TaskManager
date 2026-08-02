namespace TaskManager.API.DTOs
{
    public record ProjectInviteResponse(
        bool Success,
        string Message,
        string? Email
    );
}
