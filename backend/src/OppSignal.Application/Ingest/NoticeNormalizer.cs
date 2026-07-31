using System.Globalization;
using OppSignal.Application.Common;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Ingest;

/// <summary>
/// Converts a raw <see cref="SamOpportunityDto"/> into a normalized
/// <see cref="Notice"/>: enum mapping, UTC date parsing, place-of-performance
/// flattening, and agency-path splitting. Pure and unit-tested.
/// </summary>
public static class NoticeNormalizer
{
    public static Notice ToNotice(SamOpportunityDto dto, DateTime nowUtc)
    {
        var agencyPath = string.IsNullOrWhiteSpace(dto.FullParentPathName) ? null : dto.FullParentPathName!.Trim();
        var (dept, subTier, office) = SplitAgencyPath(agencyPath);

        var primary = dto.PointOfContact?
            .OrderByDescending(p => string.Equals(p.Type, "primary", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        var pop = dto.PlaceOfPerformance;

        var postedDate = ParseDate(dto.PostedDate) ?? nowUtc.Date;

        return new Notice
        {
            NoticeId = dto.NoticeId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "(untitled notice)" : dto.Title!.Trim(),
            SolicitationNumber = Trim(dto.SolicitationNumber),
            Type = SamMappings.ParseNoticeType(dto.Type ?? dto.BaseType),
            BaseType = Trim(dto.BaseType),
            AgencyPath = agencyPath,
            DepartmentName = dept,
            SubTierName = subTier,
            OfficeName = office,
            NaicsCode = Trim(dto.NaicsCode),
            PscCode = Trim(dto.ClassificationCode),
            SetAside = SamMappings.ParseSetAside(dto.TypeOfSetAside),
            SetAsideDescription = Trim(dto.TypeOfSetAsideDescription),
            PostedDate = DateTime.SpecifyKind(postedDate, DateTimeKind.Utc),
            ResponseDeadline = ParseDateTimeUtc(dto.ResponseDeadLine),
            ArchiveDate = ParseDate(dto.ArchiveDate) is { } ad ? DateTime.SpecifyKind(ad, DateTimeKind.Utc) : null,
            PopState = Trim(pop?.State?.Code)?.ToUpperInvariant(),
            PopCity = Trim(pop?.City?.Name),
            PopZip = Trim(pop?.Zip),
            PopCountry = Trim(pop?.Country?.Code),
            UiLink = Trim(dto.UiLink),
            DescriptionLink = Trim(dto.Description),
            Description = Trim(dto.DescriptionText),
            PrimaryContactName = Trim(primary?.FullName),
            PrimaryContactEmail = Trim(primary?.Email),
            PrimaryContactPhone = Trim(primary?.Phone),
            IsActive = dto.Active is null || dto.Active.Equals("Yes", StringComparison.OrdinalIgnoreCase),
            RawJson = string.IsNullOrWhiteSpace(dto.RawJson) ? "{}" : dto.RawJson,
            FirstSeenAt = nowUtc,
            LastSeenAt = nowUtc,
            SourceUpdatedAt = DateTime.SpecifyKind(postedDate, DateTimeKind.Utc),
        };
    }

    /// <summary>Split "DEPT.SUBTIER.OFFICE" -> (department, sub-tier, office).</summary>
    public static (string? Dept, string? SubTier, string? Office) SplitAgencyPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return (null, null, null);
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dept = parts.Length > 0 ? parts[0] : null;
        var subTier = parts.Length > 1 ? parts[1] : null;
        var office = parts.Length > 0 ? parts[^1] : null;
        return (dept, subTier, office);
    }

    private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DateTime? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt.Date;
        return null;
    }

    private static DateTime? ParseDateTimeUtc(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return dto.UtcDateTime;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return null;
    }
}
