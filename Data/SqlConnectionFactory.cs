using System.Data;
using Microsoft.Data.SqlClient;

namespace MenuMint.Data;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing from appsettings.json.");
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}