namespace CraftServer.Models
{
    public class ResetPasswordModel
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }
}