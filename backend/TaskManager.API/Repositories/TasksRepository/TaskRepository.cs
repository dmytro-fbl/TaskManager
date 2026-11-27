using Dapper;
using Npgsql;
using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Models.TasksTables;

namespace TaskManager.API.Repositories.TasksRepository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        private const string TaskColumns = @"
            id, project_id, author_id, status_id,
            title, notes, priority, start_date, due_date,
            created_at, updated_at, completed_at,
            COALESCE((SELECT array_agg(user_id) FROM app.task_assignments WHERE task_id = app.tasks.id), '{}') AS assignee_ids";

        public TaskRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<Guid> CreateTaskAsync(TaskItem task)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var taskId = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id;
            var now = DateTimeOffset.UtcNow;

            const string sql = @"
                INSERT INTO app.tasks (
                    id, project_id, author_id, status_id,
                    title, notes, priority, start_date, due_date,
                    created_at, updated_at, completed_at
                )
                VALUES (
                    @id, @projectId, @authorId, @statusId,
                    @title, @notes, @priority, @startDate, @dueDate,
                    @createdAt, @updatedAt, @completedAt
                )
                RETURNING id;";

            var createdTaskId = await connection.ExecuteScalarAsync<Guid?>(sql, new
            {
                id = taskId,
                projectId = task.ProjectId,
                authorId = task.AuthorId,
                statusId = task.StatusId,
                title = task.Title,
                notes = task.Notes,
                priority = task.Priority,
                startDate = task.StartDate,
                dueDate = task.DueDate,
                createdAt = now,
                updatedAt = now,
                completedAt = task.CompletedAt
            });

            return createdTaskId ?? throw new GraphQLException("Не вдалося створити таску.");
        }

        public async Task<TaskItem?> GetTaskByIdAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = $@"
                SELECT {TaskColumns}
                FROM app.tasks
                WHERE id = @taskId;";

            return await connection.QuerySingleOrDefaultAsync<TaskItem>(sql, new { taskId });
        }

        public async Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = $@"
                SELECT {TaskColumns}
                FROM app.tasks
                WHERE project_id = @projectId
                ORDER BY created_at DESC;";

            return await connection.QueryAsync<TaskItem>(sql, new { projectId });
        }

        public async Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, task_id, user_id, role_id, estimated_hours, assigned_by, assigned_at
                FROM app.task_assignments
                WHERE task_id = @taskId
                ORDER BY assigned_at;";

            return await connection.QueryAsync<TaskAssignment>(sql, new { taskId });
        }

        public async Task<bool> AddTaskAssignmentAsync(TaskAssignment assignment)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO app.task_assignments
                    (id, task_id, user_id, role_id, estimated_hours, assigned_by, assigned_at)
                VALUES
                    (@id, @taskId, @userId, @roleId, @estimatedHours, @assignedBy, @assignedAt)
                ON CONFLICT (task_id, user_id) DO UPDATE
                SET role_id = EXCLUDED.role_id,
                    estimated_hours = EXCLUDED.estimated_hours;";

            return await connection.ExecuteAsync(sql, new
            {
                id = assignment.Id == Guid.Empty ? Guid.NewGuid() : assignment.Id,
                assignment.TaskId,
                assignment.UserId,
                assignment.RoleId,
                assignment.EstimatedHours,
                assignment.AssignedBy,
                assignment.AssignedAt
            }) > 0;
        }

        public async Task<bool> RemoveTaskAssignmentAsync(Guid taskId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.task_assignments
                WHERE task_id = @taskId
                  AND user_id = @userId;";

            return await connection.ExecuteAsync(sql, new { taskId, userId }) > 0;
        }

        public async Task<bool> UpdateTaskStatusAsync(Guid taskId, Guid statusId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.tasks
                SET status_id = @statusId,
                    updated_at = @updatedAt
                WHERE id = @taskId;";

            return await connection.ExecuteAsync(sql, new
            {
                taskId,
                statusId,
                updatedAt = DateTimeOffset.UtcNow
            }) > 0;
        }

        public async Task<bool> IsProjectStatusAsync(Guid projectId, Guid statusId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.project_statuses
                    WHERE project_id = @projectId
                      AND id = @statusId
                );";

            return await connection.ExecuteScalarAsync<bool>(sql, new { projectId, statusId });
        }

        public async Task<bool> HasUserTasksInProjectAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.tasks t
                    LEFT JOIN app.task_assignments ta ON t.id = ta.task_id
                    WHERE t.project_id = @projectId
                      AND (t.author_id = @userId OR ta.user_id = @userId)
                );";

            return await connection.ExecuteScalarAsync<bool>(sql, new { projectId, userId });
        }

        public async Task<bool> UpdateTaskAsync(UpdateTaskInput input)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.tasks
                SET title = @title,
                    notes = @notes,
                    priority = @priority,
                    start_date = @startDate,
                    due_date = @dueDate,
                    updated_at = @updatedAt
                WHERE id = @taskId;";

            return await connection.ExecuteAsync(sql, new
            {
                taskId = input.TaskId,
                title = input.Title.Trim(),
                notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
                priority = input.Priority.Trim().ToLowerInvariant(),
                startDate = input.StartDate,
                dueDate = input.DueDate,
                updatedAt = DateTimeOffset.UtcNow
            }) > 0;
        }

        public async Task<bool> AddWorkLogAsync(Guid userId, WorkLogInput input)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var now = DateTimeOffset.UtcNow;

            const string sql = @"
                INSERT INTO app.worklogs (task_id, user_id, hours_spent, log_date, description, created_at)
                VALUES (@taskId, @userId, @hoursSpent, @logDate, @description, @createdAt);";

            return await connection.ExecuteAsync(sql, new
            {
                taskId = input.TaskId,
                userId,
                hoursSpent = input.HoursSpent,
                logDate = DateOnly.FromDateTime(now.UtcDateTime),
                description = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim(),
                createdAt = now
            }) > 0;
        }

        public async Task<WorkLogDTO?> GetWorkLogByIdAsync(Guid workLogId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    w.id AS Id,
                    w.task_id,
                    w.user_id,
                    w.hours_spent,
                    w.log_date,
                    w.description AS Comment
                FROM app.worklogs w
                WHERE w.id = @id;";

            return await connection.QuerySingleOrDefaultAsync<WorkLogDTO>(sql, new { id = workLogId });
        }

        public async Task<bool> UpdateWorkLogAsync(Guid workLogId, decimal hoursSpent, string? comment)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.worklogs
                SET hours_spent = @hoursSpent,
                    description = @description
                WHERE id = @id;";

            return await connection.ExecuteAsync(sql, new
            {
                id = workLogId,
                hoursSpent,
                description = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            }) > 0;
        }

        public async Task<bool> DeleteWorkLogAsync(Guid workLogId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.worklogs
                WHERE id = @id;";

            return await connection.ExecuteAsync(sql, new { id = workLogId }) > 0;
        }

        public async Task<IEnumerable<WorkLogDTO>> GetTaskWorkLogsAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT
                    w.id AS Id,
                    w.task_id,
                    u.id AS user_id,
                    u.name AS user_name,
                    prl.name AS role_name,
                    w.hours_spent,
                    w.log_date,
                    w.description AS Comment
                FROM app.worklogs w
                JOIN app.users u ON w.user_id = u.id
                LEFT JOIN app.task_assignments ta ON ta.task_id = w.task_id AND ta.user_id = w.user_id
                LEFT JOIN app.project_role_labels prl ON ta.role_id = prl.id
                WHERE w.task_id = @taskId
                ORDER BY w.log_date DESC;";

            return await connection.QueryAsync<WorkLogDTO>(sql, new { taskId });
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.tasks
                WHERE id = @taskId;";

            return await connection.ExecuteAsync(sql, new { taskId }) > 0;
        }

        public async Task<bool> HasWorkLogsAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.worklogs
                    WHERE task_id = @taskId
                );";

            return await connection.ExecuteScalarAsync<bool>(sql, new { taskId });
        }

        public async Task<bool> IsRoleInProjectAsync(Guid projectId, Guid roleId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.project_role_labels
                    WHERE id = @roleId AND project_id = @projectId
                );";

            return await connection.ExecuteScalarAsync<bool>(sql, new { projectId, roleId });
        }

        public async Task AddProjectHoursAsync(Guid projectId, Guid userId, decimal hours, string? roleLabelName = null)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO app.project_hours (project_id, user_id, hours, role_label_name, created_at)
                VALUES (@projectId, @userId, @hours, @roleLabelName, now());";

            await connection.ExecuteAsync(sql, new { projectId, userId, hours, roleLabelName });
        }
    }
}
