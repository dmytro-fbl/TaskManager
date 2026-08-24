namespace TaskManager.API.DTOs.Dashboard
{
    public class ManagerProjectDashboardDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal BudgetHours { get; set; }
        public decimal UsedHours { get; set; }
        public List<RoleHoursDto> RolesHours { get; set; } = new();
    }

    public class RoleHoursDto
    {
        public string RoleName { get; set; } = string.Empty;
        public decimal UsedHours { get; set; }
    }
}