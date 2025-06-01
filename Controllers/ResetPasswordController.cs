using CraftServer.Context;
using CraftServer.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CraftServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetPasswordController : ControllerBase
    {
        private DbContext _context;
        private IConfiguration _config;
        public ResetPasswordController(DbContext context, IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }
        [HttpPost("requestOtp")]
        public async Task<IActionResult> RequestOtp(UserEmail userEmail)
        {
            try
            {
                string email = userEmail.Email;
                var query = "SELECT * FROM Users WHERE UserEmail = @UserEmail";
                using var connection = _context.CreateConnection();
                var user = await connection.QueryFirstOrDefaultAsync<User>(query, new { UserEmail = email });

                if (user == null)
                {
                    return BadRequest(new { message = "A user with this email doesn't exists" });
                }

                var otp = new Random().Next(1000, 9999).ToString("D4");
                var otpExpiry = DateTime.UtcNow.AddMinutes(10);

                var resetInsertQuery = @"INSERT INTO PasswordResetOtps (UserID, Otp, ExpiryDate, IsUsed) VALUES
                                       (@UserID, @Otp, @ExpiryDate, @IsUsed)";

                await connection.ExecuteAsync(resetInsertQuery, new { UserID = user.UserID, Otp = otp, ExpiryDate = otpExpiry, IsUsed = 0 });

                await SendOtpEmail(email, otp);

                return Ok(new { message = "OTP sent successfully", otp = otp });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "An error occurred while requesting OTP.", error = ex.Message });
            }
        }

        public async Task SendOtpEmail(string email, string otp)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var message = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"], "Craft App"),
                Subject = "Your OTP for password reset",
                Body = @$"Your OTP for password reset is: <strong>{otp}</strong>
                       <br/>This OTP is valid for 10 minutes",
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var smtpClient = new SmtpClient(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]))
            {
                Credentials = new System.Net.NetworkCredential(emailSettings["SenderEmail"], emailSettings["SenderAppPassword"]),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message);
        }
    }
    public class UserEmail
    {
        public string Email { get; set; } = "";
    }
}
