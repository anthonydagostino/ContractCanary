using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Enums;

namespace OppSignal.Infrastructure.Ingest;

/// <summary>
/// Real SAM.gov Get Opportunities Public API v2 client. See docs/SAM_API.md.
/// GET {BaseUrl}/opportunities/v2/search with required postedFrom/postedTo
/// (MM/dd/yyyy), limit (≤1000), offset, and api_key. Retries 429/5xx with
/// exponential backoff, honoring Retry-After.
/// </summary>
public sealed class SamOpportunitiesClient : ISamOpportunitiesClient
{
    private readonly HttpClient _http;
    private readonly SamOptions _options;
    private readonly ILogger<SamOpportunitiesClient> _log;

    public SamOpportunitiesClient(HttpClient http, IOptions<SamOptions> options, ILogger<SamOpportunitiesClient> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public IngestSource Source => IngestSource.Sam;

    public async Task<SamPage> FetchPageAsync(
        DateOnly postedFrom, DateOnly postedTo, int limit, int offset, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Sam:ApiKey is not configured. Set the SAM.gov API key (env Sam__ApiKey) or use Ingest__Source=Fixture.");

        var q = HttpUtility.ParseQueryString(string.Empty);
        q["limit"] = Math.Clamp(limit, 1, 1000).ToString(CultureInfo.InvariantCulture);
        q["offset"] = Math.Max(0, offset).ToString(CultureInfo.InvariantCulture);
        q["postedFrom"] = postedFrom.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        q["postedTo"] = postedTo.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        if (!_options.UseHeaderAuth) q["api_key"] = _options.ApiKey;

        var url = $"{_options.BaseUrl.TrimEnd('/')}/opportunities/v2/search?{q}";

        using var response = await SendWithRetryAsync(url, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return Parse(await JsonDocument.ParseAsync(stream, cancellationToken: ct));
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string url, CancellationToken ct)
    {
        var attempts = Math.Max(1, _options.RetryCount);
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (_options.UseHeaderAuth) request.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (HttpRequestException) when (attempt < attempts)
            {
                await BackoffAsync(attempt, null, ct);
                continue;
            }

            var retryable = response.StatusCode == HttpStatusCode.TooManyRequests
                            || (int)response.StatusCode >= 500;
            if (!retryable || attempt >= attempts) return response;

            var retryAfter = response.Headers.RetryAfter?.Delta;
            _log.LogWarning("SAM API {Status} on attempt {Attempt}/{Max}; backing off", (int)response.StatusCode, attempt, attempts);
            response.Dispose();
            await BackoffAsync(attempt, retryAfter, ct);
        }
    }

    private static async Task BackoffAsync(int attempt, TimeSpan? retryAfter, CancellationToken ct)
    {
        var delay = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2,4,8,16s
        await Task.Delay(delay, ct);
    }

    private static SamPage Parse(JsonDocument doc)
    {
        var root = doc.RootElement;
        var total = root.TryGetProperty("totalRecords", out var tr) && tr.TryGetInt32(out var t) ? t : 0;
        var limit = root.TryGetProperty("limit", out var l) && l.TryGetInt32(out var lv) ? lv : 0;
        var offset = root.TryGetProperty("offset", out var o) && o.TryGetInt32(out var ov) ? ov : 0;

        var items = new List<SamOpportunityDto>();
        if (root.TryGetProperty("opportunitiesData", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var dto = el.Deserialize<SamOpportunityDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                          ?? new SamOpportunityDto();
                dto.RawJson = el.GetRawText();
                if (!string.IsNullOrWhiteSpace(dto.NoticeId)) items.Add(dto);
            }
        }

        return new SamPage(total, limit, offset, items);
    }
}
