using Npgsql;
using TaskManager.API.Models.TasksTables;

namespace TaskManager.API.Repositories.TasksRepository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        private const string TaskColumns = @"
            id, project_id, author_id, assignee_id, status_id,
            title, notes, priority, start_date, due_date,
            estimated_budget, estimated_unit,
            created_at, updated_at, completed_at";

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
                    id, project_id, author_id, assignee_id, status_id,
                    title, notes, priority, start_date, due_date,
                    estimated_budget, estimated_unit,
                    created_at, updated_at, completed_at
                )
                VALUES (
                    @id, @project_id, @author_id, @assignee_id, @status_id,
                    @title, @notes, @priority, @start_date, @due_date,
                    @estimated_budget, @estimated_unit,
                    @created_at, @updated_at, @completed_at
                )
                RETURNING id;";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "id", taskId);
            AddParameter(command, "project_id", task.ProjectId);
            AddParameter(command, "author_id", task.AuthorId);
            AddParameter(command, "assignee_id", task.AssigneeId);
            AddParameter(command, "status_id", task.StatusId);
            AddParameter(command, "title", task.Title);
            AddParameter(command, "notes", task.Notes);
            AddParameter(command, "priority", task.Priority);
            AddParameter(command, "start_date", task.StartDate);
            AddParameter(command, "due_date", task.DueDate);
            AddParameter(command, "estimated_budget", task.EstimatedBudget);
            AddParameter(command, "estimated_unit", task.EstimatedUnit);
            AddParameter(command, "created_at", now);
            AddParameter(command, "updated_at", now);
            AddParameter(command, "completed_at", task.CompletedAt);

            var result = await command.ExecuteScalarAsync();

            return result is Guid createdTaskId
                ? createdTaskId
                : throw new GraphQLException("Не вдалося створити таску.");
        }

        public async Task<TaskItem?> GetTaskByIdAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            var sql = $@"
                SELECT {TaskColumns}
                FROM app.tasks
                WHERE id = @task_id;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("task_id", taskId);

            await using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync()
                ? MapTask(reader)
                : null;
        }

        public async Task<IEnumerable<TaskItem>> GetProjectTasksAsync(Guid projectId)
        {
            var tasks = new List<TaskItem>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            var sql = $@"
                SELECT {TaskColumns}
                FROM app.tasks
                WHERE project_id = @project_id
                ORDER BY created_at DESC;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(MapTask(reader));
            }

            return tasks;
        }

        public async Task<IEnumerable<TaskAssignment>> GetTaskAssignmentsAsync(Guid taskId)
        {
            var assignments = new List<TaskAssignment>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, task_id, user_id, estimated_hours, assigned_by, assigned_at
                FROM app.task_assignments
                WHERE task_id = @task_id
                ORDER BY assigned_at;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("task_id", taskId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                assignments.Add(new TaskAssignment
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    TaskId = reader.GetGuid(reader.GetOrdinal("task_id")),
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                    EstimatedHours = reader.GetDecimal(
                        reader.GetOrdinal("estimated_hours")
                    ),
                    AssignedBy = reader.GetGuid(reader.GetOrdinal("assigned_by")),
                    AssignedAt = reader.GetFieldValue<DateTimeOffset>(
                        reader.GetOrdinal("assigned_at")
                    )
                });
            }

            return assignments;
        }

        public async Task<bool> AddTaskAssignmentAsync(TaskAssignment assignment)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO app.task_assignments
                    (id, task_id, user_id, estimated_hours, assigned_by, assigned_at)
                VALUES
                    (@id, @task_id, @user_id, @estimated_hours, @assigned_by, @assigned_at)
                ON CONFLICT (task_id, user_id) DO NOTHING;";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(
                command,
                "id",
                assignment.Id == Guid.Empty ? Guid.NewGuid() : assignment.Id
            );
            AddParameter(command, "task_id", assignment.TaskId);
            AddParameter(command, "user_id", assignment.UserId);
            AddParameter(command, "estimated_hours", assignment.EstimatedHours);
            AddParameter(command, "assigned_by", assignment.AssignedBy);
            AddParameter(command, "assigned_at", assignment.AssignedAt);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RemoveTaskAssignmentAsync(
            Guid taskId,
            Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                DELETE FROM app.task_assignments
                WHERE task_id = @task_id
                  AND user_id = @user_id;";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("task_id", taskId);
            command.Parameters.AddWithValue("user_id", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParameter(
            NpgsqlCommand command,
            string name,
            object? value)
        {
            command.Parameters.AddWithValue(
                name,
                value ?? DBNull.Value
            );
        }
        public async Task<bool> IsProjectStatusAsync( Guid projectId, Guid statusId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM app.project_statuses
                    WHERE project_id = @project_id
                      AND id = @status_id
                );";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("status_id", statusId);

            return (bool)(await command.ExecuteScalarAsync() ?? false);
        }
        private static TaskItem MapTask(NpgsqlDataReader reader)
        {
            var assigneeIdOrdinal = reader.GetOrdinal("assignee_id");
            var notesOrdinal = reader.GetOrdinal("notes");
            var startDateOrdinal = reader.GetOrdinal("start_date");
            var dueDateOrdinal = reader.GetOrdinal("due_date");
            var estimatedBudgetOrdinal = reader.GetOrdinal("estimated_budget");
            var estimatedUnitOrdinal = reader.GetOrdinal("estimated_unit");
            var completedAtOrdinal = reader.GetOrdinal("completed_at");

            return new TaskItem
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                ProjectId = reader.GetGuid(reader.GetOrdinal("project_id")),
                AuthorId = reader.GetGuid(reader.GetOrdinal("author_id")),

                AssigneeId = reader.IsDBNull(assigneeIdOrdinal)
                    ? null
                    : reader.GetGuid(assigneeIdOrdinal),

                StatusId = reader.GetGuid(reader.GetOrdinal("status_id")),
                Title = reader.GetString(reader.GetOrdinal("title")),

                Notes = reader.IsDBNull(notesOrdinal)
                    ? null
                    : reader.GetString(notesOrdinal),

                Priority = reader.GetString(reader.GetOrdinal("priority")),

                StartDate = reader.IsDBNull(startDateOrdinal)
                    ? null
                    : reader.GetDateTime(startDateOrdinal),

                DueDate = reader.IsDBNull(dueDateOrdinal)
                    ? null
                    : reader.GetDateTime(dueDateOrdinal),

                EstimatedBudget = reader.IsDBNull(estimatedBudgetOrdinal)
                    ? null
                    : reader.GetDecimal(estimatedBudgetOrdinal),

                EstimatedUnit = reader.IsDBNull(estimatedUnitOrdinal)
                    ? null
                    : reader.GetString(estimatedUnitOrdinal),

                CreatedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("created_at")
                ),

                UpdatedAt = reader.GetFieldValue<DateTimeOffset>(
                    reader.GetOrdinal("updated_at")
                ),

                CompletedAt = reader.IsDBNull(completedAtOrdinal)
                    ? null
                    : reader.GetFieldValue<DateTimeOffset>(
                        completedAtOrdinal
                    )
            };
        }
    }
}