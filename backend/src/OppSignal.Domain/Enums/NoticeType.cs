namespace OppSignal.Domain.Enums;

/// <summary>
/// SAM.gov opportunity notice types. Maps to the <c>ptype</c> codes and the
/// human-readable <c>type</c>/<c>baseType</c> strings returned by the API.
/// </summary>
public enum NoticeType
{
    Unknown = 0,
    Presolicitation = 1,      // ptype p
    Solicitation = 2,         // ptype o
    CombinedSynopsis = 3,     // ptype k  (Combined Synopsis/Solicitation)
    SourcesSought = 4,        // ptype r
    SpecialNotice = 5,        // ptype s
    AwardNotice = 6,          // ptype a
    Justification = 7,        // ptype u  (Justification & Approval)
    SaleOfSurplus = 8,        // ptype g
    IntentToBundle = 9,       // ptype i
}
