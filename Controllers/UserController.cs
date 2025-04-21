using CraftServer.Context;
using CraftServer.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CraftServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        public readonly IConfiguration _config;
        public readonly DbContext _context;

        public UserController(IConfiguration config, DbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpGet("getUsers")]
        [Authorize]
        public async Task<IEnumerable<User>> GetUserDetails()
        {
            var query = "SELECT * FROM Users";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<User>(query);
        }
    }
}
