using FluentAssertions;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using Xunit;

namespace OppSignal.UnitTests.Ingest;

public class NoticeChangeDetectorTests
{
    private static Notice N(bool active = true, DateTime? deadline = null) => new()
    {
        NoticeId = "x",
        Title = "T",
        IsActive = active,
        ResponseDeadline = deadline,
    };

    private static readonly DateTime D1 = new(2026, 7, 1, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime D2 = new(2026, 7, 15, 17, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Reports_cancellation_when_active_to_inactive()
    {
        var changes = NoticeChangeDetector.Detect(N(active: true, deadline: D1), N(active: false, deadline: D1));
        changes.Should().ContainSingle();
        changes[0].Type.Should().Be(AlertType.Cancelled);
        changes[0].Message.Should().Contain("cancelled");
    }

    [Fact]
    public void Cancellation_supersedes_a_deadline_change()
    {
        var changes = NoticeChangeDetector.Detect(N(active: true, deadline: D1), N(active: false, deadline: D2));
        changes.Should().ContainSingle().Which.Type.Should().Be(AlertType.Cancelled);
    }

    [Fact]
    public void Reports_deadline_move_with_from_and_to()
    {
        var changes = NoticeChangeDetector.Detect(N(deadline: D1), N(deadline: D2));
        changes.Should().ContainSingle();
        changes[0].Type.Should().Be(AlertType.DeadlineChanged);
        changes[0].Message.Should().Contain("Jul 15, 2026");
        changes[0].Message.Should().Contain("moved from");
    }

    [Fact]
    public void Reports_deadline_set_when_previously_none()
    {
        var changes = NoticeChangeDetector.Detect(N(deadline: null), N(deadline: D2));
        changes.Should().ContainSingle();
        changes[0].Type.Should().Be(AlertType.DeadlineChanged);
        changes[0].Message.Should().Contain("set to");
    }

    [Fact]
    public void No_alert_when_deadline_unchanged()
    {
        NoticeChangeDetector.Detect(N(deadline: D1), N(deadline: D1)).Should().BeEmpty();
    }

    [Fact]
    public void No_alert_on_reactivation_with_same_deadline()
    {
        NoticeChangeDetector.Detect(N(active: false, deadline: D1), N(active: true, deadline: D1)).Should().BeEmpty();
    }
}
