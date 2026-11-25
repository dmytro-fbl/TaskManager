using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using TaskManager.API.Extensions;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Services.TaskServices;

namespace TaskManager.API.GraphQL.Queries
{
    [ExtendObjectType("Query")]
    public class TaskCommentQueries
    {
        [Authorize]
        public async Task<IEnumerable<TaskComment>> GetTaskCommentsAsync(
            Guid taskId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = claimsPrincipal.GetUserId();
            return await commentService.GetCommentsByTaskIdAsync(taskId, userId);
        }

        [Authorize]
        public async Task<IEnumerable<TaskCommentVersion>> GetTaskCommentVersionsAsync(
            Guid commentId,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = claimsPrincipal.GetUserId();
            return await commentService.GetVersionsAsync(commentId, userId);
        }
    }
}