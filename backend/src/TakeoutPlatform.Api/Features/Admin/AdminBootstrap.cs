using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TakeoutPlatform.Api.Data;
using TakeoutPlatform.Api.Features.Auth;

namespace TakeoutPlatform.Api.Features.Admin;

public sealed class AdminBootstrapOptions
{
    public const string SectionName = "Admin";
    public const int MinimumPasswordLength = 12;

    /// <summary>
    /// Password for the built-in <c>admin</c> account. Supplied from a secret in the environment
    /// (<c>Admin__InitialPassword</c>); it is never committed.
    /// </summary>
    public string? InitialPassword { get; set; }
}

/// <summary>
/// The database ships with an <c>admin</c> account that cannot sign in (its password hash is
/// <see cref="PasswordHashes.Unusable"/>). Nobody gets administrator access by default: an
/// operator sets <c>Admin__InitialPassword</c> and this activates the account once. It never
/// replaces a password that has already been set, so it is safe to leave configured.
/// </summary>
public static class AdminBootstrap
{
    public const string Username = "admin";

    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var options = provider.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value;

        // Nothing configured: do not touch the database at all (hosts that start before the
        // schema exists, such as the integration-test host, must keep working).
        if (string.IsNullOrEmpty(options.InitialPassword))
            return;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(AdminBootstrap));
        var db = provider.GetRequiredService<AppDbContext>();

        var normalized = AuthService.NormalizeUsername(Username);
        var account = await db.Accounts.SingleOrDefaultAsync(
            item => item.NormalizedUsername == normalized && item.Role == AccountRole.Administrator,
            cancellationToken);
        if (account is null)
            return;

        if (options.InitialPassword.Length < AdminBootstrapOptions.MinimumPasswordLength)
        {
            throw new InvalidOperationException(
                $"Admin:InitialPassword must be at least {AdminBootstrapOptions.MinimumPasswordLength} characters.");
        }

        if (account.PasswordHash != PasswordHashes.Unusable)
            return; // already activated: never overwrite a password someone chose

        var hasher = provider.GetRequiredService<IPasswordHasher<Account>>();
        account.PasswordHash = hasher.HashPassword(account, options.InitialPassword);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("The administrator account has been activated.");
    }
}
