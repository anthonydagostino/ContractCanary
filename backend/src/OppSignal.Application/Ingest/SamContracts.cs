using System.Text.Json.Serialization;

namespace OppSignal.Application.Ingest;

/// <summary>
/// Raw SAM.gov opportunity record (subset we consume), modeled on the
/// Opportunities v2 <c>opportunitiesData[]</c> element. The original element JSON
/// is preserved in <see cref="RawJson"/> for the <c>Notice.RawJson</c> column.
/// </summary>
public sealed class SamOpportunityDto
{
    [JsonPropertyName("noticeId")] public string NoticeId { get; set; } = default!;
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("solicitationNumber")] public string? SolicitationNumber { get; set; }
    [JsonPropertyName("fullParentPathName")] public string? FullParentPathName { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("baseType")] public string? BaseType { get; set; }
    [JsonPropertyName("typeOfSetAside")] public string? TypeOfSetAside { get; set; }
    [JsonPropertyName("typeOfSetAsideDescription")] public string? TypeOfSetAsideDescription { get; set; }
    [JsonPropertyName("responseDeadLine")] public string? ResponseDeadLine { get; set; }
    [JsonPropertyName("postedDate")] public string? PostedDate { get; set; }
    [JsonPropertyName("archiveDate")] public string? ArchiveDate { get; set; }
    [JsonPropertyName("naicsCode")] public string? NaicsCode { get; set; }
    [JsonPropertyName("classificationCode")] public string? ClassificationCode { get; set; }
    [JsonPropertyName("active")] public string? Active { get; set; }
    [JsonPropertyName("uiLink")] public string? UiLink { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; } // this is a URL
    [JsonPropertyName("organizationType")] public string? OrganizationType { get; set; }

    /// <summary>
    /// Resolved description TEXT (not part of the base API record, which only gives
    /// a URL). The fixture populates this; the real client can populate it later by
    /// fetching the noticedesc resource. When present it becomes Notice.Description.
    /// </summary>
    [JsonPropertyName("descriptionText")] public string? DescriptionText { get; set; }

    [JsonPropertyName("placeOfPerformance")] public SamPlaceOfPerformance? PlaceOfPerformance { get; set; }
    [JsonPropertyName("pointOfContact")] public List<SamPointOfContact>? PointOfContact { get; set; }

    /// <summary>Original element JSON (set by the client), stored verbatim.</summary>
    [JsonIgnore] public string RawJson { get; set; } = "{}";
}

public sealed class SamPlaceOfPerformance
{
    [JsonPropertyName("city")] public SamCodeName? City { get; set; }
    [JsonPropertyName("state")] public SamCodeName? State { get; set; }
    [JsonPropertyName("zip")] public string? Zip { get; set; }
    [JsonPropertyName("country")] public SamCodeName? Country { get; set; }
}

public sealed class SamCodeName
{
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed class SamPointOfContact
{
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("fullName")] public string? FullName { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
}

/// <summary>One page of opportunity results.</summary>
public sealed record SamPage(int TotalRecords, int Limit, int Offset, IReadOnlyList<SamOpportunityDto> Items);
