namespace TaskManager.API.DTOs.Projects
{
    public record CreateProjectInput(
        string Title,
        string? Description,
        decimal? BudgetCap,
        DateTimeOffset Deadline
    );

}
