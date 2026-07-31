using Microsoft.AspNetCore.Identity;

namespace OppSignal.Infrastructure.Identity;

/// <summary>
/// Application user. Extends ASP.NET Core Identity with profile + admin fields.
/// Lives in Infrastructure because it depends on the Identity package; domain
/// entities reference it only by <c>UserId</c> (Guid), keeping the domain pure.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }

    /// <summary>IANA timezone id used for digest send timing. Default America/New_York.</summary>
    public string TimeZoneId { get; set; } = "America/New_York";

    public bool IsAdmin { get; set; }

    public DateTime CreatedAt { get; set; }
}
