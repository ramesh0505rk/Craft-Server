using CraftServer.Context;
using CraftServer.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CraftServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignInController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly DbContext _context;

        public SignInController(IConfiguration config, DbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("getToken")]
        public async Task<IActionResult> SignIn(UserCreds userCreds)
        {
            try
            {
                var user = await Authenticate(userCreds);
                var token = GenerateToken(user);
                return Ok(new { token = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Invalid username or password " +ex });
            }
        }

        public async Task<User> Authenticate(UserCreds userCreds)
        {
            string userName = userCreds.UserName;
            string password = userCreds.Password;

            var query = "SELECT * FROM Users where UserName = @UserName AND Password = @Password";
            using var connection = _context.CreateConnection();

            var parameters = new { UserName = userName, Password = password };

            try
            {
                return await connection.QueryFirstAsync<User>(query, parameters);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string GenerateToken(User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("UserID",user.UserID+""),
                new Claim("UserName",user.UserName),
                new Claim("FirstName",user.FirstName),
                new Claim("LastName",user.LastName),
                new Claim("UserEmail",user.UserEmail)
            };

            var token = new JwtSecurityToken(jwtSettings["Issuer"],
                jwtSettings["Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(int.Parse(jwtSettings["ExpireInMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
