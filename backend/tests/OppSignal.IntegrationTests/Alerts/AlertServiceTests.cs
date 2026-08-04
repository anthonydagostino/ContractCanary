using FluentAssertions;
using OppSignal.Application.Alerts;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Alerts;

public class AlertServiceTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public AlertServiceTests(PostgresTestDatabase pg) => _pg = pg;
    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private AlertService Service(AppDbContext db) => new(db, _clock);

    private NoticeAlert Alert(Guid user, string noticeId, AlertType type, string message, DateTime at, DateTime? readAt = null)
        => new() { UserId = user, NoticeId = noticeId, Type = type, Message = message, CreatedAt = at, ReadAt = readAt };

    [Fact]
    public async Task List_returns_newest_first_with_the_notice_title_and_type_label()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A1"));
        await db.SaveChangesAsync();
        db.NoticeAlerts.Add(Alert(user, "A1", AlertType.DeadlineChanged, "moved", _clock.UtcNow));
        db.NoticeAlerts.Add(Alert(user, "A1", AlertType.Cancelled, "cancelled", _clock.UtcNow.AddMinutes(1)));
        await db.SaveChangesAsync();

        var list = await Service(db).ListAsync(user);

        list.Should().HaveCount(2);
        list[0].Message.Should().Be("cancelled");              // newest first
        list[0].TypeLabel.Should().Be("Cancelled");
        list[0].NoticeTitle.Should().Be("Notice A1");
        list[1].TypeLabel.Should().Be("Deadline changed");
        list.Should().OnlyContain(a => !a.Read);
    }

    [Fact]
    public async Task List_order_is_deterministic_after_the_join()
    {
        // Regression: OrderBy-before-Take becomes a LIMIT subquery whose result
        // order the outer join does not preserve; the feed needs its own ORDER BY.
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A9"));
        await db.SaveChangesAsync();
        for (var i = 0; i < 10; i++)
            db.NoticeAlerts.Add(Alert(user, "A9", AlertType.DeadlineChanged, $"msg-{i}", _clock.UtcNow.AddMinutes(i)));
        await db.SaveChangesAsync();

        var list = await Service(db).ListAsync(user);

        list.Select(a => a.CreatedAt).Should().BeInDescendingOrder();
        list.First().Message.Should().Be("msg-9");
        list.Last().Message.Should().Be("msg-0");
    }

    [Fact]
    public async Task Unread_count_ignores_already_read_alerts()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A2"));
        await db.SaveChangesAsync();
        db.NoticeAlerts.Add(Alert(user, "A2", AlertType.DeadlineChanged, "unread", _clock.UtcNow));
        db.NoticeAlerts.Add(Alert(user, "A2", AlertType.Cancelled, "read", _clock.UtcNow, readAt: _clock.UtcNow));
        await db.SaveChangesAsync();

        (await Service(db).UnreadCountAsync(user)).Should().Be(1);
    }

    [Fact]
    public async Task Mark_all_read_stamps_every_unread_alert()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A3"));
        await db.SaveChangesAsync();
        db.NoticeAlerts.Add(Alert(user, "A3", AlertType.DeadlineChanged, "one", _clock.UtcNow));
        db.NoticeAlerts.Add(Alert(user, "A3", AlertType.Cancelled, "two", _clock.UtcNow));
        await db.SaveChangesAsync();

        await Service(db).MarkAllReadAsync(user);

        (await Service(db).UnreadCountAsync(user)).Should().Be(0);
        (await Service(db).ListAsync(user)).Should().OnlyContain(a => a.Read);
    }

    [Fact]
    public async Task Mark_all_read_with_nothing_unread_is_a_no_op()
    {
        // No alerts at all → early return, must not throw.
        await Service(_pg.NewContext()).MarkAllReadAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task Alerts_are_scoped_to_their_own_user()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("A4"));
        await db.SaveChangesAsync();
        db.NoticeAlerts.Add(Alert(a, "A4", AlertType.DeadlineChanged, "for-a", _clock.UtcNow));
        db.NoticeAlerts.Add(Alert(b, "A4", AlertType.Cancelled, "for-b", _clock.UtcNow));
        await db.SaveChangesAsync();

        (await Service(db).ListAsync(a)).Should().ContainSingle().Which.Message.Should().Be("for-a");

        // Marking A read must not touch B's alert.
        await Service(db).MarkAllReadAsync(a);
        (await Service(db).UnreadCountAsync(b)).Should().Be(1);
    }
}
