using Dapper;
using Npgsql;
using TaskManager.API.DTOs.Dashboard;

namespace TaskManager.API.Repositories.ProjectsRepository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public DashboardRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IEnumerable<MyProjectDashboardDto>> GetMyProjectsWithHoursAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    p.id,
                    p.title,
                    p.description,
                    p.budget_cap AS budget_hours,
                    p.status,
                    pm.project_role AS my_role,
                    COALESCE(SUM(w.hours_spent), 0) AS used_hours
                FROM app.projects p
                JOIN app.project_memberships pm ON pm.project_id = p.id
                LEFT JOIN app.tasks t ON t.project_id = p.id
                LEFT JOIN app.worklogs w ON w.task_id = t.id
                WHERE pm.user_id = @userId
                  AND p.is_archived = false
                GROUP BY
                    p.id, p.title, p.description, p.budget_cap, p.status, pm.project_role, p.created_at
                ORDER BY p.created_at DESC;";

            return await connection.QueryAsync<MyProjectDashboardDto>(sql, new { userId });
        }

        public async Task<IEnumerable<AvailableProjectDto>> GetAvailableProjectsAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    p.id,
                    p.title,
                    p.description,
                    p.status,
                    u.id AS manager_id,
                    u.name AS manager_name,
                    u.email AS manager_email
                FROM app.projects p
                JOIN app.users u ON u.id = p.owner_id
                WHERE p.is_archived = false
                  AND NOT EXISTS (
                      SELECT 1
                      FROM app.project_memberships pm
                      WHERE pm.project_id = p.id
                        AND pm.user_id = @userId
                  )
                ORDER BY p.created_at DESC;";

            return await connection.QueryAsync<AvailableProjectDto>(sql, new { userId });
        }

        public async Task<IEnumerable<ManagerProjectDashboardDto>> GetManagerProjectsWithRoleHoursAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                WITH project_hours AS (
                    SELECT
                        p.id AS project_id,
                        p.title,
                        p.status,
                        COALESCE(p.budget_cap, 0) AS budget_hours,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    WHERE pm.user_id = @userId
                      AND pm.project_role = 'manager'
                      AND p.is_archived = false
                    GROUP BY p.id, p.title, p.status, p.budget_cap
                ),
                role_hours AS (
                    SELECT
                        p.id AS project_id,
                        prl.name AS role_name,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    LEFT JOIN app.project_role_labels prl ON w.role_label_id = prl.id
                    WHERE pm.user_id = @userId
                      AND pm.project_role = 'manager'
                      AND p.is_archived = false
                    GROUP BY p.id, prl.name
                )
                SELECT
                    ph.project_id,
                    ph.title,
                    ph.status,
                    ph.budget_hours,
                    ph.used_hours,
                    rh.role_name,
                    COALESCE(rh.used_hours, 0) AS role_used_hours
                FROM project_hours ph
                LEFT JOIN role_hours rh ON rh.project_id = ph.project_id
                ORDER BY ph.project_id, rh.role_name;";

            var rows = await connection.QueryAsync<ManagerProjectRoleRow>(sql, new { userId });

            var projects = new Dictionary<Guid, ManagerProjectDashboardDto>();
            foreach (var row in rows)
            {
                if (!projects.TryGetValue(row.ProjectId, out var project))
                {
                    project = new ManagerProjectDashboardDto
                    {
                        Id = row.ProjectId,
                        Title = row.Title,
                        Status = row.Status,
                        BudgetHours = row.BudgetHours,
                        UsedHours = row.UsedHours,
                        RolesHours = new List<RoleHoursDto>()
                    };
                    projects[row.ProjectId] = project;
                }

                if (row.RoleName is not null)
                {
                    project.RolesHours.Add(new RoleHoursDto
                    {
                        RoleName = row.RoleName,
                        UsedHours = row.RoleUsedHours
                    });
                }
            }

            return projects.Values;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                WITH user_projects AS (
                    SELECT
                        p.id,
                        p.budget_cap AS budget_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    WHERE pm.user_id = @userId
                      AND p.is_archived = false
                ),
                used AS (
                    SELECT
                        p.id AS project_id,
                        COALESCE(SUM(w.hours_spent), 0) AS used_hours
                    FROM app.projects p
                    JOIN app.project_memberships pm ON pm.project_id = p.id
                    LEFT JOIN app.tasks t ON t.project_id = p.id
                    LEFT JOIN app.worklogs w ON w.task_id = t.id
                    WHERE pm.user_id = @userId
                      AND p.is_archived = false
                    GROUP BY p.id
                ),
                project_stats AS (
                    SELECT
                        up.id AS project_id,
                        up.budget_hours,
                        u.used_hours,
                        CASE
                            WHEN up.budget_hours IS NULL OR up.budget_hours = 0 THEN 'no_budget'
                            WHEN u.used_hours / up.budget_hours < 0.8 THEN 'on_track'
                            WHEN u.used_hours / up.budget_hours <= 1.0 THEN 'at_risk'
                            ELSE 'over_budget'
                        END AS budget_status
                    FROM user_projects up
                    JOIN used u ON u.project_id = up.id
                )
                SELECT
                    COUNT(*)::int AS total_projects,
                    COALESCE(SUM(budget_hours), 0) AS total_budget_hours,
                    COALESCE(SUM(used_hours), 0) AS total_used_hours,
                    COALESCE(SUM(CASE WHEN budget_status = 'on_track' THEN 1 ELSE 0 END), 0)::int AS projects_on_track,
                    COALESCE(SUM(CASE WHEN budget_status = 'at_risk' THEN 1 ELSE 0 END), 0)::int AS projects_at_risk,
                    COALESCE(SUM(CASE WHEN budget_status = 'over_budget' THEN 1 ELSE 0 END), 0)::int AS projects_over_budget
                FROM project_stats;";

            return await connection.QuerySingleOrDefaultAsync<DashboardStatsDto>(sql, new { userId })
                   ?? new DashboardStatsDto();
        }

        private sealed class ManagerProjectRoleRow
        {
            public Guid ProjectId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public decimal BudgetHours { get; set; }
            public decimal UsedHours { get; set; }
            public string? RoleName { get; set; }
            public decimal RoleUsedHours { get; set; }
        }
    }
}
