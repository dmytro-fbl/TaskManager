namespace TaskManager.API.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int TotalProjects { get; set; }
        public decimal TotalBudgetHours { get; set; }
        public decimal TotalUsedHours { get; set; }
        public int ProjectsOnTrack { get; set; }
        public int ProjectsAtRisk { get; set; }
        public int ProjectsOverBudget { get; set; }
    }
}