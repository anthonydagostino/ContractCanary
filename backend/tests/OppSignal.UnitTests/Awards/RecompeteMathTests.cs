using FluentAssertions;
using OppSignal.Application.Awards;
using OppSignal.Infrastructure.Awards;
using Xunit;

namespace OppSignal.UnitTests.Awards;

public class RecompeteMathTests
{
    private static readonly DateTime Now = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Window_opens_twelve_months_before_the_period_of_performance_ends()
    {
        RecompeteMath.WindowOpens(new DateTime(2027, 10, 1)).Should().Be(new DateTime(2026, 10, 1));
        RecompeteMath.WindowOpens(null).Should().BeNull();
    }

    [Theory]
    [InlineData("2027-02-01", true)]   // ends in 6 months → window opened 6 months ago
    [InlineData("2027-08-05", true)]   // ends in exactly 12 months → opens today
    [InlineData("2028-06-01", false)]  // ends in ~22 months → window still ahead
    public void Window_open_state_is_relative_to_now(string end, bool expected)
    {
        RecompeteMath.IsWindowOpen(DateTime.Parse(end), Now).Should().Be(expected);
    }

    [Fact]
    public void Fixture_awards_are_deterministic_and_inside_the_requested_window()
    {
        var client = new FixtureAwardsClient();
        var from = new DateOnly(2026, 8, 5);
        var to = from.AddMonths(18);

        var first = client.FetchExpiringAwardsAsync("541511", from, to).Result;
        var second = client.FetchExpiringAwardsAsync("541511", from, to).Result;

        first.Should().NotBeEmpty();
        second.Select(a => a.AwardKey).Should().Equal(first.Select(a => a.AwardKey),
            "ingest idempotency tests depend on stable fixture keys");
        first.Should().OnlyContain(a => a.PeriodOfPerformanceEnd != null
            && DateOnly.FromDateTime(a.PeriodOfPerformanceEnd!.Value) >= from
            && DateOnly.FromDateTime(a.PeriodOfPerformanceEnd!.Value) <= to);
        first.Should().OnlyContain(a => a.NaicsCode!.StartsWith("541511"),
            "fixture awards must fall under the watched code so prefix matching works end to end");
    }

    [Fact]
    public void Prefix_codes_produce_leaf_naics_fixtures()
    {
        var client = new FixtureAwardsClient();
        var from = new DateOnly(2026, 8, 5);
        var awards = client.FetchExpiringAwardsAsync("54", from, from.AddMonths(18)).Result;

        awards.Should().OnlyContain(a => a.NaicsCode!.Length == 6 && a.NaicsCode.StartsWith("54"));
    }
}
