using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.UnitTests.Support;

/// <summary>Fluent-ish builders for concise, readable matcher tests.</summary>
public static class TestData
{
    public static Notice Notice(
        string id = "n1",
        string title = "IT Support Services for the Army",
        string? description = "Provide help desk and network support.",
        string? naics = "541519",
        string? psc = "D399",
        SetAsideCode setAside = SetAsideCode.TotalSmallBusiness,
        string? state = "VA",
        NoticeType type = NoticeType.CombinedSynopsis,
        string? agencyPath = "DEPT OF DEFENSE.DEPT OF THE ARMY.ARMY CONTRACTING COMMAND")
        => new()
        {
            NoticeId = id,
            Title = title,
            Description = description,
            NaicsCode = naics,
            PscCode = psc,
            SetAside = setAside,
            PopState = state,
            Type = type,
            AgencyPath = agencyPath,
            PostedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    public static MatchProfile Profile(
        List<string>? naics = null,
        List<string>? psc = null,
        List<string>? keywords = null,
        List<string>? agencyPaths = null,
        List<SetAsideCode>? setAsides = null,
        List<string>? states = null,
        List<NoticeType>? noticeTypes = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Test profile",
            Naics = naics ?? new(),
            Psc = psc ?? new(),
            Keywords = keywords ?? new(),
            AgencyPaths = agencyPaths ?? new(),
            SetAsides = setAsides ?? new(),
            States = states ?? new(),
            NoticeTypes = noticeTypes ?? new(),
            IsActive = true,
        };
}
