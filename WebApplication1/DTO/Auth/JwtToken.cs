namespace WebApplication1.DTO.Auth
{
    public class JwtToken
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
