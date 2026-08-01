namespace TaskManager.API.Models.ProjectsTables
{
    public class ProjectMembership
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string ProjectRole { get; set; } = string.Empty;
        public Guid? RoleLabelId { get; set; }
        public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
