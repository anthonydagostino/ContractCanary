using OppSignal.Domain.Enums;

namespace OppSignal.Application.Common;

/// <summary>
/// Bidirectional maps between SAM.gov string codes/labels and our domain enums.
/// Used by the ingest normalizer, the fixture generator, the reference seed, and
/// the API when translating profile filter values.
/// </summary>
public static class SamMappings
{
    // ---- Notice types --------------------------------------------------------

    /// <summary>ptype single-letter code -> NoticeType.</summary>
    public static readonly IReadOnlyDictionary<string, NoticeType> PTypeToNoticeType =
        new Dictionary<string, NoticeType>(StringComparer.OrdinalIgnoreCase)
        {
            ["p"] = NoticeType.Presolicitation,
            ["o"] = NoticeType.Solicitation,
            ["k"] = NoticeType.CombinedSynopsis,
            ["r"] = NoticeType.SourcesSought,
            ["s"] = NoticeType.SpecialNotice,
            ["a"] = NoticeType.AwardNotice,
            ["u"] = NoticeType.Justification,
            ["g"] = NoticeType.SaleOfSurplus,
            ["i"] = NoticeType.IntentToBundle,
        };

    public static readonly IReadOnlyDictionary<NoticeType, string> NoticeTypeToPType =
        PTypeToNoticeType.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>Human "type"/"baseType" strings from the API -> NoticeType.</summary>
    public static readonly IReadOnlyDictionary<string, NoticeType> DisplayToNoticeType =
        new Dictionary<string, NoticeType>(StringComparer.OrdinalIgnoreCase)
        {
            ["Presolicitation"] = NoticeType.Presolicitation,
            ["Solicitation"] = NoticeType.Solicitation,
            ["Combined Synopsis/Solicitation"] = NoticeType.CombinedSynopsis,
            ["Sources Sought"] = NoticeType.SourcesSought,
            ["Special Notice"] = NoticeType.SpecialNotice,
            ["Award Notice"] = NoticeType.AwardNotice,
            ["Justification"] = NoticeType.Justification,
            ["Justification and Approval (J&A)"] = NoticeType.Justification,
            ["Sale of Surplus Property"] = NoticeType.SaleOfSurplus,
            ["Intent to Bundle Requirements (DoD-Funded)"] = NoticeType.IntentToBundle,
        };

    private static readonly IReadOnlyDictionary<NoticeType, string> NoticeTypeDisplay =
        new Dictionary<NoticeType, string>
        {
            [NoticeType.Presolicitation] = "Presolicitation",
            [NoticeType.Solicitation] = "Solicitation",
            [NoticeType.CombinedSynopsis] = "Combined Synopsis/Solicitation",
            [NoticeType.SourcesSought] = "Sources Sought",
            [NoticeType.SpecialNotice] = "Special Notice",
            [NoticeType.AwardNotice] = "Award Notice",
            [NoticeType.Justification] = "Justification (J&A)",
            [NoticeType.SaleOfSurplus] = "Sale of Surplus Property",
            [NoticeType.IntentToBundle] = "Intent to Bundle Requirements (DoD-Funded)",
            [NoticeType.Unknown] = "Notice",
        };

    public static NoticeType ParseNoticeType(string? display)
    {
        if (string.IsNullOrWhiteSpace(display)) return NoticeType.Unknown;
        return DisplayToNoticeType.TryGetValue(display.Trim(), out var t) ? t : NoticeType.Unknown;
    }

    public static string NoticeTypeLabel(NoticeType t) =>
        NoticeTypeDisplay.TryGetValue(t, out var s) ? s : "Notice";

    // ---- Set-asides ----------------------------------------------------------

    public static readonly IReadOnlyDictionary<string, SetAsideCode> CodeToSetAside =
        new Dictionary<string, SetAsideCode>(StringComparer.OrdinalIgnoreCase)
        {
            ["SBA"] = SetAsideCode.TotalSmallBusiness,
            ["SBP"] = SetAsideCode.PartialSmallBusiness,
            ["8A"] = SetAsideCode.EightA,
            ["8AN"] = SetAsideCode.EightASoleSource,
            ["HZC"] = SetAsideCode.HubZone,
            ["HZS"] = SetAsideCode.HubZoneSoleSource,
            ["SDVOSBC"] = SetAsideCode.Sdvosb,
            ["SDVOSBS"] = SetAsideCode.SdvosbSoleSource,
            ["WOSB"] = SetAsideCode.Wosb,
            ["WOSBSS"] = SetAsideCode.WosbSoleSource,
            ["EDWOSB"] = SetAsideCode.Edwosb,
            ["EDWOSBSS"] = SetAsideCode.EdwosbSoleSource,
            ["LAS"] = SetAsideCode.LocalArea,
            ["IEE"] = SetAsideCode.IndianEconomicEnterprise,
            ["ISBEE"] = SetAsideCode.IndianSmallBusinessEE,
            ["BI"] = SetAsideCode.BuyIndian,
            ["VSA"] = SetAsideCode.VeteranOwned,
            ["VSS"] = SetAsideCode.VeteranOwned,
        };

