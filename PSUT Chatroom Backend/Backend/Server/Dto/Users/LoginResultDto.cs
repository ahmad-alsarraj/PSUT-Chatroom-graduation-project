namespace Server.Dto.Users
{
    public class LoginResultDto
    {
        public UserDto User { get; set; }
        public string Token { get; set; }
    }
}