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
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "(untitled notice)" : Cap(dto.Title, 1024)!,
            SolicitationNumber = Cap(dto.SolicitationNumber, 256),
            Type = SamMappings.ParseNoticeType(dto.Type ?? dto.BaseType),
            BaseType = Trim(dto.BaseType),
            AgencyPath = Cap(agencyPath, 1024),
            DepartmentName = Cap(dept, 512),
            SubTierName = Cap(subTier, 512),
            OfficeName = Cap(office, 512),
            NaicsCode = Cap(dto.NaicsCode, 12),
            PscCode = Cap(dto.ClassificationCode, 12),
            SetAside = SamMappings.ParseSetAside(dto.TypeOfSetAside),
            SetAsideDescription = Trim(dto.TypeOfSetAsideDescription),
            PostedDate = DateTime.SpecifyKind(postedDate, DateTimeKind.Utc),
            ResponseDeadline = ParseDateTimeUtc(dto.ResponseDeadLine),
            ArchiveDate = ParseDate(dto.ArchiveDate) is { } ad ? DateTime.SpecifyKind(ad, DateTimeKind.Utc) : null,
            PopState = StateCode(pop?.State?.Code),
            PopCity = Trim(pop?.City?.Name),
            PopZip = Trim(pop?.Zip),
            PopCountry = Cap(pop?.Country?.Code, 8),
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

    // Every column-bounded string must be capped to its width: SAM data
    // occasionally exceeds the documented sizes, and one oversize value would
    // otherwise fail the entire ingest batch on every run for the whole
    // rolling window (a multi-day outage from a single bad record).
    private static string? Cap(string? s, int max)
    {
        var t = Trim(s);
        return t is null || t.Length <= max ? t : t[..max];
    }

    // Place-of-performance state: usually a 2-letter US code, but SAM data can carry
    // longer international province codes or malformed values. Store what's there,
    // upper-cased and capped to the column width, so one bad record can't fail the batch.
    private const int PopStateMaxLength = 16;
    private static string? StateCode(string? s)
    {
        var t = Trim(s)?.ToUpperInvariant();
        if (t is null) return null;
        return t.Length <= PopStateMaxLength ? t : t[..PopStateMaxLength];
    }

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
