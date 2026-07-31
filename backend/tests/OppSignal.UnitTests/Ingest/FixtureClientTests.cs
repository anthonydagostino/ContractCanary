using FluentAssertions;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Ingest;
using Xunit;

namespace OppSignal.UnitTests.Ingest;

public class FixtureClientTests
{
    private sealed class Clock : IClock { public DateTime UtcNow { get; set; } = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc); }

    [Fact]
    public async Task Generates_a_large_varied_corpus_within_the_window()
    {
        var client = new FixtureOpportunitiesClient(new Clock());
        var to = new DateOnly(2026, 6, 15);
        var from = to.AddDays(-44);

        var first = await client.FetchPageAsync(from, to, 1000, 0, default);

        first.TotalRecords.Should().BeGreaterThanOrEqualTo(500, "spec requires 500+ notices");
        first.Items.Should().OnlyContain(o => !string.IsNullOrWhiteSpace(o.NoticeId));
        first.Items.Select(o => o.NaicsCode).Distinct().Count().Should().BeGreaterThan(10, "varied NAICS");
        first.Items.Select(o => o.FullParentPathName).Distinct().Count().Should().BeGreaterThan(5, "varied agencies");
    }

    [Fact]
    public async Task Is_deterministic_for_the_same_day()
    {
        var to = new DateOnly(2026, 6, 15);
        var from = to.AddDays(-44);

        var a = await new FixtureOpportunitiesClient(new Clock()).FetchPageAsync(from, to, 50, 0, default);
        var b = await new FixtureOpportunitiesClient(new Clock()).FetchPageAsync(from, to, 50, 0, default);

        a.Items.Select(x => (x.NoticeId, x.Title, x.NaicsCode))
            .Should().Equal(b.Items.Select(x => (x.NoticeId, x.Title, x.NaicsCode)));
    }

    [Fact]
    public async Task Respects_paging_and_date_window()
    {
        var client = new FixtureOpportunitiesClient(new Clock());
        var to = new DateOnly(2026, 6, 15);

        // Narrow 1-day window returns far fewer than the full corpus.
        var narrow = await client.FetchPageAsync(to, to, 1000, 0, default);
        var wide = await client.FetchPageAsync(to.AddDays(-44), to, 1000, 0, default);
        narrow.TotalRecords.Should().BeLessThan(wide.TotalRecords);

        // Paging: page 2 does not overlap page 1.
        var page1 = await client.FetchPageAsync(to.AddDays(-44), to, 25, 0, default);
        var page2 = await client.FetchPageAsync(to.AddDays(-44), to, 25, 25, default);
        page1.Items.Select(i => i.NoticeId).Should().NotIntersectWith(page2.Items.Select(i => i.NoticeId));
    }
}
