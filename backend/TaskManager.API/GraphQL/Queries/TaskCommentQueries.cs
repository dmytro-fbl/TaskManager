using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Services.TaskServices;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType("Query")]
    public class TaskCommentQueries
    {
        private Guid GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userId)) return userId;
            throw new GraphQLException("Недійсно авторизований користувач.");
        }

        [Authorize]
        public async Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(
            Guid taskId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            return await commentService.GetCommentsByTaskIdAsync(taskId, userId);
        }

        [Authorize]
        public async Task<IEnumerable<TaskCommentVersion>> GetTaskCommentVersionsAsync(
            Guid commentId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            return await commentService.GetVersionsAsync(commentId, userId);
        }
    }
}