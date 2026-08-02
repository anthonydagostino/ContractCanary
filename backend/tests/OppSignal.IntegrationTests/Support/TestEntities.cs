using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.IntegrationTests.Support;

/// <summary>Small builders for seeding domain entities in integration tests.</summary>
public static class TestEntities
{
    private static readonly DateTime Seen = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    public static Notice Notice(
        string id,
        string? naics = null,
        string? dept = null,
        NoticeType type = NoticeType.Solicitation,
        bool active = true,
        DateTime? posted = null,
        DateTime? deadline = null,
        SetAsideCode setAside = SetAsideCode.None,
        string? rawJson = null)
        => new()
        {
            NoticeId = id,
            Title = $"Notice {id}",
            Type = type,
            NaicsCode = naics,
            DepartmentName = dept,
            AgencyPath = dept,
            SetAside = setAside,
            PostedDate = posted ?? new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ResponseDeadline = deadline,
            IsActive = active,
            RawJson = rawJson ?? "{}",
            FirstSeenAt = Seen,
            LastSeenAt = Seen,
        };

    public static MatchProfile Profile(Guid userId, string name, params string[] naics)
        => new()
        {
            UserId = userId,
            Name = name,
            Naics = naics.ToList(),
            NoticeTypes = new(),
            IsActive = true,
            CreatedAt = Seen,
            UpdatedAt = Seen,
        };
}
