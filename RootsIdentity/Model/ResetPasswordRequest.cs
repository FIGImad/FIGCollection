namespace RootsIdentity.Models
{
    public class ResetPasswordRequest
    {
        public string UserId { get; set; } = "";
        public long TokenId { get; set; } = -1;
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
