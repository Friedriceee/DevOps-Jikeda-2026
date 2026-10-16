using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TakeoutPlatform.Api.Features.Audit;

namespace TakeoutPlatform.Api.Tests;

public class AuditLogServiceTests : ServiceTestBase
{
    [Test]
    public async Task Log_and_query_persist_a_complete_record()
    {
        await using (var db = CreateContext())
        {
            var service = new AuditLogService(db);
            await service.LogAsync(Event("CouponCreated", "PlatformCoupon", "12"), CancellationToken.None);
        }

        await using var verify = CreateContext();
        var page = await new AuditLogService(verify).QueryAsync(
            "admin", "CouponCreated", "PlatformCoupon", "Success",
            null, null, 1, 20, CancellationToken.None);

        Assert.That(page.TotalCount, Is.EqualTo(1));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(page.Items[0].Actor, Is.EqualTo("admin"));
            Assert.That(page.Items[0].TargetId, Is.EqualTo("12"));
            Assert.That(page.Items[0].CorrelationId, Is.EqualTo("trace-123"));
        }));
    }

    [Test]
    public async Task Query_returns_newest_records_first_and_paginates()
    {
        await using (var db = CreateContext())
        {
            db.AuditLogs.AddRange(
                Log("Old", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)),
                Log("New", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc)));
            await db.SaveChangesAsync();
        }

        await using var verify = CreateContext();
        var page = await new AuditLogService(verify).QueryAsync(
            null, null, null, null, null, null, 1, 1, CancellationToken.None);
        Assert.That(page.TotalCount, Is.EqualTo(2));
        Assert.That(page.Items.Single().Action, Is.EqualTo("New"));
    }

    [Test]
    public async Task Existing_audit_record_cannot_be_updated_or_deleted()
    {
        await using var db = CreateContext();
        db.AuditLogs.Add(Log("Created", DateTime.UtcNow));
        await db.SaveChangesAsync();
        var log = await db.AuditLogs.SingleAsync();
        log.Result = "Changed";

        var error = Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(() => db.SaveChangesAsync()));
        Assert.That(error!.Message, Does.Contain("append-only"));
    }

    [Test]
    public void Blank_required_field_is_rejected()
    {
        using var db = CreateContext();
        var service = new AuditLogService(db);
        var invalid = Event(" ", "Account", "1");
        Assert.ThrowsAsync<ArgumentException>((Func<Task>)(() => service.LogAsync(invalid, CancellationToken.None)));
    }

    private static AuditEvent Event(string action, string targetType, string targetId) =>
        new("admin", "Administrator", action, targetType, targetId, "Success",
            new DateTime(2026, 10, 14, 1, 2, 3, DateTimeKind.Utc), "trace-123");

    private static AuditLog Log(string action, DateTime timestamp) => new()
    {
        Actor = "admin", Role = "Administrator", Action = action,
        TargetType = "Account", TargetId = "1", Result = "Success",
        TimestampUtc = timestamp, CorrelationId = "trace",
    };
}
