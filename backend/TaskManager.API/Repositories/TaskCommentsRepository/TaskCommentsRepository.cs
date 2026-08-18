using Npgsql;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Repositories.CommentsRepository;
using static HotChocolate.Types.SpecScalarNames;

namespace TaskManager.API.Repositories.TaskCommentsRepository
{
    public class TaskCommentsRepository : ITaskCommentsRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public TaskCommentsRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
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

        public async Task<Guid> CreateAsync(TaskComment comment)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO  app.task_comments (id, task_id, author_id, body, created_at, updated_at, is_edited, parent_comment_id)
                VALUES (@id, @task_id, @author_id, @body, @created_at, @updated_at, @is_edited, @parent_comment_id)
                RETURNING id;";

            using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "id", Guid.NewGuid());
            AddParameter(command, "task_id", comment.TaskId);
            AddParameter(command, "author_id", comment.AuthorId);
            AddParameter(command, "body", comment.Body);
            AddParameter(command, "created_at", comment.CreatedAt);
            AddParameter(command, "updated_at", comment.UpdatedAt);
            AddParameter(command, "is_edited", comment.IsEdited);
            AddParameter(command, "parent_comment_id", comment.ParentCommentId);

            var result = await command.ExecuteScalarAsync();

            return result is Guid createdTaskCommentId
                ? createdTaskCommentId
                : throw new GraphQLException("Не вдалось створити коментар.");

        }

        public async Task DeleteAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.task_comments 
                SET is_deleted = true, 
                updated_at = NOW()
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            AddParameter(command, "id", commentId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<TaskComment?> GetByIdAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT *
                FROM app.task_comments
                WHERE id = @id;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "id", commentId);
            var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new TaskComment
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    TaskId = reader.GetGuid(reader.GetOrdinal("task_id")),
                    AuthorId = reader.GetGuid(reader.GetOrdinal("author_id")),
                    Body = reader.GetString(reader.GetOrdinal("body")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("is_edited")),
                    ParentCommentId = reader.IsDBNull(reader.GetOrdinal("parent_comment_id"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("parent_comment_id"))
                };
            }
            return null;

        }

        public async Task<IEnumerable<TaskComment>> GetByTaskIdAsync(Guid taskId)
        {
            List<TaskComment> comments = new List<TaskComment>();

            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, task_id, author_id, body, created_at, updated_at, is_edited, parent_comment_id, is_deleted
                FROM app.task_comments
                WHERE task_id = @task_id
                ORDER BY created_at ASC;
            ";

            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "task_id", taskId);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                comments.Add(new TaskComment
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    TaskId = reader.GetGuid(reader.GetOrdinal("task_id")),
                    AuthorId = reader.GetGuid(reader.GetOrdinal("author_id")),
                    Body = reader.GetString(reader.GetOrdinal("body")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                        ? null
                        : reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("updated_at")),
                    IsEdited = reader.GetBoolean(reader.GetOrdinal("is_edited")),
                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                    ParentCommentId = reader.IsDBNull(reader.GetOrdinal("parent_comment_id"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("parent_comment_id"))
                });
            }
            return comments;
        }

        public async Task<IEnumerable<TaskCommentVersion>> GetVersionsAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();
            List<TaskCommentVersion> commentVersions = new List<TaskCommentVersion>(); 

            const string sql = @"
                SELECT id, comment_id, previous_body, changed_at
                FROM app.task_comment_versions
                WHERE comment_id = @comment_id
                ORDER BY changed_at ASC;
            ";
            await using var command = new NpgsqlCommand(sql, connection);

            AddParameter(command, "comment_id", commentId);

            var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                commentVersions.Add(new TaskCommentVersion
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    CommentId = reader.GetGuid(reader.GetOrdinal("comment_id")),
                    PreviousBody = reader.GetString(reader.GetOrdinal("previous_body")),  
                    ChangedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("changed_at"))
                });
            }
            return commentVersions;
        }

        public async Task UpdateAsync(Guid commentId, string newBody)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string insertVersionSql = @"
                    INSERT INTO app.task_comment_versions (comment_id, previous_body)
                    SELECT id, body
                    FROM app.task_comments
                    WHERE id = @id;
                ";

                await using var insertCmd = new NpgsqlCommand(insertVersionSql, connection);
                AddParameter(insertCmd, "id", commentId);
                await insertCmd.ExecuteNonQueryAsync();

                const string updateCommentSql = @"
                    UPDATE app.task_comments 
                    SET body = @new_body, 
                        is_edited = true, 
                        updated_at = NOW()
                    WHERE id = @id;
                ";
                await using var updateCmd = new NpgsqlCommand(updateCommentSql, connection);
                AddParameter(updateCmd, "id", commentId);
                AddParameter(updateCmd, "new_body", newBody);
                await updateCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
