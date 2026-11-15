using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MenuMint.Data;

public static class DbInitializer
{
    private static readonly string[] BusinessTables = { "Restaurants", "Categories", "MenuItems" };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var config = services.GetRequiredService<IConfiguration>();
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var factory = services.GetRequiredService<IDbConnectionFactory>();
        var connectionString = config.GetConnectionString("DefaultConnection")!;

        // 1. Identity tables (EF Core also creates the database itself if it does not exist yet)
        var identityDb = services.GetRequiredService<ApplicationDbContext>();
        await identityDb.Database.MigrateAsync();
        logger.LogInformation("Identity tables are up to date.");

        // 2. Business tables: plain ADO.NET (SqlConnection, SqlCommand, SqlDataReader)
        var existing = await GetExistingTablesAsync(connectionString);
        var missing = BusinessTables.Except(existing).ToList();

        if (missing.Count > 0)
        {
            var scriptPath = Path.Combine(env.ContentRootPath, "Database", "schema.sql");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("schema.sql was not found. It must be in the Database folder.", scriptPath);

            var script = await File.ReadAllTextAsync(scriptPath);

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(script, conn);
            await cmd.ExecuteNonQueryAsync();

            logger.LogInformation("Created business tables: {Tables}", string.Join(", ", missing));
        }

        // 3. Sanity check through the connection factory + Dapper
        using var check = factory.CreateConnection();
        var count = await check.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.tables WHERE name IN @names",
            new { names = BusinessTables });

        if (count != BusinessTables.Length)
            throw new InvalidOperationException($"Expected {BusinessTables.Length} business tables but found {count}.");

        logger.LogInformation("Database ready ({Count} business tables).", count);
    }

    // Raw ADO.NET on purpose: the proposal names SqlCommand and SqlDataReader.
    private static async Task<List<string>> GetExistingTablesAsync(string connectionString)
    {
        var found = new List<string>();

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT name FROM sys.tables WHERE name IN ('Restaurants', 'Categories', 'MenuItems');";
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            found.Add(reader.GetString(0));

        return found;
    }
}