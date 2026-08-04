using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Notices;

public class NoticeQueryTests : ApiTestBase
{
    public NoticeQueryTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Posted_date_filters_return_200_not_500()
    {
        // Regression: query-string dates bind with Kind=Unspecified, which Npgsql
        // refuses to compare against timestamptz — a documented filter 500'd.
        var (client, _, _) = await RegisterAndLoginAsync($"dates_{Guid.NewGuid():N}@test.dev");

        var res = await client.GetAsync("/api/notices?postedFrom=2026-01-01&postedTo=2026-12-31");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deadline_sort_pagination_neither_drops_nor_duplicates_tied_rows()
    {
        // Regression: the deadline sort had no unique tiebreak, so Skip/Take
        // pages over notices sharing a deadline could repeat and drop rows.
        var (client, _, _) = await RegisterAndLoginAsync($"pages_{Guid.NewGuid():N}@test.dev");

        var sharedDeadline = DateTime.UtcNow.AddDays(14);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            for (var i = 0; i < 30; i++)
                db.Notices.Add(TestEntities.Notice($"TIE-{i:D3}", naics: "541511", deadline: sharedDeadline));
            await db.SaveChangesAsync();
        }

        var seen = new List<string>();
        for (var page = 1; page <= 3; page++)
        {
            var res = await client.GetFromJsonAsync<PagedResult>(
                $"/api/notices?sort=deadline&direction=asc&page={page}&pageSize=10");
            seen.AddRange(res!.Items.Select(i => i.NoticeId));
        }

        seen.Should().OnlyHaveUniqueItems("no row may appear on two pages");
        seen.Where(id => id.StartsWith("TIE-")).Should().HaveCount(30, "no tied row may be dropped between pages");
    }

    [Fact]
    public async Task Csv_export_starts_with_a_utf8_bom_and_uses_consistent_newlines()
    {
        var (client, _, userId) = await RegisterAndLoginAsync($"csv_{Guid.NewGuid():N}@test.dev");
        await SetPlanAsync(userId, PlanTier.Pro);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(TestEntities.Notice("CSV-1", naics: "541511"));
            await db.SaveChangesAsync();
        }

        var res = await client.GetAsync("/api/notices/export.csv");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await res.Content.ReadAsByteArrayAsync();

        // BOM keeps Excel from mangling accented agency names.
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF });

        var text = System.Text.Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        text.Should().NotContain("\r\n", "line endings must be consistent LF");
        text.Split('\n').First().Should().StartWith("NoticeId,Title");
    }

    private sealed record PagedResult(List<Item> Items, int Total, int Page, int TotalPages);
    private sealed record Item(string NoticeId);
}
