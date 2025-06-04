using CraftServer.Context;
using CraftServer.Data;
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
    public class SignUpController : ControllerBase
    {
        public readonly IConfiguration _config;
        public readonly DbContext _context;

        public SignUpController(IConfiguration config, DbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("getToken")]
        public async Task<IActionResult> SignUp(UserRegister userRegister)
        {
            try
            {
                bool userExists = await CheckUserExists(userRegister);
                if (!userExists)
                {
                    User user = await InsertUser(userRegister);
                    var token = GenerateToken(user);
                    return Ok(new { token = token });
                }
                return StatusCode(500, new { messae = "A user with this email already exists" });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = "An error occurred while creating the user.", error = e.Message });
            }
        }

        public async Task<User> InsertUser(UserRegister userRegister)
        {
            Guid userID = Guid.NewGuid();
            string userName = userRegister.UserName;
            string password = userRegister.Password;
            string firstName = userRegister.FirstName;
            string lastName = userRegister.LastName;
            string userEmail = userRegister.UserEmail;

            var parameters = new { UserID = userID, UserName = userName, Password = password, FirstName = firstName, LastName = lastName, UserEmail = userEmail };

            using var connection = _context.CreateConnection();
            return await connection.QuerySingleAsync<User>(Queries.CreateUser, parameters);

        }

        public async Task<bool> CheckUserExists(UserRegister userRegister)
        {
            string email = userRegister.UserEmail;

            var query = "SELECT 1 FROM Users where UserEmail = @Email";
            var parameters = new { Email = email };

            using var connection = _context.CreateConnection();

            var result = await connection.QueryFirstOrDefaultAsync<int?>(query, parameters);
            return result.HasValue;
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
