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
