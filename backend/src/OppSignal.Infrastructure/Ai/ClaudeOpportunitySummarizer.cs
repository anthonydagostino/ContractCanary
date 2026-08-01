using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Ai;
using OppSignal.Domain.Entities;

namespace OppSignal.Infrastructure.Ai;

/// <summary>
/// Summarizes an opportunity via the Anthropic Messages API. Configured purely by
/// env (Ai__ApiKey, Ai__Model, …). Uses structured output so the response is
/// guaranteed-parseable JSON. Returns null on any failure so enrichment skips and retries.
/// </summary>
public sealed class ClaudeOpportunitySummarizer : IOpportunitySummarizer
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<ClaudeOpportunitySummarizer> _log;

    public ClaudeOpportunitySummarizer(HttpClient http, IOptions<AiOptions> options, ILogger<ClaudeOpportunitySummarizer> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public bool Enabled => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<OpportunityAiSummary?> SummarizeAsync(Notice notice, CancellationToken ct = default)
    {
        if (!Enabled) return null;

        var userPrompt = SummaryPromptBuilder.BuildUser(notice, _options.MaxDescriptionChars);

        var payload = new
        {
            model = _options.Model,
            max_tokens = _options.MaxOutputTokens,
            system = SummaryPromptBuilder.System,
            messages = new[] { new { role = "user", content = userPrompt } },
            // Structured output → the response text block is valid JSON in this shape.
            output_config = new
            {
                format = new
                {
                    type = "json_schema",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            summary = new { type = "string" },
                            keyPoints = new { type = "array", items = new { type = "string" } },
                            fitNote = new { type = "string" },
                        },
                        required = new[] { "summary", "keyPoints", "fitNote" },
                        additionalProperties = false,
                    },
                },
            },
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));

        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", _options.AnthropicVersion);

            using var response = await _http.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Anthropic summarize failed ({Status}) for {NoticeId}: {Body}",
                    (int)response.StatusCode, notice.NoticeId, Truncate(body, 500));
                return null;
            }

            return SummaryResponseParser.Parse(body, _options.Model);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _log.LogWarning("Anthropic summarize timed out for {NoticeId}", notice.NoticeId);
            return null;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
