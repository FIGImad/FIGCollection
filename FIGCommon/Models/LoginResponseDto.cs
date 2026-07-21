namespace FIGCommon.Models
{
    public class LoginResponseDto
    {
        public string? AccessToken { get; set; } = null;
        public string? TokenType { get; set; } = null;
        public int? ExpiresIn { get; set; } = null;
        public string? UserName { get; set; } = null;
        //public DateTime? issued { get; set; } = null;
        //public DateTime? expires { get; set; } = null;

    }
}
