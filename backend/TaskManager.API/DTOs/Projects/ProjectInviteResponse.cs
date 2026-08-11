namespace TaskManager.API.DTOs.Projects
{
    public record ProjectInviteResponse(
        bool Success,
        string Message,
        string? Email
    );
}
