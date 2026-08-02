using HotChocolate.Types;
using TaskManager.API.Models;
using TaskManager.API.Models.ProjectsTables;
using TaskManager.API.Repositories;

namespace TaskManager.API.GraphQL.Extensions
{
    [ExtendObjectType(typeof(ProjectMembership))]
    public class ProjectMembershipExtensions
    {
        public async Task<User> GetUser(
            [Parent] ProjectMembership membership,
            [Service] IUserRepository userRepository)
        {
            var user = await userRepository.GetUserByIdAsync(membership.UserId);
            if (user == null)
            {
                throw new GraphQLException("Користувача для цього membership не знайдено.");
            }

            return user;
        }
    }
}