using Dapper;
using Npgsql;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Repositories.CommentsRepository;

namespace TaskManager.API.Repositories.TaskCommentsRepository
{
    public class TaskCommentsRepository : ITaskCommentsRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public TaskCommentsRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<Guid> CreateAsync(TaskComment comment)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                INSERT INTO app.task_comments (id, task_id, author_id, body, created_at, updated_at, is_edited, parent_comment_id)
                VALUES (@Id, @TaskId, @AuthorId, @Body, @CreatedAt, @UpdatedAt, @IsEdited, @ParentCommentId)
                RETURNING id;";

            return await connection.ExecuteScalarAsync<Guid>(sql, new
            {
                Id = Guid.NewGuid(),
                comment.TaskId,
                comment.AuthorId,
                comment.Body,
                comment.CreatedAt,
                comment.UpdatedAt,
                comment.IsEdited,
                comment.ParentCommentId
            });
        }

        public async Task DeleteAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                UPDATE app.task_comments
                SET is_deleted = true,
                    updated_at = NOW()
                WHERE id = @id;";

            await connection.ExecuteAsync(sql, new { id = commentId });
        }

        public async Task<TaskComment?> GetByIdAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, task_id, author_id, body, created_at, updated_at, is_edited, is_deleted, parent_comment_id
                FROM app.task_comments
                WHERE id = @id;";

            return await connection.QuerySingleOrDefaultAsync<TaskComment>(sql, new { id = commentId });
        }

        public async Task<IEnumerable<TaskComment>> GetByTaskIdAsync(Guid taskId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, task_id, author_id, body, created_at, updated_at, is_edited, parent_comment_id, is_deleted
                FROM app.task_comments
                WHERE task_id = @taskId
                ORDER BY created_at ASC;";

            return await connection.QueryAsync<TaskComment>(sql, new { taskId });
        }

        public async Task<IEnumerable<TaskCommentVersion>> GetVersionsAsync(Guid commentId)
        {
            await using var connection = await _dataSource.OpenConnectionAsync();

            const string sql = @"
                SELECT id, comment_id, previous_body, changed_at
                FROM app.task_comment_versions
                WHERE comment_id = @commentId
                ORDER BY changed_at ASC;";

            return await connection.QueryAsync<TaskCommentVersion>(sql, new { commentId });
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
                    WHERE id = @id;";

                await connection.ExecuteAsync(insertVersionSql, new { id = commentId }, transaction);

                const string updateCommentSql = @"
                    UPDATE app.task_comments
                    SET body = @newBody,
                        is_edited = true,
                        updated_at = NOW()
                    WHERE id = @id;";

                await connection.ExecuteAsync(updateCommentSql, new { id = commentId, newBody }, transaction);

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