    public static readonly IReadOnlyDictionary<SetAsideCode, string> SetAsideToCode =
        new Dictionary<SetAsideCode, string>
        {
            [SetAsideCode.TotalSmallBusiness] = "SBA",
            [SetAsideCode.PartialSmallBusiness] = "SBP",
            [SetAsideCode.EightA] = "8A",
            [SetAsideCode.EightASoleSource] = "8AN",
            [SetAsideCode.HubZone] = "HZC",
            [SetAsideCode.HubZoneSoleSource] = "HZS",
            [SetAsideCode.Sdvosb] = "SDVOSBC",
            [SetAsideCode.SdvosbSoleSource] = "SDVOSBS",
            [SetAsideCode.Wosb] = "WOSB",
            [SetAsideCode.WosbSoleSource] = "WOSBSS",
            [SetAsideCode.Edwosb] = "EDWOSB",
            [SetAsideCode.EdwosbSoleSource] = "EDWOSBSS",
            [SetAsideCode.LocalArea] = "LAS",
            [SetAsideCode.IndianEconomicEnterprise] = "IEE",
            [SetAsideCode.IndianSmallBusinessEE] = "ISBEE",
            [SetAsideCode.BuyIndian] = "BI",
            [SetAsideCode.VeteranOwned] = "VSA",
        };

    private static readonly IReadOnlyDictionary<SetAsideCode, (string Name, string Description)> SetAsideInfo =
        new Dictionary<SetAsideCode, (string, string)>
        {
            [SetAsideCode.None] = ("Full & Open", "No set-aside; full and open competition."),
            [SetAsideCode.TotalSmallBusiness] = ("Small Business", "Total Small Business Set-Aside (FAR 19.5)."),
            [SetAsideCode.PartialSmallBusiness] = ("Partial Small Business", "Partial Small Business Set-Aside (FAR 19.5)."),
            [SetAsideCode.EightA] = ("8(a)", "8(a) Business Development Program Set-Aside (FAR 19.8)."),
            [SetAsideCode.EightASoleSource] = ("8(a) Sole Source", "8(a) Sole Source (FAR 19.8)."),
            [SetAsideCode.HubZone] = ("HUBZone", "HUBZone Set-Aside (FAR 19.13)."),
            [SetAsideCode.HubZoneSoleSource] = ("HUBZone Sole Source", "HUBZone Sole Source (FAR 19.13)."),
            [SetAsideCode.Sdvosb] = ("SDVOSB", "Service-Disabled Veteran-Owned Small Business Set-Aside (FAR 19.14)."),
            [SetAsideCode.SdvosbSoleSource] = ("SDVOSB Sole Source", "SDVOSB Sole Source (FAR 19.14)."),
            [SetAsideCode.Wosb] = ("WOSB", "Women-Owned Small Business Program Set-Aside (FAR 19.15)."),
            [SetAsideCode.WosbSoleSource] = ("WOSB Sole Source", "WOSB Sole Source (FAR 19.15)."),
            [SetAsideCode.Edwosb] = ("EDWOSB", "Economically Disadvantaged WOSB Set-Aside (FAR 19.15)."),
            [SetAsideCode.EdwosbSoleSource] = ("EDWOSB Sole Source", "EDWOSB Sole Source (FAR 19.15)."),
            [SetAsideCode.LocalArea] = ("Local Area", "Local Area Set-Aside (disaster/emergency, FAR 26.2)."),
            [SetAsideCode.IndianEconomicEnterprise] = ("Indian Economic Enterprise", "Buy Indian Act — Indian Economic Enterprise."),
            [SetAsideCode.IndianSmallBusinessEE] = ("Indian SB Economic Enterprise", "Buy Indian Act — Indian Small Business Economic Enterprise."),
            [SetAsideCode.BuyIndian] = ("Buy Indian", "Buy Indian Act Set-Aside."),
            [SetAsideCode.VeteranOwned] = ("Veteran-Owned", "Veteran-Owned Small Business Set-Aside."),
            [SetAsideCode.Other] = ("Other", "Other / uncategorized set-aside."),
        };

    public static SetAsideCode ParseSetAside(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return SetAsideCode.None;
        return CodeToSetAside.TryGetValue(code.Trim(), out var s) ? s : SetAsideCode.Other;
    }

    public static string SetAsideName(SetAsideCode c) =>
        SetAsideInfo.TryGetValue(c, out var i) ? i.Name : c.ToString();

    public static string SetAsideDescription(SetAsideCode c) =>
        SetAsideInfo.TryGetValue(c, out var i) ? i.Description : c.ToString();

    /// <summary>Notice types exposed in the profile UI (spec: solicitation, presolicitation, sources sought, combined synopsis).</summary>
    public static IEnumerable<NoticeType> ProfileSelectableNoticeTypes => new[]
    {
        NoticeType.Solicitation,
        NoticeType.Presolicitation,
        NoticeType.SourcesSought,
        NoticeType.CombinedSynopsis,
    };

    /// <summary>Set-asides exposed in the profile UI (spec: small business, 8(a), SDVOSB, WOSB, HUBZone, etc.).</summary>
    public static IEnumerable<SetAsideCode> ProfileSelectableSetAsides => new[]
    {
        SetAsideCode.TotalSmallBusiness,
        SetAsideCode.PartialSmallBusiness,
        SetAsideCode.EightA,
        SetAsideCode.HubZone,
        SetAsideCode.Sdvosb,
        SetAsideCode.Wosb,
        SetAsideCode.Edwosb,
        SetAsideCode.VeteranOwned,
    };
}
