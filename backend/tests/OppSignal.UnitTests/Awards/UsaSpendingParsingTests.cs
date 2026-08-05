using System.Text.Json;
using FluentAssertions;
using OppSignal.Infrastructure.Awards;
using Xunit;

namespace OppSignal.UnitTests.Awards;

public class UsaSpendingParsingTests
{
    private static JsonElement Root(string json) => JsonDocument.Parse(json).RootElement;

    private const string SampleRow =
        """
        {
          "generated_internal_id": "CONT_AWD_123_9700",
          "Award ID": "W91QUZ-22-C-0042",
          "Recipient Name": "IRONCLAD SOLUTIONS INC",
          "Recipient UEI": "ABC123DEF456",
          "Awarding Agency": "Department of Defense",
          "Award Amount": 2145000.5,
          "Start Date": "2022-10-01",
          "End Date": "2026-11-30",
          "NAICS": "541511",
          "PSC": "D302",
          "Place of Performance State Code": "VA"
        }
        """;

    [Fact]
    public void Parses_a_complete_row_inside_the_window()
    {
        var root = Root($$"""{"results":[{{SampleRow}}]}""");
        var (records, sawOlder, count) = UsaSpendingAwardsClient.ParsePage(
            root, endFrom: new DateOnly(2026, 8, 1), endTo: new DateOnly(2027, 12, 31));

        count.Should().Be(1);
        sawOlder.Should().BeFalse();
        var r = records.Should().ContainSingle().Subject;
        r.AwardKey.Should().Be("CONT_AWD_123_9700");
        r.DisplayId.Should().Be("W91QUZ-22-C-0042");
        r.RecipientName.Should().Be("IRONCLAD SOLUTIONS INC");
        r.AwardingAgency.Should().Be("Department of Defense");
        r.ObligatedAmount.Should().Be(2145000.5m);
        r.NaicsCode.Should().Be("541511");
        r.PscCode.Should().Be("D302");
        r.PeriodOfPerformanceEnd.Should().Be(new DateTime(2026, 11, 30));
        r.PopState.Should().Be("VA");
    }

    [Fact]
    public void Rows_ending_before_the_window_are_skipped_and_flag_the_stop_condition()
    {
        var old = SampleRow.Replace("2026-11-30", "2025-01-15");
        var root = Root($$"""{"results":[{{old}}]}""");
        var (records, sawOlder, _) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        records.Should().BeEmpty();
        sawOlder.Should().BeTrue("descending End Date sort means later pages are older still");
    }

    [Fact]
    public void Rows_ending_after_the_window_are_skipped_without_stopping()
    {
        var far = SampleRow.Replace("2026-11-30", "2031-01-15");
        var root = Root($$"""{"results":[{{far}}]}""");
        var (records, sawOlder, _) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        records.Should().BeEmpty();
        sawOlder.Should().BeFalse();
    }

    [Fact]
    public void Tolerates_missing_optional_fields_and_decorated_code_values()
    {
        // Real responses sometimes render "541511 - Custom Computer Programming
        // Services" and omit optional fields; neither may sink the parse.
        var root = Root(
            """
            {"results":[{
              "generated_internal_id": "CONT_AWD_MIN",
              "End Date": "2026-12-01",
              "NAICS": "541511 - Custom Computer Programming Services",
              "Award Amount": "1500000"
            }]}
            """);
        var (records, _, _) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        var r = records.Should().ContainSingle().Subject;
        r.NaicsCode.Should().Be("541511");
        r.ObligatedAmount.Should().Be(1_500_000m);
        r.RecipientName.Should().BeNull();
    }

    [Theory]
    [InlineData("2026-08-01")] // exactly endFrom
    [InlineData("2027-12-31")] // exactly endTo
    public void Window_boundaries_are_inclusive(string endDate)
    {
        var row = SampleRow.Replace("2026-11-30", endDate);
        var root = Root($$"""{"results":[{{row}}]}""");
        var (records, _, _) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        records.Should().ContainSingle("an award ending exactly on a window edge is still a recompete");
    }

    [Fact]
    public void Amounts_with_thousands_separators_parse()
    {
        var row = SampleRow.Replace("2145000.5", "\"2,145,000.50\"");
        var root = Root($$"""{"results":[{{row}}]}""");
        var (records, _, _) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        records.Should().ContainSingle().Which.ObligatedAmount.Should().Be(2_145_000.50m);
    }

    [Fact]
    public void Rows_without_a_key_or_end_date_are_skipped_and_odd_payloads_do_not_throw()
    {
        var root = Root(
            """
            {"results":[
              {"Award ID":"NO-KEY","End Date":"2026-12-01"},
              {"generated_internal_id":"NO-END"},
              {"generated_internal_id":"BAD-END","End Date":"not-a-date"}
            ]}
            """);
        var (records, _, count) = UsaSpendingAwardsClient.ParsePage(
            root, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));

        count.Should().Be(3);
        records.Should().BeEmpty();

        var noResults = Root("""{"page_metadata":{}}""");
        var (empty, stop, _) = UsaSpendingAwardsClient.ParsePage(noResults, new DateOnly(2026, 8, 1), new DateOnly(2027, 12, 31));
        empty.Should().BeEmpty();
        stop.Should().BeTrue();
    }

    [Fact]
    public void Request_body_carries_the_naics_filter_window_and_paging()
    {
        var body = UsaSpendingAwardsClient.BuildRequestBody("5415", new DateOnly(2027, 2, 1), pageSize: 100, page: 3);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        root.GetProperty("filters").GetProperty("naics_codes")[0].GetString().Should().Be("5415");
        root.GetProperty("filters").GetProperty("award_type_codes").GetArrayLength().Should().Be(4);
        root.GetProperty("sort").GetString().Should().Be("End Date");
        root.GetProperty("order").GetString().Should().Be("desc");
        root.GetProperty("page").GetInt32().Should().Be(3);
        root.GetProperty("limit").GetInt32().Should().Be(100);
        root.GetProperty("subawards").GetBoolean().Should().BeFalse();
    }
}
