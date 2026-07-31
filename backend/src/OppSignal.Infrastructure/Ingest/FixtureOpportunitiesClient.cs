using System.Globalization;
using System.Text.Json;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Ingest;
using OppSignal.Domain.Enums;

namespace OppSignal.Infrastructure.Ingest;

/// <summary>
/// Deterministic fixture data source modeled field-for-field on the real SAM.gov
/// schema. Generates 600 varied notices spread over the last ~45 days so the whole
/// system runs and demos with ZERO credentials. Honors the same date-window +
/// paging contract as the real client. Corpus is stable within a day (keyed by the
/// clock's date) so re-ingest is idempotent.
/// </summary>
public sealed class FixtureOpportunitiesClient : ISamOpportunitiesClient
{
    private const int Count = 600;
    private const int SpreadDays = 45;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly object Gate = new();
    private static DateOnly _cachedFor;
    private static List<SamOpportunityDto>? _corpus;

    private readonly IClock _clock;

    public FixtureOpportunitiesClient(IClock clock) => _clock = clock;

    public IngestSource Source => IngestSource.Fixture;

    public Task<SamPage> FetchPageAsync(
        DateOnly postedFrom, DateOnly postedTo, int limit, int offset, CancellationToken ct = default)
    {
        var corpus = GetCorpus(DateOnly.FromDateTime(_clock.UtcNow));

        var filtered = corpus
            .Where(o =>
            {
                var d = DateOnly.Parse(o.PostedDate!, CultureInfo.InvariantCulture);
                return d >= postedFrom && d <= postedTo;
            })
            .ToList();

        var pageItems = filtered.Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 1000)).ToList();
        return Task.FromResult(new SamPage(filtered.Count, limit, offset, pageItems));
    }

    private static List<SamOpportunityDto> GetCorpus(DateOnly today)
    {
        lock (Gate)
        {
            if (_corpus is not null && _cachedFor == today) return _corpus;
            _corpus = Build(today);
            _cachedFor = today;
            return _corpus;
        }
    }

    private static List<SamOpportunityDto> Build(DateOnly today)
    {
        var list = new List<SamOpportunityDto>(Count);
        for (var i = 0; i < Count; i++)
        {
            var rng = new Random(unchecked(i * 2654435761u).GetHashCode()); // deterministic per index
            var vertical = Verticals[i % Verticals.Length];
            var agency = Agencies[rng.Next(Agencies.Length)];
            var naics = vertical.Naics[rng.Next(vertical.Naics.Length)];
            var psc = vertical.Psc[rng.Next(vertical.Psc.Length)];
            var (setAsideCode, setAsideDesc) = SetAsides[rng.Next(SetAsides.Length)];
            var (ptypeLabel, _) = NoticeTypes[rng.Next(NoticeTypes.Length)];
            var (state, city, zip) = Places[rng.Next(Places.Length)];

            var posted = today.AddDays(-(i % SpreadDays));
            var deadline = posted.AddDays(14 + rng.Next(32));
            var titleTemplate = vertical.Titles[rng.Next(vertical.Titles.Length)];
            var title = $"{titleTemplate} — {agency.Office}";
            var offsetStr = "-05:00";

            var dto = new SamOpportunityDto
            {
                NoticeId = $"FIX-{i:D5}",
                Title = title,
                SolicitationNumber = $"{agency.Abbrev}-{today.Year % 100}-{vertical.Prefix}-{i:D4}",
                FullParentPathName = agency.Path,
                Type = ptypeLabel,
                BaseType = ptypeLabel,
                TypeOfSetAside = setAsideCode,
                TypeOfSetAsideDescription = setAsideDesc,
                ResponseDeadLine = $"{deadline:yyyy-MM-dd}T17:00:00{offsetStr}",
                PostedDate = posted.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ArchiveDate = deadline.AddDays(15).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                NaicsCode = naics,
                ClassificationCode = psc,
                Active = "Yes",
                UiLink = $"https://sam.gov/opp/FIX{i:D5}/view",
                Description = $"https://api.sam.gov/opportunities/v1/noticedesc?noticeid=FIX-{i:D5}",
                OrganizationType = "OFFICE",
                PlaceOfPerformance = new SamPlaceOfPerformance
                {
                    City = new SamCodeName { Name = city },
                    State = new SamCodeName { Code = state, Name = state },
                    Zip = zip,
                    Country = new SamCodeName { Code = "USA", Name = "UNITED STATES" },
                },
                PointOfContact = new List<SamPointOfContact>
                {
                    new()
                    {
                        Type = "primary",
                        FullName = Contacts[rng.Next(Contacts.Length)],
                        Email = $"contracting@{agency.Abbrev.ToLowerInvariant()}.gov",
                        Phone = $"({rng.Next(200, 999)}) {rng.Next(200, 999)}-{rng.Next(1000, 9999)}",
                        Title = "Contract Specialist",
                    },
                },
            };

            // Give the notice a searchable description so keyword filters have signal.
            var descriptionText =
                $"The {agency.Office} is seeking sources for {vertical.Keywords[rng.Next(vertical.Keywords.Length)]}. " +
                $"This {ptypeLabel.ToLowerInvariant()} covers {title}. Responses due {deadline:MMMM d, yyyy}. " +
                $"Place of performance: {city}, {state}.";

            dto.DescriptionText = descriptionText;
            dto.RawJson = SerializeRaw(dto, descriptionText);
            list.Add(dto);
        }
        return list;
    }

    private static string SerializeRaw(SamOpportunityDto dto, string descriptionText)
    {
        // Store a raw record that includes a resolved descriptionText field so the
        // normalizer/detail view has real text to show/search (kept in RawJson).
        var raw = new
        {
            dto.NoticeId,
            dto.Title,
            dto.SolicitationNumber,
            dto.FullParentPathName,
            dto.Type,
            dto.BaseType,
            dto.TypeOfSetAside,
            dto.TypeOfSetAsideDescription,
            dto.ResponseDeadLine,
            dto.PostedDate,
            dto.ArchiveDate,
            dto.NaicsCode,
            dto.ClassificationCode,
            dto.Active,
            dto.UiLink,
            dto.Description,
            descriptionText,
            dto.OrganizationType,
            dto.PlaceOfPerformance,
            dto.PointOfContact,
        };
        return JsonSerializer.Serialize(raw, Json);
    }

    // ---- Deterministic content pools ---------------------------------------

    private sealed record Vertical(string Prefix, string[] Naics, string[] Psc, string[] Titles, string[] Keywords);

    private static readonly Vertical[] Verticals =
    {
        new("IT", new[] { "541511", "541512", "541513", "541519", "518210" },
            new[] { "D307", "D310", "D302", "D399", "7030" },
            new[] { "IT Help Desk and Network Support Services", "Cloud Migration and Modernization",
                    "Custom Software Development", "Cybersecurity Assessment and Monitoring",
                    "Enterprise IT Managed Services" },
            new[] { "help desk", "network support", "cloud migration", "cybersecurity", "software development" }),
        new("CON", new[] { "236220", "237310", "238160", "238210" },
            new[] { "Y1AA", "Z2AA", "C211", "5680" },
            new[] { "Building Renovation and Alteration", "Roadway Paving and Repair",
                    "Roofing Replacement Project", "Facility Electrical Upgrades" },
            new[] { "construction", "renovation", "roofing", "paving", "electrical" }),
        new("HVAC", new[] { "238220", "811310" },
            new[] { "J041", "Z2AA", "4120" },
            new[] { "HVAC Chiller Replacement", "Boiler Maintenance and Repair",
                    "Air Conditioning System Upgrade", "Building Mechanical Services" },
            new[] { "HVAC", "air conditioning", "chiller", "boiler", "mechanical" }),
        new("PRO", new[] { "541611", "541618", "541690", "541990" },
            new[] { "R408", "R425", "R707", "R499" },
            new[] { "Program Management Support Services", "Acquisition and Advisory Support",
                    "Management Consulting Services", "Engineering and Technical Support" },
            new[] { "program management", "advisory", "consulting", "acquisition support" }),
        new("STAFF", new[] { "561320", "561311", "561110" },
            new[] { "R699", "R497" },
            new[] { "Temporary Administrative Staffing", "Professional Staffing Services",
                    "Facilities Support Staffing" },
            new[] { "staffing", "temporary personnel", "administrative support" }),
        new("FAC", new[] { "561720", "561210", "561730", "561612" },
            new[] { "S201", "S216", "S208", "S206" },
            new[] { "Custodial and Janitorial Services", "Grounds Maintenance and Landscaping",
                    "Base Operations Support", "Security Guard Services" },
            new[] { "custodial", "janitorial", "landscaping", "grounds maintenance", "guard services" }),
    };

    private sealed record AgencyRef(string Path, string Office, string Abbrev);

    private static readonly AgencyRef[] Agencies =
    {
        new("DEPT OF DEFENSE.DEPT OF THE ARMY.ARMY CONTRACTING COMMAND", "Army Contracting Command", "ACC"),
        new("DEPT OF DEFENSE.DEPT OF THE NAVY.NAVAL FACILITIES ENGINEERING SYSTEMS COMMAND", "NAVFAC", "NAVFAC"),
        new("DEPT OF DEFENSE.DEPT OF THE AIR FORCE.AIR FORCE MATERIEL COMMAND", "Air Force Materiel Command", "AFMC"),
        new("DEPT OF DEFENSE.DEFENSE LOGISTICS AGENCY", "Defense Logistics Agency", "DLA"),
        new("GENERAL SERVICES ADMINISTRATION.PUBLIC BUILDINGS SERVICE", "GSA Public Buildings Service", "GSA"),
        new("DEPT OF VETERANS AFFAIRS.VETERANS HEALTH ADMINISTRATION", "Veterans Health Administration", "VA"),
        new("DEPT OF HOMELAND SECURITY.FEDERAL EMERGENCY MANAGEMENT AGENCY", "FEMA", "FEMA"),
        new("DEPT OF HOMELAND SECURITY.U.S. COAST GUARD", "U.S. Coast Guard", "USCG"),
        new("DEPT OF HEALTH AND HUMAN SERVICES.NATIONAL INSTITUTES OF HEALTH", "National Institutes of Health", "NIH"),
        new("DEPT OF AGRICULTURE.FOREST SERVICE", "USDA Forest Service", "USDA"),
        new("DEPT OF THE INTERIOR.NATIONAL PARK SERVICE", "National Park Service", "NPS"),
        new("DEPT OF JUSTICE.FEDERAL BUREAU OF PRISONS", "Federal Bureau of Prisons", "BOP"),
        new("DEPT OF TRANSPORTATION.FEDERAL AVIATION ADMINISTRATION", "Federal Aviation Administration", "FAA"),
    };

    private static readonly (string Code, string Desc)[] SetAsides =
    {
        ("", "N/A"),
        ("SBA", "Total Small Business Set-Aside (FAR 19.5)"),
        ("SBA", "Total Small Business Set-Aside (FAR 19.5)"),
        ("8A", "8(a) Set-Aside (FAR 19.8)"),
        ("SDVOSBC", "Service-Disabled Veteran-Owned Small Business Set-Aside (FAR 19.14)"),
        ("WOSB", "Women-Owned Small Business Set-Aside (FAR 19.15)"),
        ("HZC", "HUBZone Set-Aside (FAR 19.13)"),
    };

    private static readonly (string Label, string Code)[] NoticeTypes =
    {
        ("Solicitation", "o"),
        ("Combined Synopsis/Solicitation", "k"),
        ("Presolicitation", "p"),
        ("Sources Sought", "r"),
        ("Solicitation", "o"),
        ("Combined Synopsis/Solicitation", "k"),
        ("Special Notice", "s"),
    };

    private static readonly (string State, string City, string Zip)[] Places =
    {
        ("VA", "Arlington", "22202"), ("TX", "San Antonio", "78234"), ("CA", "San Diego", "92101"),
        ("FL", "Jacksonville", "32202"), ("MD", "Bethesda", "20814"), ("GA", "Atlanta", "30303"),
        ("CO", "Denver", "80202"), ("WA", "Tacoma", "98402"), ("OH", "Dayton", "45402"),
        ("PA", "Philadelphia", "19107"), ("AL", "Huntsville", "35808"), ("AZ", "Phoenix", "85004"),
        ("NC", "Fayetteville", "28301"), ("NY", "Buffalo", "14202"), ("IL", "Chicago", "60604"),
    };

    private static readonly string[] Contacts =
    {
        "Jane Whitfield", "Marcus Bell", "Priya Nair", "David Okoro", "Emily Carter",
        "Luis Ramirez", "Sarah Kim", "James Donovan", "Aisha Rahman", "Robert Chen",
    };
}
