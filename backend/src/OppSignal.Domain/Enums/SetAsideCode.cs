namespace OppSignal.Domain.Enums;

/// <summary>
/// Set-aside program of a notice / a profile filter. Maps to SAM.gov
/// <c>typeOfSetAside</c> codes (see <c>SamMappings</c> in the Application layer).
/// <see cref="None"/> means full &amp; open (no set-aside); <see cref="Other"/>
/// preserves matches for codes we don't individually enumerate.
/// </summary>
public enum SetAsideCode
{
    None = 0,
    TotalSmallBusiness = 1,       // SBA
    PartialSmallBusiness = 2,     // SBP
    EightA = 3,                   // 8A
    EightASoleSource = 4,         // 8AN
    HubZone = 5,                  // HZC
    HubZoneSoleSource = 6,        // HZS
    Sdvosb = 7,                   // SDVOSBC
    SdvosbSoleSource = 8,         // SDVOSBS
    Wosb = 9,                     // WOSB
    WosbSoleSource = 10,          // WOSBSS
    Edwosb = 11,                  // EDWOSB
    EdwosbSoleSource = 12,        // EDWOSBSS
    LocalArea = 13,               // LAS
    IndianEconomicEnterprise = 14,// IEE
    IndianSmallBusinessEE = 15,   // ISBEE
    BuyIndian = 16,               // BI
    VeteranOwned = 17,            // VSA / VSS
    Other = 999,
}
