using FluentAssertions;
using OppSignal.Application.Ai;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using Xunit;

namespace OppSignal.UnitTests.Ai;

public class SummaryPromptBuilderTests
{
    private static Notice Sample(string? description = "Provide help desk support.") => new()
    {
        NoticeId = "abc",
        Title = "IT Help Desk Support",
        AgencyPath = "DEPT OF DEFENSE.DEPT OF THE ARMY",
        Type = NoticeType.CombinedSynopsis,
        NaicsCode = "541519",
        PscCode = "D399",
        SetAside = SetAsideCode.Sdvosb,
        PopCity = "Arlington",
        PopState = "VA",
        PostedDate = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
        Description = description,
    };

    [Fact]
    public void Includes_core_fields_and_description()
    {
        var prompt = SummaryPromptBuilder.BuildUser(Sample());

        prompt.Should().Contain("IT Help Desk Support");
        prompt.Should().Contain("DEPT OF DEFENSE.DEPT OF THE ARMY");
        prompt.Should().Contain("541519");
        prompt.Should().Contain("Arlington, VA");
        prompt.Should().Contain("Provide help desk support.");
    }

    [Fact]
    public void Strips_html_from_description()
    {
        var n = Sample("<p>Provide <b>help&nbsp;desk</b> support.</p>");
        var prompt = SummaryPromptBuilder.BuildUser(n);

        prompt.Should().NotContain("<p>");
        prompt.Should().NotContain("<b>");
        prompt.Should().Contain("Provide");
        prompt.Should().Contain("support.");
    }

    [Fact]
    public void Notes_when_description_missing()
    {
        var prompt = SummaryPromptBuilder.BuildUser(Sample(description: null));
        prompt.Should().Contain("details are limited");
    }

    [Fact]
    public void Truncates_long_description()
    {
        var big = new string('x', 20_000);
        var prompt = SummaryPromptBuilder.BuildUser(Sample(big), maxDescriptionChars: 500);

        prompt.Should().Contain("[truncated]");
        prompt.Length.Should().BeLessThan(2_000);
    }
}
