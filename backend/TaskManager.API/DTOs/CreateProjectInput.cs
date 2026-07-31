namespace TaskManager.API.DTOs
{
    public record CreateProjectInput(
        string Title,
        string? Description,
        decimal? BudgetCap
    );

}
