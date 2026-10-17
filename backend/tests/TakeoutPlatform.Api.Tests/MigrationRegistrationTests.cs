using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using TakeoutPlatform.Api.Data;

namespace TakeoutPlatform.Api.Tests;

/// <summary>
/// EF Core only runs a migration class that carries [DbContext] and [Migration].
/// A hand-written migration without them compiles fine and is silently ignored, so a
/// database created from the migrations is missing its changes (this once left a fresh
/// database without the second demo merchant). This test needs no database.
/// </summary>
public class MigrationRegistrationTests
{
    private static IEnumerable<Type> MigrationClasses() =>
        typeof(AppDbContext).Assembly.GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract);

    [Test]
    public void Every_migration_class_is_registered_with_EF_Core()
    {
        var unregistered = MigrationClasses()
            .Where(type => type.GetCustomAttribute<MigrationAttribute>() is null
                || type.GetCustomAttribute<DbContextAttribute>()?.ContextType != typeof(AppDbContext))
            .Select(type => type.Name)
            .ToList();

        Assert.That(unregistered, Is.Empty,
            "Add [DbContext(typeof(AppDbContext))] and [Migration(\"<id>\")] to: " + string.Join(", ", unregistered));
    }

    [Test]
    public void Migration_ids_are_unique_and_match_the_file_naming_pattern()
    {
        var ids = MigrationClasses()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>()!.Id)
            .ToList();

        Assert.That(ids, Is.Unique);
        Assert.That(ids, Has.All.Matches(@"^\d{12,14}_[A-Za-z0-9]+$"));
    }
}
