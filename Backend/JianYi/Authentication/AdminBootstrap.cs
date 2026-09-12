using System.ComponentModel.DataAnnotations;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Authentication;

public static class AdminBootstrap
{
    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var displayName = ReadValue("SKILLBRIDGE_ADMIN_NAME", "Administrator display name: ").Trim();
        var email = ReadValue("SKILLBRIDGE_ADMIN_EMAIL", "Administrator email: ").Trim();
        var password = ReadPassword();
        if (displayName.Length is < 2 or > 80 || email.Length > 254 || !new EmailAddressAttribute().IsValid(email) || password.Length is < AccountService.MinimumPasswordLength or > AccountService.MaximumPasswordLength)
        {
            Console.Error.WriteLine("No account was created. Supply a name of 2–80 characters, a valid email of up to 254 characters and a password of 12–128 characters.");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        try
        {
            await accounts.CreateAdminAsync(displayName, email, password, cancellationToken);
            Console.WriteLine("Administrator created. Sign in through /Account/Login. No existing account was changed.");
            return 0;
        }
        catch (DuplicateEmailException)
        {
            Console.Error.WriteLine("No account was created. The supplied email is already registered; this command never promotes existing accounts.");
            return 1;
        }
        catch (DatabaseUnavailableException)
        {
            Console.Error.WriteLine("No account was created. The database is unavailable; check the local setup instructions.");
            return 1;
        }
    }

    private static string ReadValue(string environmentName, string prompt)
    {
        var value = Environment.GetEnvironmentVariable(environmentName);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (Console.IsInputRedirected)
            return string.Empty;

        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }

    private static string ReadPassword()
    {
        var password = Environment.GetEnvironmentVariable("SKILLBRIDGE_ADMIN_PASSWORD");
        if (!string.IsNullOrEmpty(password))
            return password;

        if (Console.IsInputRedirected)
            return string.Empty;

        Console.Write("Administrator password (12–128 characters, hidden): ");
        var characters = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(characters.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count == 0)
                    continue;

                characters.RemoveAt(characters.Count - 1);
                continue;
            }

            if (char.IsControl(key.KeyChar) || characters.Count >= AccountService.MaximumPasswordLength + 1)
                continue;

            characters.Add(key.KeyChar);
        }
    }
}
