using Microsoft.Data.SqlClient;
using System.Data;

namespace CraftServer.Context
{
    public class DbContext
    {
        private readonly string _connectionString;
        public DbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CraftConn");
        }
        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
