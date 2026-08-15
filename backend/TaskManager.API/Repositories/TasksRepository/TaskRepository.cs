using Npgsql;
using TaskManager.API.DTOs.Tasks;
using TaskManager.API.Models.TasksTables;

namespace TaskManager.API.Repositories.TasksRepository
{
    public class TaskRepository : ITaskRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        private const string TaskColumns = @"
            id, project_id, author_id, assignee_id, status_id,
            title, notes, priority, start_date, due_date,
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
                    created_at, updated_at, completed_at
                )
                VALUES (
                    @id, @project_id, @author_id, @assignee_id, @status_id,
                    @title, @notes, @priority, @start_date, @due_date,
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
            await using var transaction = await connection.BeginTransactionAsync();

            const string insertSql = @"
            INSERT INTO app.task_assignments
                (id, task_id, user_id, estimated_hours, assigned_by, assigned_at)
            VALUES
                (@id, @task_id, @user_id, @estimated_hours, @assigned_by, @assigned_at)
            ON CONFLICT (task_id, user_id) DO NOTHING;";

            await using var insertCommand = new NpgsqlCommand(
                insertSql,
                connection,
                transaction
            );

            AddParameter(
                insertCommand,
                "id",
                assignment.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : assignment.Id
            );

            AddParameter(insertCommand, "task_id", assignment.TaskId);
            AddParameter(insertCommand, "user_id", assignment.UserId);
            AddParameter(
                insertCommand,
                "estimated_hours",
                assignment.EstimatedHours
            );
            AddParameter(
                insertCommand,
                "assigned_by",
                assignment.AssignedBy
            );
            AddParameter(
                insertCommand,
                "assigned_at",
                assignment.AssignedAt
            );

            var inserted = await insertCommand.ExecuteNonQueryAsync() > 0;

            if (!inserted)
            {
                await transaction.RollbackAsync();
                return false;
            }

            const string updateTaskSql = @"
                UPDATE app.tasks
                SET assignee_id = @user_id,
                    updated_at = @updated_at
                WHERE id = @task_id;";

            await using var updateTaskCommand = new NpgsqlCommand(
                updateTaskSql,
                connection,
                transaction
            );

            AddParameter(updateTaskCommand, "task_id", assignment.TaskId);
            AddParameter(updateTaskCommand, "user_id", assignment.UserId);
            AddParameter(
                updateTaskCommand,
                "updated_at",
                DateTimeOffset.UtcNow
            );

            await updateTaskCommand.ExecuteNonQueryAsync();
            await transaction.CommitAsync();

            return true;
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
        public async Task<bool> UpdateTaskStatusAsync(Guid taskId,Guid statusId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.tasks
                SET status_id = @status_id,
                    updated_at = @updated_at
                WHERE id = @task_id;";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "task_id", taskId);
            AddParameter(command, "status_id", statusId);
            AddParameter(command, "updated_at", DateTimeOffset.UtcNow);

            return await command.ExecuteNonQueryAsync() > 0;
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

        public async Task<bool> HasUserTasksInProjectAsync(Guid projectId, Guid userId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT exists(
                    SELECT 1
                    FROM app.tasks
                    WHERE project_id = @project_id
                            AND (assignee_id = @user_id OR author_id = @user_id)
                );
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("project_id", projectId);
            command.Parameters.AddWithValue("user_id", userId);
            var result = await command.ExecuteScalarAsync();
            return result is bool hasTasks && hasTasks;
        }

        public async Task<bool> UpdateTaskAsync(UpdateTaskInput input)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.tasks
                SET title = @title,
                    notes = @notes,
                    priority = @priority,
                    start_date = @start_date,
                    due_date = @due_date,
                    updated_at = @updated_at
                WHERE id = @task_id;
            ";
            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "task_id", input.TaskId);
            AddParameter(command, "title", input.Title.Trim());
            AddParameter(command, "notes", string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim());
            AddParameter(command, "priority", input.Priority.Trim().ToLowerInvariant());
            AddParameter(command, "start_date", input.StartDate);
            AddParameter(command, "due_date", input.DueDate);
            AddParameter(command, "updated_at", DateTimeOffset.UtcNow);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> AddWorkLogAsync(Guid userId, WorkLogInput input)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            Guid workLogId = Guid.NewGuid();

            const string sql = @"
                INSERT INTO app.worklogs (task_id, user_id, hours_spent, log_date, description, created_at)
                VALUES (@task_id, @user_id, @hours_spent, @log_date, @description, @created_at);
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "task_id", input.TaskId);
            AddParameter(command, "user_id", userId);
            AddParameter(command, "hours_spent", input.HoursSpent);
            AddParameter(command, "log_date", DateTimeOffset.UtcNow);
            AddParameter(command, "description", string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim());
            AddParameter(command, "created_at", DateTimeOffset.UtcNow);

            var result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        public async Task<IEnumerable<WorkLogDTO>> GetTaskWorkLogsAsync(Guid taskId)
        {
            var workLogs = new List<WorkLogDTO>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
            SELECT
    w.id AS work_id,
    w.task_id,
    u.id AS user_id,
    u.name AS user_name,
    prl.name AS role_name,
    w.hours_spent,
    w.log_date,
    w.description
    FROM app.worklogs w
    JOIN app.users u ON w.user_id = u.id
    LEFT JOIN app.project_role_labels prl ON w.role_label_id = prl.id 
    WHERE w.task_id = @task_id
    ORDER BY w.log_date DESC;                
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "task_id", taskId);

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                workLogs.Add(new WorkLogDTO
                {
                    Id = reader.GetGuid(reader.GetOrdinal("work_id")),
                    TaskId = reader.GetGuid(reader.GetOrdinal("task_id")),
                    UserId = reader.GetGuid(reader.GetOrdinal("user_id")),
                    UserName = reader.GetString(reader.GetOrdinal("user_name")),
                    
                    RoleName = reader.IsDBNull(reader.GetOrdinal("role_name"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("role_name")),

                    HoursSpent = reader.GetDecimal(reader.GetOrdinal("hours_spent")),
                    LogDate = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("log_date")),
                    Comment = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null :
                    reader.GetString(reader.GetOrdinal("description"))
                });
            }

            return workLogs;
        }
    }
}