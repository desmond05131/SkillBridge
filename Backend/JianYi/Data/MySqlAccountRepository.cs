using MySqlConnector;
using SkillBridge.Web.Features.Accounts;

namespace SkillBridge.Web.Infrastructure.Data;

public sealed class MySqlAccountRepository(MySqlConnectionFactory connections) : IAccountRepository
{
    private const string UserColumns = "u.id, u.display_name, u.email, r.name AS role_name, u.is_active, u.security_stamp, u.password_hash";

    public async Task<AccountLookup> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand($"SELECT {UserColumns} FROM users u JOIN roles r ON r.id = u.role_id WHERE u.normalized_email = @email LIMIT 1", connection);
        command.Parameters.AddWithValue("@email", normalizedEmail);
        return await ReadAccountAsync(command, cancellationToken);
    }

    public async Task<AccountLookup> FindByIdAsync(long userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand($"SELECT {UserColumns} FROM users u JOIN roles r ON r.id = u.role_id WHERE u.id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", userId);
        return await ReadAccountAsync(command, cancellationToken);
    }

    public async Task<AccountUser> CreateAsync(string displayName, string email, string normalizedEmail, string passwordHash, UserRole role, string securityStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            INSERT INTO users (display_name, email, normalized_email, password_hash, role_id, is_active, security_stamp)
            VALUES (@name, @email, @normalizedEmail, @hash, (SELECT id FROM roles WHERE name = @role), TRUE, @stamp)
            """, connection);
        command.Parameters.AddWithValue("@name", displayName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@normalizedEmail", normalizedEmail);
        command.Parameters.AddWithValue("@hash", passwordHash);
        command.Parameters.AddWithValue("@role", role.ToString());
        command.Parameters.AddWithValue("@stamp", securityStamp);
        await ExecuteUniqueAsync(command, cancellationToken);
        return new AccountUser(command.LastInsertedId, displayName, email, role, true, securityStamp);
    }

    public async Task<AccountUser> UpdateProfileAsync(AccountUser user, string displayName, string email, string normalizedEmail, string securityStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            UPDATE users SET display_name = @name, email = @email, normalized_email = @normalizedEmail, security_stamp = @stamp
            WHERE id = @id AND is_active = TRUE AND security_stamp = @oldStamp
            """, connection);
        command.Parameters.AddWithValue("@name", displayName);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@normalizedEmail", normalizedEmail);
        AddIdentityParameters(command, user, securityStamp);
        var affected = await ExecuteUniqueAsync(command, cancellationToken);
        if (affected != 1)
            throw new AccountChangedException();

        return user with { DisplayName = displayName, Email = email, SecurityStamp = securityStamp };
    }

    public async Task<AccountUser> UpdatePasswordAsync(AccountUser user, string passwordHash, string securityStamp, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand("""
            UPDATE users SET password_hash = @hash, security_stamp = @stamp
            WHERE id = @id AND is_active = TRUE AND security_stamp = @oldStamp
            """, connection);
        command.Parameters.AddWithValue("@hash", passwordHash);
        AddIdentityParameters(command, user, securityStamp);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new AccountChangedException();

        return user with { SecurityStamp = securityStamp };
    }

    private static void AddIdentityParameters(MySqlCommand command, AccountUser user, string securityStamp)
    {
        command.Parameters.AddWithValue("@id", user.Id);
        command.Parameters.AddWithValue("@stamp", securityStamp);
        command.Parameters.AddWithValue("@oldStamp", user.SecurityStamp);
    }

    private static async Task<int> ExecuteUniqueAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new DuplicateEmailException(exception);
        }
    }

    private static async Task<AccountLookup> ReadAccountAsync(MySqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return new AccountLookup.Missing();

        var user = new AccountUser(reader.GetInt64("id"), reader.GetString("display_name"), reader.GetString("email"),
            Enum.Parse<UserRole>(reader.GetString("role_name")), reader.GetBoolean("is_active"), reader.GetString("security_stamp"));
        return new AccountLookup.Found(new AccountCredentials(user, reader.GetString("password_hash")));
    }
}
