using System.Security.Cryptography;
using System.Text;
using OppSignal.Application.Awards;

namespace OppSignal.Infrastructure.Awards;

/// <summary>
/// Deterministic sample award data for dev/demo/tests: a handful of plausible
/// expiring incumbent contracts per NAICS code, stable across runs (seeded from
/// the code itself) so ingest idempotency is exercised for real.
/// </summary>
public sealed class FixtureAwardsClient : IAwardsClient
{
    private static readonly string[] Recipients =
    {
        "Summit Federal Services LLC", "Ironclad Solutions Inc", "BlueRidge Technologies",
        "Patriot Support Group", "Meridian Systems Corp", "Cardinal Point Partners",
    };

    private static readonly string[] Agencies =
    {
        "Department of Defense", "Department of Veterans Affairs", "General Services Administration",
        "Department of Homeland Security", "Department of the Army",
    };

    private static readonly string[] States = { "VA", "MD", "TX", "CA", "FL", "CO" };

    public string Source => "Fixture";

    public Task<IReadOnlyList<AwardRecord>> FetchExpiringAwardsAsync(
        string naicsCode, DateOnly endFrom, DateOnly endTo, CancellationToken ct = default)
    {
        var seedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(naicsCode.ToUpperInvariant()));
        var seed = BitConverter.ToInt32(seedBytes, 0);
        var rng = new Random(seed);

        // Fill a prefix code out to a plausible 6-digit leaf so fixtures behave
        // like real data ("54" watches produce "5415xx" awards).
        var leaf = naicsCode.Length >= 6
            ? naicsCode[..6]
            : naicsCode + string.Concat(Enumerable.Range(0, 6 - naicsCode.Length).Select(_ => rng.Next(1, 9).ToString()));

        var count = 3 + Math.Abs(seed % 3); // 3–5 awards per code
        var spanDays = Math.Max(30, endTo.DayNumber - endFrom.DayNumber);
        var list = new List<AwardRecord>(count);
        for (var i = 0; i < count; i++)
        {
            var end = endFrom.AddDays(rng.Next(20, spanDays));
            var start = end.AddYears(-5);
            var amount = Math.Round((decimal)(rng.NextDouble() * 4_500_000 + 250_000), 2);
            var key = $"CONT_AWD_FIX{Math.Abs(seed)}_{i}";
            list.Add(new AwardRecord(
                AwardKey: key,
                DisplayId: $"W91{Math.Abs(seed) % 100:D2}-{20 + i}-C-{1000 + i}",
                RecipientName: Recipients[(Math.Abs(seed) + i) % Recipients.Length],
                RecipientUei: $"FIXUEI{Math.Abs(seed) % 1000:D3}{i:D3}",
                AwardingAgency: Agencies[(Math.Abs(seed) + i) % Agencies.Length],
                NaicsCode: leaf,
                PscCode: null,
                ObligatedAmount: amount,
                PotentialTotalValue: Math.Round(amount * 1.4m, 2),
                PeriodOfPerformanceStart: start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                PeriodOfPerformanceEnd: end.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                PopState: States[(Math.Abs(seed) + i) % States.Length],
                RawJson: "{\"fixture\":true}"));
        }

        return Task.FromResult<IReadOnlyList<AwardRecord>>(list);
    }
}
