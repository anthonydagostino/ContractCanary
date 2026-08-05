using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Awards;

namespace OppSignal.Infrastructure.Awards;

/// <summary>
/// USAspending.gov "spending_by_award" client (free public API, no key).
/// POST /api/v2/search/spending_by_award/ filtered to prime contracts
/// (award type codes A–D) under one NAICS code, sorted by End Date descending;
/// we page until rows fall past the requested window and keep the ones inside
/// it. Parsing is deliberately tolerant — the API adds/renames display fields
/// over time, and a missing optional field must never sink the ingest.
/// </summary>
public sealed class UsaSpendingAwardsClient : IAwardsClient
{
    private static readonly string[] Fields =
    {
        "Award ID", "Recipient Name", "Recipient UEI", "Awarding Agency",
        "Award Amount", "Start Date", "End Date", "NAICS", "PSC",
        "Place of Performance State Code",
    };

    private readonly HttpClient _http;
    private readonly AwardsOptions _options;
    private readonly ILogger<UsaSpendingAwardsClient> _log;

    public UsaSpendingAwardsClient(HttpClient http, IOptions<AwardsOptions> options, ILogger<UsaSpendingAwardsClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public string Source => "UsaSpending";

    public async Task<IReadOnlyList<AwardRecord>> FetchExpiringAwardsAsync(
        string naicsCode, DateOnly endFrom, DateOnly endTo, CancellationToken ct = default)
    {
        var results = new List<AwardRecord>();
        var pageSize = Math.Clamp(_options.PageSize, 1, 100);
        var maxPreamblePages = Math.Max(1, _options.MaxPagesPerCode);
        var maxWindowPages = Math.Max(1, _options.MaxWindowPagesPerCode);
        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/v2/search/spending_by_award/";

        // Two paging phases, because descending End Date order serves far-future
        // awards first: the "preamble" (rows beyond endTo) and the window
        // itself. Each gets its own cap — and crucially, the deepest window
        // pages hold the SOONEST-expiring awards, so mid-window truncation
        // would silently drop the most urgent recompetes. Keep it rare & loud.
        var windowPages = 0;
        var reachedWindow = false;
        for (var page = 1; ; page++)
        {
            var body = BuildRequestBody(naicsCode, endTo, pageSize, page);
            using var response = await SendWithRetryAsync(url, body, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var (pageRecords, sawOlderThanWindow, rowCount) = ParsePage(doc.RootElement, endFrom, endTo);
            results.AddRange(pageRecords);
            if (pageRecords.Count > 0 || sawOlderThanWindow) reachedWindow = true;
            if (reachedWindow) windowPages++;

            // Once rows end before the window starts, every later page is older still.
            if (sawOlderThanWindow || rowCount < pageSize) break;

            if (!reachedWindow && page >= maxPreamblePages)
            {
                _log.LogWarning(
                    "USAspending: NAICS {Code} exhausted {Pages} pages of far-future awards without reaching the expiry window — raise Awards:MaxPagesPerCode to surface this code's recompetes",
                    naicsCode, maxPreamblePages);
                break;
            }

            if (reachedWindow && windowPages >= maxWindowPages)
            {
                _log.LogWarning(
                    "USAspending: NAICS {Code} truncated mid-window after {Pages} in-window pages — the SOONEST-expiring awards are on the unfetched pages; raise Awards:MaxWindowPagesPerCode",
                    naicsCode, maxWindowPages);
                break;
            }
        }

        return results;
    }

    internal static string BuildRequestBody(string naicsCode, DateOnly endTo, int pageSize, int page)
    {
        // time_period bounds the award's action dates; a generous 10-year
        // lookback comfortably covers any contract still performing today.
        var body = new
        {
            subawards = false,
            filters = new
            {
                award_type_codes = new[] { "A", "B", "C", "D" },
                naics_codes = new[] { naicsCode },
                time_period = new[]
                {
                    new { start_date = $"{DateTime.UtcNow.AddYears(-10):yyyy-MM-dd}", end_date = endTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                },
            },
            fields = Fields,
            sort = "End Date",
            order = "desc",
            page,
            limit = pageSize,
        };
        return JsonSerializer.Serialize(body);
    }

    /// <summary>Parse one response page. Internal for unit tests with canned JSON.</summary>
    internal static (List<AwardRecord> Records, bool SawOlderThanWindow, int RowCount) ParsePage(
        JsonElement root, DateOnly endFrom, DateOnly endTo)
    {
        var records = new List<AwardRecord>();
        var sawOlder = false;
        var rowCount = 0;

        if (!root.TryGetProperty("results", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return (records, true, 0);

        foreach (var row in rows.EnumerateArray())
        {
            rowCount++;
            var end = GetDate(row, "End Date");
            if (end is null) continue;
            var endDay = DateOnly.FromDateTime(end.Value);
            if (endDay < endFrom) { sawOlder = true; continue; }
            if (endDay > endTo) continue;

            // generated_internal_id is the stable unique key and the deep-link id.
            var key = GetString(row, "generated_internal_id");
            if (string.IsNullOrWhiteSpace(key)) continue;

            records.Add(new AwardRecord(
                AwardKey: key!,
                DisplayId: GetString(row, "Award ID"),
                RecipientName: GetString(row, "Recipient Name"),
                RecipientUei: GetString(row, "Recipient UEI"),
                AwardingAgency: GetString(row, "Awarding Agency"),
                NaicsCode: FirstToken(GetString(row, "NAICS")),
                PscCode: FirstToken(GetString(row, "PSC")),
                ObligatedAmount: GetDecimal(row, "Award Amount"),
                PotentialTotalValue: null,
                PeriodOfPerformanceStart: GetDate(row, "Start Date"),
                PeriodOfPerformanceEnd: end,
                PopState: GetString(row, "Place of Performance State Code"),
                RawJson: row.GetRawText()));
        }

        return (records, sawOlder, rowCount);
    }

    // USAspending sometimes renders coded fields as "541511" and sometimes as
    // "541511 - Custom Computer Programming Services"; keep the code.
    private static string? FirstToken(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        var cut = t.IndexOfAny(new[] { ' ', ':' });
        return cut > 0 ? t[..cut].TrimEnd('-', ' ') : t;
    }

    private static string? GetString(JsonElement row, string name)
        => row.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? GetDecimal(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => null,
        };
    }

    private static DateTime? GetDate(JsonElement row, string name)
    {
        var s = GetString(row, name);
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d.Date
            : null;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string url, string body, CancellationToken ct)
    {
        const int attempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (HttpRequestException) when (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                continue;
            }

            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (!retryable || attempt >= attempts) return response;

            _log.LogWarning("USAspending {Status} on attempt {Attempt}/{Max}; backing off",
                (int)response.StatusCode, attempt, attempts);
            response.Dispose();
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
        }
    }
}
