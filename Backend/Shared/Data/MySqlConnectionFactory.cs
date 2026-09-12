using MySqlConnector;

namespace SkillBridge.Web.Infrastructure.Data;

public sealed class DatabaseUnavailableException(Exception innerException) : Exception("The database is unavailable. Please try again shortly.", innerException);

public sealed class MySqlConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SkillBridge");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:SkillBridge with .NET user secrets or the ConnectionStrings__SkillBridge environment variable. See database/README.md.");

        var settings = new MySqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(settings.Database) || string.IsNullOrWhiteSpace(settings.Server) || string.IsNullOrWhiteSpace(settings.UserID))
            throw new InvalidOperationException("The SkillBridge connection requires Server, Database and User ID settings.");

        _connectionString = settings.ConnectionString;
    }

    public async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new MySqlCommand("SET time_zone = '+00:00'", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch (MySqlException exception)
        {
            await connection.DisposeAsync();
            throw new DatabaseUnavailableException(exception);
        }
        catch (OperationCanceledException)
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
