using CraftServer.Context;
using CraftServer.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CraftServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetPasswordController : ControllerBase
    {
        private DbContext _context;
        public ResetPasswordController(DbContext context)
        {
            _context = context;
        }
        [HttpPost("requestOtp")]
        public async Task<IActionResult> RequestOtp([FromBody] string email)
        {
            try
            {
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

                //await SendOtpEmail()

                return Ok(new { message = "OTP sent successfully", otp = otp });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "An error occurred while requesting OTP.", error = ex.Message });
            }
        }

        public async Task SendOtpEmail(string email, string otp)
        {

        }
    }
}
