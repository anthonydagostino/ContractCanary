using FluentAssertions;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Enums;
using Xunit;

namespace OppSignal.UnitTests.Ingest;

public class NoticeNormalizerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Maps_core_fields_and_enums()
    {
        var dto = new SamOpportunityDto
        {
            NoticeId = "abc123",
            Title = "  IT Support  ",
            SolicitationNumber = "W912-24-R-0001",
            FullParentPathName = "DEPT OF DEFENSE.DEPT OF THE ARMY.ARMY CONTRACTING COMMAND",
            Type = "Combined Synopsis/Solicitation",
            TypeOfSetAside = "SDVOSBC",
            NaicsCode = "541519",
            ClassificationCode = "D399",
            PostedDate = "2026-06-10",
            ResponseDeadLine = "2026-07-01T17:00:00-04:00",
            Active = "Yes",
            PlaceOfPerformance = new SamPlaceOfPerformance
            {
                State = new SamCodeName { Code = "va" }, City = new SamCodeName { Name = "Arlington" }, Zip = "22202",
            },
            DescriptionText = "help desk and network support",
        };

        var n = NoticeNormalizer.ToNotice(dto, Now);

        n.NoticeId.Should().Be("abc123");
        n.Title.Should().Be("IT Support");
        n.Type.Should().Be(NoticeType.CombinedSynopsis);
        n.SetAside.Should().Be(SetAsideCode.Sdvosb);
        n.DepartmentName.Should().Be("DEPT OF DEFENSE");
        n.SubTierName.Should().Be("DEPT OF THE ARMY");
        n.OfficeName.Should().Be("ARMY CONTRACTING COMMAND");
        n.NaicsCode.Should().Be("541519");
        n.PscCode.Should().Be("D399");
        n.PopState.Should().Be("VA");
        n.Description.Should().Be("help desk and network support");
        n.ResponseDeadline.Should().Be(new DateTime(2026, 7, 1, 21, 0, 0, DateTimeKind.Utc)); // -04:00 -> UTC
        n.PostedDate.Kind.Should().Be(DateTimeKind.Utc);
        n.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Handles_missing_optional_fields_gracefully()
    {
        var dto = new SamOpportunityDto { NoticeId = "x", Title = null, PostedDate = null };
        var n = NoticeNormalizer.ToNotice(dto, Now);

        n.Title.Should().Be("(untitled notice)");
        n.Type.Should().Be(NoticeType.Unknown);
        n.SetAside.Should().Be(SetAsideCode.None);
        n.PostedDate.Date.Should().Be(Now.Date);
        n.ResponseDeadline.Should().BeNull();
        n.PopState.Should().BeNull();
    }

    [Theory]
    [InlineData("va", "VA")]                                   // normal 2-letter US code, upper-cased
    [InlineData("nsw", "NSW")]                                 // international province: >2 chars, must not overflow
    [InlineData("Virginia", "VIRGINIA")]                       // malformed record: full name in the code field
    [InlineData("THIS-IS-A-VERY-LONG-BOGUS-VALUE", "THIS-IS-A-VERY-L")] // pathological: capped to column width (16)
    public void PopState_accepts_longer_or_messy_codes_without_overflow(string code, string expected)
    {
        // Regression: real SAM data carries place-of-performance state codes longer than
        // the original varchar(2), which aborted the whole ingest batch. Normalizer now
        // upper-cases and caps to the (widened) column width instead of failing.
        var dto = new SamOpportunityDto
        {
            NoticeId = "x",
            PlaceOfPerformance = new SamPlaceOfPerformance { State = new SamCodeName { Code = code } },
        };

        var n = NoticeNormalizer.ToNotice(dto, Now);

        n.PopState.Should().Be(expected);
        n.PopState!.Length.Should().BeLessThanOrEqualTo(16);
    }

    [Fact]
    public void Oversize_bounded_strings_are_capped_instead_of_failing_the_batch()
    {
        // Regression (same class as the PopState overflow): any column-bounded
        // string that exceeds its width fails the ENTIRE ingest batch on every
        // run for the whole rolling window — a multi-day outage from one bad
        // record. Title was unguarded.
        var dto = new SamOpportunityDto
        {
            NoticeId = "x",
            Title = new string('T', 2000),
            SolicitationNumber = new string('S', 400),
            FullParentPathName = new string('A', 1500),
            NaicsCode = "541511-TOO-LONG-CODE",
            ClassificationCode = "D302-TOO-LONG-CODE",
            PlaceOfPerformance = new SamPlaceOfPerformance { Country = new SamCodeName { Code = "TOOLONGCOUNTRY" } },
        };

        var n = NoticeNormalizer.ToNotice(dto, Now);

        n.Title.Length.Should().Be(1024);
        n.SolicitationNumber!.Length.Should().Be(256);
        n.AgencyPath!.Length.Should().Be(1024);
        n.DepartmentName!.Length.Should().BeLessThanOrEqualTo(512);
        n.OfficeName!.Length.Should().BeLessThanOrEqualTo(512);
        n.NaicsCode!.Length.Should().Be(12);
        n.PscCode!.Length.Should().Be(12);
        n.PopCountry!.Length.Should().Be(8);
    }

    [Theory]
    [InlineData("DEPT OF DEFENSE", "DEPT OF DEFENSE", null, "DEPT OF DEFENSE")]
    [InlineData("A.B.C.D", "A", "B", "D")]
    [InlineData("", null, null, null)]
    public void SplitAgencyPath_cases(string path, string? dept, string? sub, string? office)
    {
        var (d, s, o) = NoticeNormalizer.SplitAgencyPath(string.IsNullOrEmpty(path) ? null : path);
        d.Should().Be(dept);
        s.Should().Be(sub);
        o.Should().Be(office);
    }
}
