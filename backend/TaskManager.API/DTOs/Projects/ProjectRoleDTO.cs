namespace TaskManager.API.DTOs.Projects
{
    public class ProjectRoleDTO
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
