namespace TaskManager.API.DTOs.Projects
{
    public class ProjectRoleDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
    }
}
