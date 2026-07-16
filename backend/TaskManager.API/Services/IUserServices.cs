namespace TaskManager.API.Services
{
    public interface IUserServices
    {
         Task InviteAdminAsync(string email, string name);
    }
}
