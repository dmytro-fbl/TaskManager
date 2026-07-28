namespace TaskManager.API.DTOs
{
    public class AuthPayload
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken {  get; set; } = string.Empty;

    }
}
