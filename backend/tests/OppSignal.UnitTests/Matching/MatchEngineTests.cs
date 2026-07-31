using FluentAssertions;
using OppSignal.Application.Matching;
using OppSignal.Domain.Enums;
using OppSignal.UnitTests.Support;
using Xunit;

namespace OppSignal.UnitTests.Matching;

public class MatchEngineTests
{
    private readonly MatchEngine _engine = new();

    // ---------------------------------------------------------------- empty --

    [Fact]
    public void Empty_profile_matches_any_notice()
    {
        var outcome = _engine.Evaluate(TestData.Notice(), TestData.Profile());
        outcome.IsMatch.Should().BeTrue();
        outcome.Reason.CodeClause.Should().Be("none");
    }

    // --------------------------------------------------------------- NAICS ---

    [Theory]
    [InlineData("541519", true)]   // exact
    [InlineData("541511", false)]  // different code
    public void Naics_only_filter(string noticeNaics, bool expected)
    {
        var notice = TestData.Notice(naics: noticeNaics);
        var profile = TestData.Profile(naics: new() { "541519" });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    [Fact]
    public void Naics_filter_no_match_when_notice_naics_null()
    {
        var notice = TestData.Notice(naics: null);
        var profile = TestData.Profile(naics: new() { "541519" });
        _engine.Evaluate(notice, profile).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Naics_filter_is_case_insensitive_and_multi_value()
    {
        var notice = TestData.Notice(naics: "236220");
        var profile = TestData.Profile(naics: new() { "541519", "236220" });
        _engine.Evaluate(notice, profile).IsMatch.Should().BeTrue();
    }

    // ----------------------------------------------------------------- PSC ---

    [Theory]
    [InlineData("D399", true)]
    [InlineData("R408", false)]
    public void Psc_only_filter(string noticePsc, bool expected)
    {
        var notice = TestData.Notice(psc: noticePsc);
        var profile = TestData.Profile(psc: new() { "D399" });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    // ------------------------------------------------ NAICS OR PSC clause ----
    // Truth table for the one subtle rule.

    [Theory]
    // both filters set: match if EITHER hits
    [InlineData(true, true, "hitN", "hitP", true, "both")]
    [InlineData(true, true, "hitN", "missP", true, "naics")]
    [InlineData(true, true, "missN", "hitP", true, "psc")]
    [InlineData(true, true, "missN", "missP", false, "none")]
    // only NAICS set: PSC is ignored entirely
    [InlineData(true, false, "hitN", "missP", true, "naics")]
    [InlineData(true, false, "missN", "hitP", false, "none")]
    // only PSC set: NAICS is ignored entirely
    [InlineData(false, true, "missN", "hitP", true, "psc")]
    [InlineData(false, true, "hitN", "missP", false, "none")]
    // neither set: always passes the code clause
    [InlineData(false, false, "missN", "missP", true, "none")]
    public void Naics_or_psc_truth_table(
        bool naicsSet, bool pscSet, string naicsCase, string pscCase, bool expectedMatch, string expectedClause)
    {
        // notice carries fixed codes; profile decides whether each filter is set
        var notice = TestData.Notice(naics: "111111", psc: "AAAA");

        var profile = TestData.Profile(
            naics: naicsSet ? new() { naicsCase == "hitN" ? "111111" : "999999" } : new(),
            psc: pscSet ? new() { pscCase == "hitP" ? "AAAA" : "ZZZZ" } : new());

        var outcome = _engine.Evaluate(notice, profile);
        outcome.IsMatch.Should().Be(expectedMatch);
        outcome.Reason.CodeClause.Should().Be(expectedClause);
    }

    // ------------------------------------------------------------- keywords --

    [Theory]
    [InlineData("network", true)]        // substring of description
    [InlineData("NETWORK", true)]        // case-insensitive
    [InlineData("help desk", true)]      // phrase across words
    [InlineData("Army", true)]           // in title
    [InlineData("aviation", false)]      // absent
    public void Keyword_filter(string keyword, bool expected)
    {
        var notice = TestData.Notice(
            title: "IT Support Services for the Army",
            description: "Provide help desk and network support.");
        var profile = TestData.Profile(keywords: new() { keyword });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    [Fact]
    public void Keyword_filter_matches_when_any_keyword_hits()
    {
        var notice = TestData.Notice(description: "network support");
        var profile = TestData.Profile(keywords: new() { "aviation", "network", "welding" });
        var outcome = _engine.Evaluate(notice, profile);
        outcome.IsMatch.Should().BeTrue();
        outcome.Reason.MatchedKeywords.Should().ContainSingle().Which.Should().Be("network");
    }

    [Fact]
    public void Keyword_filter_handles_null_description()
    {
        var notice = TestData.Notice(title: "Roofing repair", description: null);
        _engine.Evaluate(notice, TestData.Profile(keywords: new() { "roofing" })).IsMatch.Should().BeTrue();
        _engine.Evaluate(notice, TestData.Profile(keywords: new() { "plumbing" })).IsMatch.Should().BeFalse();
    }

    // -------------------------------------------------------------- agency ---

    [Theory]
    [InlineData("DEPT OF DEFENSE", true)]                      // root prefix (department)
    [InlineData("dept of defense", true)]                      // case-insensitive
    [InlineData("DEPT OF THE ARMY", true)]                     // path segment (sub-tier)
    [InlineData("ARMY CONTRACTING COMMAND", true)]             // deeper segment
    [InlineData("DEPT OF THE NAVY", false)]                    // different branch
    public void Agency_filter(string agencySelection, bool expected)
    {
        var notice = TestData.Notice(agencyPath: "DEPT OF DEFENSE.DEPT OF THE ARMY.ARMY CONTRACTING COMMAND");
        var profile = TestData.Profile(agencyPaths: new() { agencySelection });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    [Fact]
    public void Agency_filter_no_match_when_path_null()
    {
        var notice = TestData.Notice(agencyPath: null);
        _engine.Evaluate(notice, TestData.Profile(agencyPaths: new() { "DEPT OF DEFENSE" })).IsMatch.Should().BeFalse();
    }

    // ------------------------------------------------------------ set-aside --

    [Theory]
    [InlineData(SetAsideCode.TotalSmallBusiness, true)]
    [InlineData(SetAsideCode.EightA, false)]
    public void SetAside_filter(SetAsideCode selected, bool expected)
    {
        var notice = TestData.Notice(setAside: SetAsideCode.TotalSmallBusiness);
        var profile = TestData.Profile(setAsides: new() { selected });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    // ---------------------------------------------------------------- state --

    [Theory]
    [InlineData("VA", true)]
    [InlineData("va", true)]
    [InlineData("TX", false)]
    public void State_filter(string selected, bool expected)
    {
        var notice = TestData.Notice(state: "VA");
        var profile = TestData.Profile(states: new() { selected });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    [Fact]
    public void State_filter_no_match_when_notice_state_null()
    {
        var notice = TestData.Notice(state: null);
        _engine.Evaluate(notice, TestData.Profile(states: new() { "VA" })).IsMatch.Should().BeFalse();
    }

    // ----------------------------------------------------------- noticeType --

    [Theory]
    [InlineData(NoticeType.CombinedSynopsis, true)]
    [InlineData(NoticeType.SourcesSought, false)]
    public void NoticeType_filter(NoticeType selected, bool expected)
    {
        var notice = TestData.Notice(type: NoticeType.CombinedSynopsis);
        var profile = TestData.Profile(noticeTypes: new() { selected });
        _engine.Evaluate(notice, profile).IsMatch.Should().Be(expected);
    }

    // --------------------------------------- AND semantics: each gate alone --
    // With every filter set to pass, flipping any single filter to fail must
    // drop the overall match. This pins the AND across all gates.

    [Fact]
    public void All_filters_set_and_passing_matches()
    {
        var outcome = _engine.Evaluate(TestData.Notice(), FullyConstrainedProfile());
        outcome.IsMatch.Should().BeTrue();
        outcome.Reason.MatchedNaics.Should().Be("541519");
        outcome.Reason.MatchedKeywords.Should().Contain("support");
        outcome.Reason.MatchedState.Should().Be("VA");
        outcome.Reason.MatchedNoticeType.Should().Be(nameof(NoticeType.CombinedSynopsis));
    }

    [Fact]
    public void Failing_naics_alone_drops_match()
    {
        var notice = TestData.Notice(naics: "999999", psc: "ZZZZ"); // neither code hits
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Failing_keyword_alone_drops_match()
    {
        var notice = TestData.Notice(title: "Bridge painting", description: "exterior coatings");
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Failing_agency_alone_drops_match()
    {
        var notice = TestData.Notice(agencyPath: "DEPT OF THE NAVY.NAVSEA");
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Failing_setaside_alone_drops_match()
    {
        var notice = TestData.Notice(setAside: SetAsideCode.EightA);
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Failing_state_alone_drops_match()
    {
        var notice = TestData.Notice(state: "TX");
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Failing_noticetype_alone_drops_match()
    {
        var notice = TestData.Notice(type: NoticeType.AwardNotice);
        _engine.Evaluate(notice, FullyConstrainedProfile()).IsMatch.Should().BeFalse();
    }

    private static Domain.Entities.MatchProfile FullyConstrainedProfile() => TestData.Profile(
        naics: new() { "541519" },
        psc: new() { "D399" },
        keywords: new() { "support" },
        agencyPaths: new() { "DEPT OF DEFENSE" },
        setAsides: new() { SetAsideCode.TotalSmallBusiness },
        states: new() { "VA" },
        noticeTypes: new() { NoticeType.CombinedSynopsis });
}
