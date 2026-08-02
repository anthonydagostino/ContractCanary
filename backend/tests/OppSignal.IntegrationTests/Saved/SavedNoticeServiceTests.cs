using FluentAssertions;
using OppSignal.Application.Common;
using OppSignal.Application.Saved;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Saved;

public class SavedNoticeServiceTests : IClassFixture<PostgresTestDatabase>, IAsyncLifetime
{
    private readonly PostgresTestDatabase _pg;
    private readonly FixedClock _clock = new(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));

    public SavedNoticeServiceTests(PostgresTestDatabase pg) => _pg = pg;
    public Task InitializeAsync() => _pg.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private SavedNoticeService Service(AppDbContext db) => new(db, _clock);

    [Fact]
    public async Task Save_then_list_returns_it_in_the_reviewing_stage_with_note_trimmed()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-1"));
        await db.SaveChangesAsync();

        await Service(db).SaveAsync(user, "SV-1", "  draft by Friday  ");

        var list = await Service(db).ListAsync(user);
        list.Should().HaveCount(1);
        list[0].NoticeId.Should().Be("SV-1");
        list[0].Status.Should().Be(PipelineStatus.Reviewing);
        list[0].Note.Should().Be("draft by Friday");
        list[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Save_is_idempotent_and_updates_the_note_on_resave()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-2"));
        await db.SaveChangesAsync();

        await Service(db).SaveAsync(user, "SV-2", "first");
        await Service(db).SaveAsync(user, "SV-2", "second");

        (await Service(db).CountAsync(user)).Should().Be(1);
        (await Service(db).ListAsync(user))[0].Note.Should().Be("second");
    }

    [Fact]
    public async Task Whitespace_note_is_stored_as_null()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-3"));
        await db.SaveChangesAsync();

        await Service(db).SaveAsync(user, "SV-3", "   ");

        (await Service(db).ListAsync(user))[0].Note.Should().BeNull();
    }

    [Fact]
    public async Task Saving_an_unknown_notice_throws_not_found()
    {
        await using var db = _pg.NewContext();
        await Assert.ThrowsAsync<NotFoundException>(
            () => Service(db).SaveAsync(Guid.NewGuid(), "does-not-exist", null));
    }

    [Fact]
    public async Task Unsave_removes_the_save_and_is_idempotent()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-4"));
        await db.SaveChangesAsync();
        var svc = Service(db);

        await svc.SaveAsync(user, "SV-4", null);
        await svc.UnsaveAsync(user, "SV-4");
        (await svc.CountAsync(user)).Should().Be(0);

        await svc.UnsaveAsync(user, "SV-4"); // second unsave: no-op, no throw
        (await svc.CountAsync(user)).Should().Be(0);
    }

    [Fact]
    public async Task Update_status_moves_the_pipeline_stage()
    {
        var user = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-5"));
        await db.SaveChangesAsync();
        var svc = Service(db);

        await svc.SaveAsync(user, "SV-5", null);
        await svc.UpdateStatusAsync(user, "SV-5", PipelineStatus.Pursuing);

        (await svc.ListAsync(user))[0].Status.Should().Be(PipelineStatus.Pursuing);
    }

    [Fact]
    public async Task Update_status_on_an_unsaved_notice_throws_not_found()
    {
        await using var db = _pg.NewContext();
        db.Notices.Add(TestEntities.Notice("SV-6"));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => Service(db).UpdateStatusAsync(Guid.NewGuid(), "SV-6", PipelineStatus.Won));
    }

    [Fact]
    public async Task List_is_scoped_to_the_user_and_ordered_newest_first()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using var db = _pg.NewContext();
        db.Notices.AddRange(TestEntities.Notice("N1"), TestEntities.Notice("N2"), TestEntities.Notice("N3"));
        await db.SaveChangesAsync();
        var svc = Service(db);

        _clock.UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        await svc.SaveAsync(a, "N1", null);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await svc.SaveAsync(a, "N2", null);
        await svc.SaveAsync(b, "N3", null);

        (await svc.ListAsync(a)).Select(x => x.NoticeId).Should().Equal("N2", "N1");
        (await svc.ListAsync(b)).Select(x => x.NoticeId).Should().Equal("N3");
    }
}
