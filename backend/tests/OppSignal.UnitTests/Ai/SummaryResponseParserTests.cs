using FluentAssertions;
using OppSignal.Application.Ai;
using Xunit;

namespace OppSignal.UnitTests.Ai;

public class SummaryResponseParserTests
{
    private const string Model = "claude-haiku-4-5";

    private static string Envelope(string innerJson, string stopReason = "end_turn")
        => $$"""
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-4-5",
         "stop_reason":"{{stopReason}}",
         "content":[{"type":"text","text":{{System.Text.Json.JsonSerializer.Serialize(innerJson)}}}]}
        """;

    [Fact]
    public void Parses_a_valid_structured_response()
    {
        var inner = """{"summary":"Short overview.","keyPoints":["One","Two"],"fitNote":"Good for small IT firms."}""";
        var result = SummaryResponseParser.Parse(Envelope(inner), Model);

        result.Should().NotBeNull();
        result!.Summary.Should().Be("Short overview.");
        result.KeyPoints.Should().Equal("One", "Two");
        result.FitNote.Should().Be("Good for small IT firms.");
        result.Model.Should().Be(Model);
    }

    [Fact]
    public void Returns_null_on_refusal()
    {
        var inner = """{"summary":"x","keyPoints":[],"fitNote":"y"}""";
        SummaryResponseParser.Parse(Envelope(inner, stopReason: "refusal"), Model).Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_summary_missing()
    {
        var result = SummaryResponseParser.ParseSummaryObject("""{"keyPoints":["a"],"fitNote":"b"}""", Model);
        result.Should().BeNull();
    }

    [Fact]
    public void Tolerates_surrounding_prose()
    {
        var text = "Here is the summary:\n{\"summary\":\"Overview\",\"keyPoints\":[\"x\"],\"fitNote\":\"z\"}\nThanks!";
        var result = SummaryResponseParser.ParseSummaryObject(text, Model);
        result.Should().NotBeNull();
        result!.Summary.Should().Be("Overview");
    }

    [Fact]
    public void Handles_missing_keypoints_gracefully()
    {
        var result = SummaryResponseParser.ParseSummaryObject("""{"summary":"Overview","fitNote":"z"}""", Model);
        result.Should().NotBeNull();
        result!.KeyPoints.Should().BeEmpty();
    }

    [Fact]
    public void Returns_null_on_malformed_json()
    {
        SummaryResponseParser.Parse("not json at all", Model).Should().BeNull();
        SummaryResponseParser.ParseSummaryObject("no braces here", Model).Should().BeNull();
    }
}
