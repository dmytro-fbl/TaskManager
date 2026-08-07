namespace TaskManager.API.Models.ProjectsTables
{
    public class ProjectStatus
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = "todo";

        public string? Color { get; set; }

        public int SortOrder { get; set; }

        public bool IsFinal { get; set; }

        public DateTimeOffset CreatedAt { get; set; } =
            DateTimeOffset.UtcNow;
    }
}