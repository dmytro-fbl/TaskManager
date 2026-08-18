using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using TaskManager.API.DTOs.Tasks.Comments;
using TaskManager.API.Models.TasksTables.Comments;
using TaskManager.API.Services.TaskServices;

namespace TaskManager.API.GraphQL.Mutations
{
    [ExtendObjectType("Mutation")]
    public class TaskCommentMutations
    {
        private Guid GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
        {
            var userIdString = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userId)) return userId;
            throw new GraphQLException("Недійсно авторизований користувач.");
        }

        [Authorize]
        public async Task<TaskComment> CreateTaskCommentAsync(
            CreateTaskCommentInput input,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            return await commentService.CreateCommentAsync(input, userId);
        }

        [Authorize]
        public async Task<TaskComment> UpdateTaskCommentAsync(
            UpdateTaskCommentInput input,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            return await commentService.UpdateCommentAsync(input, userId);
        }

        [Authorize]
        public async Task<bool> DeleteTaskCommentAsync(
            DeleteTaskCommentInput input,
            ClaimsPrincipal claimsPrincipal,
            [Service] ITaskCommentService commentService)
        {
            var userId = GetCurrentUserId(claimsPrincipal);
            await commentService.DeleteCommentAsync(input, userId);
            return true; 
        }
    }
}