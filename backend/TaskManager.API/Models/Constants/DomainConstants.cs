namespace TaskManager.API.Models.Constants
{
    public static class TaskPriorities
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Critical = "critical";

        public static readonly IReadOnlySet<string> Allowed =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Low,
                Medium,
                High,
                Critical
            };
    }

    public static class ProjectStatuses
    {
        public const string Active = "active";
        public const string Archived = "archived";
    }

    public static class ProjectRoles
    {
        public const string Manager = "manager";
        public const string Contributor = "contributor";

        public static readonly IReadOnlySet<string> Allowed =
            new HashSet<string>(StringComparer.Ordinal)
            {
                Manager,
                Contributor
            };
    }
}
