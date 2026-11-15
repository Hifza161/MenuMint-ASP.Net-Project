using System.Data;

namespace MenuMint.Data;

public interface IDbConnectionFactory
{
    /// <summary>Returns a new, closed connection. The caller disposes it (using var).</summary>
    IDbConnection CreateConnection();
}